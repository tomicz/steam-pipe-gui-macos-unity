using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Tomicz.Deployer
{
    /// <summary>
    /// The build and upload steps shared by the Inspector buttons and the command line entry points.
    /// Every step logs what went wrong and returns false instead of throwing.
    /// </summary>
    public static class Deployer
    {
        private const string DoNotShipFolderSuffix = "_BackUpThisFolder_ButDontShipItWithYourGame";

        /// <summary>
        /// Validates the target, writes the VDF scripts and builds the player. Returns true when the build succeeded.
        /// </summary>
        public static bool Build(DeploymentConfigurator configurator)
        {
            if (!configurator.Validate())
            {
                return false;
            }

            WriteVdfScripts(configurator);
            return configurator.BuildPlayer();
        }

        /// <summary>
        /// Validates the target, checks that a build and steamcmd exist, rewrites the VDF scripts so they match the
        /// current values, deletes the IL2CPP do-not-ship folder and writes the upload script.
        /// Returns true when the upload script can be run.
        /// </summary>
        public static bool PrepareUpload(DeploymentConfigurator configurator)
        {
            if (!configurator.Validate())
            {
                return false;
            }

            if (!configurator.HasBuild)
            {
                Debug.LogError($"No build found at {configurator.ExecutablePath}. Generate a build first.", configurator);
                return false;
            }

            string steamcmdPath = GetSteamcmdPath(configurator);

            if (!File.Exists(steamcmdPath))
            {
                Debug.LogError($"steamcmd.sh not found at {steamcmdPath}. Check the SDK folder path.", configurator);
                return false;
            }

            if (configurator.SdkPath.Contains("'"))
            {
                Debug.LogError("The SDK folder path must not contain a single quote (').", configurator);
                return false;
            }

            WriteVdfScripts(configurator);
            DeleteDoNotShipFolder(configurator);
            File.WriteAllText(GetUploadScriptPath(configurator), GetUploadScript(configurator));

            return true;
        }

        public static string GetSteamcmdPath(DeploymentConfigurator configurator)
        {
            return Path.Combine(configurator.ContentBuilderPath, "builder_osx", "steamcmd.sh");
        }

        /// <summary>
        /// Shell script written by PrepareUpload. Running it uploads the prepared build.
        /// </summary>
        public static string GetUploadScriptPath(DeploymentConfigurator configurator)
        {
            return Path.Combine(configurator.ScriptsPath, $"upload_build_{configurator.AppId}_{configurator.BuildTarget}.sh");
        }

        /// <summary>
        /// The steamcmd command line. Paths are single-quoted so SDK folders containing spaces work in the shell.
        /// </summary>
        public static string GetUploadCommand(DeploymentConfigurator configurator)
        {
            return $"'{GetSteamcmdPath(configurator)}' +login {configurator.SteamUsername} +run_app_build_http '{configurator.AppVdfPath}' +quit";
        }

        /// <summary>
        /// Opens a Terminal window and runs the upload command in it, so the user can answer Steam's password and
        /// Steam Guard prompts.
        /// </summary>
        public static void RunUploadInTerminal(DeploymentConfigurator configurator)
        {
            string activate = "tell application \"Terminal\" to activate";
            string doScript = $"tell application \"Terminal\" to do script \"{GetUploadCommand(configurator)}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e {QuoteArgument(activate)} -e {QuoteArgument(doScript)}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
        }

        public static void WriteVdfScripts(DeploymentConfigurator configurator)
        {
            Directory.CreateDirectory(configurator.ScriptsPath);

            List<KeyValuePair<string, string>> depotScripts = new List<KeyValuePair<string, string>>();

            foreach (Depot depot in configurator.Depots)
            {
                string depotVdfPath = configurator.GetDepotVdfPath(depot.DepotId);
                File.WriteAllText(depotVdfPath, VdfGenerator.DepotBuild(depot.DepotId, configurator.ContentPath, depot.LocalPath));
                depotScripts.Add(new KeyValuePair<string, string>(depot.DepotId, depotVdfPath));
            }

            File.WriteAllText(configurator.AppVdfPath, VdfGenerator.AppBuild(configurator.AppId, configurator.BuildDescription, configurator.BuildOutputPath, configurator.SetLiveBranch, depotScripts));
        }

        public static void DeleteDoNotShipFolder(DeploymentConfigurator configurator)
        {
            if (!configurator.DeleteDoNotShipFolder)
            {
                return;
            }

            string folderPath = Path.Combine(configurator.ContentPath, configurator.AppName + DoNotShipFolderSuffix);

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                Debug.Log($"Deleted IL2CPP debug folder: {folderPath}");
            }
        }

        private static string GetUploadScript(DeploymentConfigurator configurator)
        {
            return "#!/bin/sh\n" +
                   $"# Generated by SteamPipeGUI for macOS. Uploads the prepared {configurator.BuildTarget} build of app {configurator.AppId}.\n" +
                   "# Regenerated every time Upload or PrepareUpload runs.\n" +
                   GetUploadCommand(configurator) + "\n";
        }

        // Wraps a process argument in double quotes, escaping backslashes and quotes inside it.
        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
