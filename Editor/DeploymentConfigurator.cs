using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tomicz.Deployer
{
    [CreateAssetMenu(fileName = "DeploymentConfigurator", menuName = "Tomicz/Steam/Deployment Target")]
    public class DeploymentConfigurator : ScriptableObject, ISerializationCallbackReceiver
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
        public IReadOnlyList<Depot> Depots => _depots;
        public string SetLiveBranch => _setLiveBranch;
        public bool DeleteDoNotShipFolder => _deleteDoNotShipFolder;
        public string SdkPath => _sdkPath;

        public string ContentBuilderPath => Path.Combine(_sdkPath, "tools", "ContentBuilder");
        public string ContentPath => Path.Combine(ContentBuilderPath, "content", _buildTarget.ToString());
        public string ScriptsPath => Path.Combine(ContentBuilderPath, "scripts");
        public string BuildOutputPath => Path.Combine(ContentBuilderPath, "output", _buildTarget.ToString());
        public string AppVdfPath => Path.Combine(ScriptsPath, $"app_build_{_appId}_{_buildTarget}.vdf");
        public string ExecutablePath => Path.Combine(ContentPath, _appName + GetExecutableExtension());
        public bool HasBuild => File.Exists(ExecutablePath) || Directory.Exists(ExecutablePath);

        [Header("Build")]
        [SerializeField] private BuildTarget _buildTarget;
        [Tooltip("Build with the Development Build option: profiler connection, script debugging and the development console.")]
        [SerializeField] private bool _developmentBuild = false;

        [Header("App info")]
        [SerializeField] private string _appName = "";
        [SerializeField] private string _description = "";
        [Tooltip("Append the version from Player Settings to the description, so \"Release candidate\" becomes \"Release candidate 1.2.0\".")]
        [SerializeField] private bool _appendVersionToDescription = false;

        [Header("Steamworks info")]
        [SerializeField] private string _steamUsername;
        [SerializeField] private string _appId;
        [Tooltip("Depots that receive this build. Most games need one per platform. Add more for DLC or shared-content depots that take a subfolder of the build.")]
        [SerializeField] private List<Depot> _depots = new List<Depot>();
        [Tooltip("Branch the uploaded build is set live on, for example beta. Leave empty to upload without setting it live, then pick the build manually under SteamPipe > Builds.")]
        [SerializeField] private string _setLiveBranch = "beta";

        // Single depot ID from versions before 1.2.0. Moved into _depots on load.
        [HideInInspector]
        [SerializeField] private string _depotId;

        [Header("IL2CPP")]
        [Tooltip("IL2CPP builds create a folder named <App Name>_BackUpThisFolder_ButDontShipItWithYourGame next to the executable. It must not be uploaded to Steam. When enabled, the folder is deleted when you click Upload, so back it up between Generate Build and Upload if you need it for debugging. Has no effect on Mono builds.")]
        [SerializeField] private bool _deleteDoNotShipFolder = true;

        // Drawn by DeploymentConfiguratorEditor next to a Browse button.
        [HideInInspector]
        [SerializeField] private string _sdkPath = "";

        public string GetDepotVdfPath(string depotId)
        {
            return Path.Combine(ScriptsPath, $"depot_build_{depotId}.vdf");
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(_depotId))
            {
                return;
            }

            if (_depots.Count == 0)
            {
                _depots.Add(new Depot(_depotId, "*"));
            }

            _depotId = null;
        }

        /// <summary>
        /// Logs an error for every required field that is missing. Returns true when all are set.
        /// </summary>
        public bool Validate()
        {
            bool valid = true;

            valid &= Require(IsStandalone(_buildTarget), $"Build target {_buildTarget} is not a standalone platform. Use StandaloneOSX, StandaloneWindows, StandaloneWindows64 or StandaloneLinux64.");
            valid &= Require(GetScenePaths().Length > 0, "No scenes are enabled in Build Settings (File > Build Settings). Add at least one scene.");
            valid &= Require(!string.IsNullOrEmpty(_sdkPath) && Directory.Exists(ContentBuilderPath), "SDK folder path is not set or does not contain tools/ContentBuilder.");
            valid &= Require(!string.IsNullOrWhiteSpace(_appName), "App name is empty.");
            valid &= Require(_appName.IndexOfAny(InvalidAppNameChars) < 0, "App name must not contain slashes or double quotes.");
            valid &= Require(_description.IndexOf('"') < 0, "Description must not contain double quotes.");
            valid &= Require(_setLiveBranch.IndexOf('"') < 0, "Set Live Branch must not contain double quotes.");
            valid &= Require(!string.IsNullOrWhiteSpace(_steamUsername), "Steam username is empty.");
            valid &= Require(!string.IsNullOrWhiteSpace(_appId), "App ID is empty.");
            valid &= Require(_depots.Count > 0, "At least one depot is required.");

            HashSet<string> seenDepotIds = new HashSet<string>();

            foreach (Depot depot in _depots)
            {
                valid &= Require(depot != null && !string.IsNullOrWhiteSpace(depot.DepotId), "A depot has no Depot ID.");

                if (depot != null && !string.IsNullOrWhiteSpace(depot.DepotId))
                {
                    valid &= Require(seenDepotIds.Add(depot.DepotId), $"Depot ID {depot.DepotId} is listed more than once.");
                }
            }

            return valid;
        }

        /// <summary>
        /// Builds the enabled scenes into the SDK content folder. Returns true when the build succeeded.
        /// </summary>
        public bool BuildPlayer()
        {
            BuildOptions options = _developmentBuild ? BuildOptions.Development : BuildOptions.None;
            BuildSummary summary = BuildPipeline.BuildPlayer(GetScenePaths(), ExecutablePath, _buildTarget, options).summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"{Deployer.LogPrefix}Build succeeded: {summary.outputPath}", this);
                return true;
            }

            Debug.LogError($"{Deployer.LogPrefix}Build did not succeed (result {summary.result}, {summary.totalErrors} error(s)). See the console for details.", this);
            return false;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{Deployer.LogPrefix}Deployment target '{name}': {message}", this);
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
