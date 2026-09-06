using System;
using System.Collections.Generic;
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
                Defer(() => GenerateBuild(configurator));
            }

            if (GUILayout.Button("Upload"))
            {
                Defer(() => Upload(configurator));
            }

            if (GUILayout.Button("Build and Upload"))
            {
                Defer(() =>
                {
                    if (GenerateBuild(configurator))
                    {
                        Upload(configurator);
                    }
                });
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

        // Builds and process launches run after the GUI pass so they cannot break the inspector layout.
        private static void Defer(Action action)
        {
            EditorApplication.delayCall += () => action();
        }

        private static bool GenerateBuild(DeploymentConfigurator configurator)
        {
            if (!configurator.Validate())
            {
                return false;
            }

            WriteVdfScripts(configurator);
            return configurator.BuildPlayer();
        }

        private static void Upload(DeploymentConfigurator configurator)
        {
            if (!configurator.Validate())
            {
                return;
            }

            if (!configurator.HasBuild)
            {
                UnityEngine.Debug.LogError($"No build found at {configurator.ExecutablePath}. Click Generate Build first.", configurator);
                return;
            }

            // Regenerate so the VDF reflects the current description and branch, even if they changed after the build.
            WriteVdfScripts(configurator);
            DeleteDoNotShipFolder(configurator);
            RunSteamcmdInTerminal(configurator);
        }

        private static void WriteVdfScripts(DeploymentConfigurator configurator)
        {
            Directory.CreateDirectory(configurator.ScriptsPath);

            File.WriteAllText(configurator.DepotVdfPath, VdfGenerator.DepotBuild(configurator.DepotId, configurator.ContentPath, "*"));

            List<KeyValuePair<string, string>> depotScripts = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(configurator.DepotId, configurator.DepotVdfPath)
            };

            File.WriteAllText(configurator.AppVdfPath, VdfGenerator.AppBuild(configurator.AppId, configurator.BuildDescription, configurator.BuildOutputPath, configurator.SetLiveBranch, depotScripts));
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

        private static void RunSteamcmdInTerminal(DeploymentConfigurator configurator)
        {
            string steamcmdPath = Path.Combine(configurator.ContentBuilderPath, "builder_osx", "steamcmd.sh");

            if (!File.Exists(steamcmdPath))
            {
                UnityEngine.Debug.LogError($"steamcmd.sh not found at {steamcmdPath}. Check the SDK folder path.", configurator);
                return;
            }

            if (configurator.SdkPath.Contains("'"))
            {
                UnityEngine.Debug.LogError("The SDK folder path must not contain a single quote (').", configurator);
                return;
            }

            // Paths are single-quoted so SDK folders containing spaces work in the shell.
            string command = $"'{steamcmdPath}' +login {configurator.SteamUsername} +run_app_build_http '{configurator.AppVdfPath}' +quit";

            string activate = "tell application \"Terminal\" to activate";
            string doScript = $"tell application \"Terminal\" to do script \"{command}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e {QuoteArgument(activate)} -e {QuoteArgument(doScript)}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
        }

        // Wraps a process argument in double quotes, escaping backslashes and quotes inside it.
        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
