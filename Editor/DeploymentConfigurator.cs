using System.IO;
using UnityEditor;
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

        public void BuildPlayer()
        {
            string outputPath = Path.Combine(ContentPath, _appName + GetExecutableExtension());

            BuildPipeline.BuildPlayer(GetScenePaths(), outputPath, _buildTarget, BuildOptions.None);

            Debug.Log("Target successfully built.");
        }

        private string GetExecutableExtension()
        {
            if (_buildTarget == BuildTarget.StandaloneOSX)
            {
                return ".app";
            }

            return ".exe";
        }

        private static string[] GetScenePaths()
        {
            string[] scenes = new string[EditorBuildSettings.scenes.Length];

            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }

            return scenes;
        }
    }
}
