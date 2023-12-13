using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Tomicz.Deployer
{
    [CustomEditor(typeof(DeploymentConfigurator))]
    public class DeploymentConfiguratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            DeploymentConfigurator deploymentConfigurator = (DeploymentConfigurator)target;

            GUILayout.BeginHorizontal();
            GetSDKPath(deploymentConfigurator);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GenerateBuild(deploymentConfigurator);
            GUILayout.EndHorizontal();

            UploadTarget(deploymentConfigurator);

            GUILayout.Space(10);
        }

        private void GenerateDepotFile(DeploymentConfigurator deploymentConfigurator)
        {
            string depotVDFPath = Path.Combine(deploymentConfigurator.sdkPath, "tools", "ContentBuilder", "scripts", $"app_{deploymentConfigurator.DepotId}.vdf");

            File.WriteAllText(depotVDFPath, GetDepotContent(deploymentConfigurator));
            UpdateDepotVDF(deploymentConfigurator);
        }

        private string GetDepotContent(DeploymentConfigurator deploymentConfigurator)
        {
            string vdfContent = "appbuild\n{\n";

            vdfContent += $"\t\"appid\" \"{deploymentConfigurator.AppId}\"\n";
            vdfContent += $"\t\"desc\" \"{deploymentConfigurator.Description}\"\n";
            vdfContent += $"\t\"buildoutput\" \"{Path.Combine(deploymentConfigurator.sdkPath, "tools", "ContentBuilder", "output", deploymentConfigurator.BuildTarget.ToString())}\" // Replace this with the correct property\n";
            vdfContent += "\t\"contentroot\" \"\"\n";
            vdfContent += "\t\"setlive\" \"beta\"\n";  // You can modify this line if needed
            vdfContent += "\t\"preview\" \"0\"\n";
            vdfContent += "\t\"local\" \"\"\n";
            vdfContent += "\t\"depots\"\n\t{\n";
            vdfContent += $"\t\t\"{deploymentConfigurator.DepotId}\" \"{Path.Combine(deploymentConfigurator.sdkPath, "tools", "ContentBuilder", "scripts", $"depot_{deploymentConfigurator.DepotId}.vdf")}\"\n";
            vdfContent += "\t}\n}";

            return vdfContent;
        }

        private void UpdateDepotVDF(DeploymentConfigurator deploymentConfigurator)
        {
            string depotVDFPath = Path.Combine(deploymentConfigurator.sdkPath, "tools", "ContentBuilder", "scripts", $"depot_{deploymentConfigurator.DepotId}.vdf");

            File.WriteAllText(depotVDFPath, GetDepotBuildConfigContent(deploymentConfigurator));
        }

        private string GetDepotBuildConfigContent(DeploymentConfigurator deploymentConfigurator)
        {
            string depotBuildConfigContent = "DepotBuildConfig\n{\n";

            depotBuildConfigContent += $"\t\"DepotID\" \"{deploymentConfigurator.DepotId}\"\n";
            depotBuildConfigContent += $"\t\"contentroot\" \"{Path.Combine(deploymentConfigurator.sdkPath, "tools", "ContentBuilder", "content", deploymentConfigurator.BuildTarget.ToString())}\"\n";
            depotBuildConfigContent += "\t\"FileMapping\"\n\t{\n";
            depotBuildConfigContent += "\t\t\"LocalPath\" \"*\"\n";
            depotBuildConfigContent += "\t\t\"DepotPath\" \".\"\n";
            depotBuildConfigContent += "\t\t\"recursive\" \"1\"\n";
            depotBuildConfigContent += "\t}\n";
            depotBuildConfigContent += "\t\"FileExclusion\" \"*.pdb\"\n";
            depotBuildConfigContent += "}\n";

            return depotBuildConfigContent;
        }

        private void GetSDKPath(DeploymentConfigurator deploymentConfigurator)
        {
            EditorGUILayout.LabelField("SDK Folder Path:", deploymentConfigurator.sdkPath);

            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string sdkPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");

                if (!string.IsNullOrEmpty(sdkPath))
                {
                    deploymentConfigurator.sdkPath = sdkPath;

                    EditorUtility.SetDirty(deploymentConfigurator);
                }
            }
        }

        private void GenerateBuild(DeploymentConfigurator deploymentConfigurator)
        {
            if (GUILayout.Button("Generate Build"))
            {
                GenerateDepotFile(deploymentConfigurator);
                deploymentConfigurator.OnBuildTargetClicked();
            }
        }

        private void UploadTarget(DeploymentConfigurator deploymentConfigurator)
        {
            if (GUILayout.Button("Upload"))
            {
                RunSteamCommand(deploymentConfigurator.sdkPath, deploymentConfigurator.SteamUsername, deploymentConfigurator.DepotId, deploymentConfigurator.Description, deploymentConfigurator.BuildTarget);
            }
        }

        public static void RunSteamCommand(string sdkPath, string username, string depotId, string buildNumber, BuildTarget buildTarget)
        {
            string steamCmdCommand = $"{sdkPath}/tools/ContentBuilder/builder_osx/steamcmd.sh +login {username} +run_app_build_http {sdkPath}/tools/ContentBuilder/scripts/app_{depotId}.vdf +quit";
            string terminalPath = "/System/Applications/Utilities/Terminal.app";

            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = terminalPath,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);

            RunSteamCmdCommand(steamCmdCommand);
        }

        static void RunSteamCmdCommand(string steamCmdCommand)
        {
            // Run the SteamCMD command in Terminal
            ProcessStartInfo runCommandInfo = new ProcessStartInfo()
            {
                FileName = "osascript", // osascript is a command-line tool for executing AppleScripts
                Arguments = $"-e 'tell application \"Terminal\" to do script \"{steamCmdCommand}\"'",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(runCommandInfo);
        }

        public static void UpdateBuildDescription(string sdkPath, string depotId, string description)
        {
            string vdfFilePath = Path.Combine(sdkPath, "tools", "ContentBuilder", "scripts", $"app_{depotId}.vdf");

            if (string.IsNullOrEmpty(vdfFilePath))
            {
                UnityEngine.Debug.LogError("Couldn't find vdf path");
                return;
            }

            // Read the content of the VDF file.
            string vdfContent = File.ReadAllText(vdfFilePath);

            // Define a regular expression to match the "description" entry.
            string pattern = "\"desc\"[ \t]+\"[^\"]+\"";

            // Use Regex to find and replace the description entry.
            vdfContent = Regex.Replace(vdfContent, pattern, $"\"desc\" \"{description}\"");

            // Write the modified content back to the file.
            File.WriteAllText(vdfFilePath, vdfContent);

            Console.WriteLine("Build description updated.");
        }
    }
}