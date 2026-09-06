using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tomicz.Deployer
{
    [CustomEditor(typeof(DeploymentConfigurator))]
    public class DeploymentConfiguratorEditor : Editor
    {
        private const string DoNotShipFolderSuffix = "_BackUpThisFolder_ButDontShipItWithYourGame";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DeploymentConfigurator configurator = (DeploymentConfigurator)target;

            GUILayout.Space(10);
            DrawSdkPathField();
            GUILayout.Space(10);

            if (GUILayout.Button("Generate Build"))
            {
                GenerateBuild(configurator);
            }

            if (GUILayout.Button("Upload"))
            {
                Upload(configurator);
            }

            GUILayout.Space(10);
        }

        private void DrawSdkPathField()
        {
            serializedObject.Update();

            SerializedProperty sdkPath = serializedObject.FindProperty("_sdkPath");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(sdkPath, new GUIContent("SDK Folder Path", "Root folder of the Steamworks SDK, the one containing tools/ContentBuilder."));

                if (GUILayout.Button("Browse", GUILayout.Width(80)))
                {
                    string selected = EditorUtility.OpenFolderPanel("Select Steamworks SDK folder", sdkPath.stringValue, "");

                    if (!string.IsNullOrEmpty(selected))
                    {
                        sdkPath.stringValue = selected;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void GenerateBuild(DeploymentConfigurator configurator)
        {
            WriteVdfScripts(configurator);
            configurator.BuildPlayer();
        }

        private static void Upload(DeploymentConfigurator configurator)
        {
            DeleteDoNotShipFolder(configurator);
            OpenTerminal(configurator.SdkPath, configurator.SteamUsername, configurator.DepotId);
        }

        private static void WriteVdfScripts(DeploymentConfigurator configurator)
        {
            Directory.CreateDirectory(configurator.ScriptsPath);
            File.WriteAllText(configurator.AppVdfPath, GetAppVdfContent(configurator));
            File.WriteAllText(configurator.DepotVdfPath, GetDepotVdfContent(configurator));
        }

        private static string GetAppVdfContent(DeploymentConfigurator configurator)
        {
            string buildOutputPath = Path.Combine(configurator.ContentBuilderPath, "output", configurator.BuildTarget.ToString());

            return "appbuild\n{\n" +
                   $"\t\"appid\" \"{configurator.AppId}\"\n" +
                   $"\t\"desc\" \"{configurator.Description}\"\n" +
                   $"\t\"buildoutput\" \"{buildOutputPath}\"\n" +
                   "\t\"contentroot\" \"\"\n" +
                   "\t\"setlive\" \"beta\"\n" +
                   "\t\"preview\" \"0\"\n" +
                   "\t\"local\" \"\"\n" +
                   "\t\"depots\"\n\t{\n" +
                   $"\t\t\"{configurator.DepotId}\" \"{configurator.DepotVdfPath}\"\n" +
                   "\t}\n}\n";
        }

        private static string GetDepotVdfContent(DeploymentConfigurator configurator)
        {
            return "DepotBuildConfig\n{\n" +
                   $"\t\"DepotID\" \"{configurator.DepotId}\"\n" +
                   $"\t\"contentroot\" \"{configurator.ContentPath}\"\n" +
                   "\t\"FileMapping\"\n\t{\n" +
                   "\t\t\"LocalPath\" \"*\"\n" +
                   "\t\t\"DepotPath\" \".\"\n" +
                   "\t\t\"recursive\" \"1\"\n" +
                   "\t}\n" +
                   "\t\"FileExclusion\" \"*.pdb\"\n" +
                   "}\n";
        }

        private static void DeleteDoNotShipFolder(DeploymentConfigurator configurator)
        {
            if (!configurator.DeleteDoNotShipFolder)
            {
                return;
            }

            string folderPath = Path.Combine(configurator.ContentPath, configurator.AppName + DoNotShipFolderSuffix);

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                UnityEngine.Debug.Log($"Deleted IL2CPP debug folder: {folderPath}");
            }
        }

        public static void OpenTerminal(string sdkPath, string username, string depotId)
        {
            string steamCmdCommand = $"{sdkPath}/tools/ContentBuilder/builder_osx/steamcmd.sh +login {username} +run_app_build_http {sdkPath}/tools/ContentBuilder/scripts/app_{depotId}.vdf +quit";
            string terminalPath = "/System/Applications/Utilities/Terminal.app";

            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = terminalPath,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);

            RunSteamcmdCommand(steamCmdCommand);
        }

        private static void RunSteamcmdCommand(string steamcmdCommand)
        {
            // Run the SteamCMD command in Terminal
            ProcessStartInfo runCommandInfo = new ProcessStartInfo()
            {
                FileName = "osascript", // osascript is a command-line tool for executing AppleScripts
                Arguments = $"-e 'tell application \"Terminal\" to do script \"{steamcmdCommand}\"'",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(runCommandInfo);
        }
    }
}
