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
        // These end up in file paths or unescaped VDF strings.
        private static readonly char[] InvalidAppNameChars = { '/', '\\', '"' };

        public BuildTarget BuildTarget => _buildTarget;
        public string Description => _description;

        /// <summary>
        /// Description written to the app VDF, with the Player Settings version appended when enabled.
        /// </summary>
        public string BuildDescription
        {
            get
            {
                if (!_appendVersionToDescription)
                {
                    return _description;
                }

                string version = PlayerSettings.bundleVersion;
                return string.IsNullOrEmpty(_description) ? version : $"{_description} {version}";
            }
        }

        public string AppName => _appName;
        public string SteamUsername => _steamUsername;
        public string AppId => _appId;
        public string DepotId => _depotId;
        public string SetLiveBranch => _setLiveBranch;
        public bool DeleteDoNotShipFolder => _deleteDoNotShipFolder;
        public string SdkPath => _sdkPath;

        public string ContentBuilderPath => Path.Combine(_sdkPath, "tools", "ContentBuilder");
        public string ContentPath => Path.Combine(ContentBuilderPath, "content", _buildTarget.ToString());
        public string ScriptsPath => Path.Combine(ContentBuilderPath, "scripts");
        public string AppVdfPath => Path.Combine(ScriptsPath, $"app_{_depotId}.vdf");
        public string DepotVdfPath => Path.Combine(ScriptsPath, $"depot_{_depotId}.vdf");
        public string ExecutablePath => Path.Combine(ContentPath, _appName + GetExecutableExtension());
        public bool HasBuild => File.Exists(ExecutablePath) || Directory.Exists(ExecutablePath);

        [SerializeField] private BuildTarget _buildTarget;

        [Header("App info")]
        [SerializeField] private string _appName = "";
        [SerializeField] private string _description = "";
        [Tooltip("Append the version from Player Settings to the description, so \"Release candidate\" becomes \"Release candidate 1.2.0\".")]
        [SerializeField] private bool _appendVersionToDescription = false;

        [Header("Steamworks info")]
        [SerializeField] private string _steamUsername;
        [SerializeField] private string _appId;
        [SerializeField] private string _depotId;
        [Tooltip("Branch the uploaded build is set live on, for example beta. Leave empty to upload without setting it live, then pick the build manually under SteamPipe > Builds.")]
        [SerializeField] private string _setLiveBranch = "beta";

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

            valid &= Require(IsStandalone(_buildTarget), $"Build target {_buildTarget} is not a standalone platform. Use StandaloneOSX, StandaloneWindows, StandaloneWindows64 or StandaloneLinux64.");
            valid &= Require(!string.IsNullOrEmpty(_sdkPath) && Directory.Exists(ContentBuilderPath), "SDK folder path is not set or does not contain tools/ContentBuilder.");
            valid &= Require(!string.IsNullOrWhiteSpace(_appName), "App name is empty.");
            valid &= Require(_appName.IndexOfAny(InvalidAppNameChars) < 0, "App name must not contain slashes or double quotes.");
            valid &= Require(_description.IndexOf('"') < 0, "Description must not contain double quotes.");
            valid &= Require(_setLiveBranch.IndexOf('"') < 0, "Set Live Branch must not contain double quotes.");
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
            BuildSummary summary = BuildPipeline.BuildPlayer(GetScenePaths(), ExecutablePath, _buildTarget, BuildOptions.None).summary;

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

        private static bool IsStandalone(BuildTarget buildTarget)
        {
            return buildTarget == BuildTarget.StandaloneOSX
                || buildTarget == BuildTarget.StandaloneWindows
                || buildTarget == BuildTarget.StandaloneWindows64
                || buildTarget == BuildTarget.StandaloneLinux64;
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
