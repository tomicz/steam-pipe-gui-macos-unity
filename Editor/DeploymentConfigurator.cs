using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tomicz.Deployer
{
    [CreateAssetMenu(fileName = "DeploymentConfigurator", menuName = "Tomicz/Steam/Deployment Target")]
    public class DeploymentConfigurator : ScriptableObject
    {
        public BuildTarget BuildTarget => _buildTarget;
        public string Description => _description;
        public string AppName => _appName;
        public string SteamUsername => _steamUsername;
        public string AppId => _appId;
        public string DepotId => _depotId;
        public bool DeleteDoNotShipFolder => _deleteDoNotShipFolder;
        public string SdkPath => _sdkPath;

        public string ContentBuilderPath => Path.Combine(_sdkPath, "tools", "ContentBuilder");
        public string ContentPath => Path.Combine(ContentBuilderPath, "content", _buildTarget.ToString());
        public string ScriptsPath => Path.Combine(ContentBuilderPath, "scripts");
        public string AppVdfPath => Path.Combine(ScriptsPath, $"app_{_depotId}.vdf");
        public string DepotVdfPath => Path.Combine(ScriptsPath, $"depot_{_depotId}.vdf");

        [SerializeField] private BuildTarget _buildTarget;

        [Header("App info")]
        [SerializeField] private string _appName = "";
        [SerializeField] private string _description = "";

        [Header("Steamworks info")]
        [SerializeField] private string _steamUsername;
        [SerializeField] private string _appId;
        [SerializeField] private string _depotId;

        [Header("IL2CPP")]
        [Tooltip("IL2CPP builds create a folder named <App Name>_BackUpThisFolder_ButDontShipItWithYourGame next to the executable. It must not be uploaded to Steam. When enabled, the folder is deleted when you click Upload, so back it up between Generate Build and Upload if you need it for debugging. Has no effect on Mono builds.")]
        [SerializeField] private bool _deleteDoNotShipFolder = true;

        // Drawn by DeploymentConfiguratorEditor next to a Browse button.
        [HideInInspector]
        [SerializeField] private string _sdkPath = "";

        /// <summary>
        /// Logs an error for every required field that is missing. Returns true when all are set.
        /// </summary>
        public bool Validate()
        {
            bool valid = true;

            valid &= Require(!string.IsNullOrEmpty(_sdkPath) && Directory.Exists(ContentBuilderPath), "SDK folder path is not set or does not contain tools/ContentBuilder.");
            valid &= Require(!string.IsNullOrWhiteSpace(_appName), "App name is empty.");
            valid &= Require(!string.IsNullOrWhiteSpace(_steamUsername), "Steam username is empty.");
            valid &= Require(!string.IsNullOrWhiteSpace(_appId), "App ID is empty.");
            valid &= Require(!string.IsNullOrWhiteSpace(_depotId), "Depot ID is empty.");

            return valid;
        }

        /// <summary>
        /// Builds the enabled scenes into the SDK content folder. Returns true when the build succeeded.
        /// </summary>
        public bool BuildPlayer()
        {
            string outputPath = Path.Combine(ContentPath, _appName + GetExecutableExtension());
            BuildSummary summary = BuildPipeline.BuildPlayer(GetScenePaths(), outputPath, _buildTarget, BuildOptions.None).summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.outputPath}", this);
                return true;
            }

            Debug.LogError($"Build {summary.result} with {summary.totalErrors} error(s). See the console for details.", this);
            return false;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"Deployment target '{name}': {message}", this);
            }

            return condition;
        }

        private string GetExecutableExtension()
        {
            switch (_buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.StandaloneLinux64:
                    return ".x86_64";
                default:
                    return ".exe";
            }
        }

        private static string[] GetScenePaths()
        {
            List<string> scenes = new List<string>();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes.ToArray();
        }
    }
}
