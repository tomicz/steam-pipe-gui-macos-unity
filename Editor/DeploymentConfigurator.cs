using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tomicz.Deployer
{
    [CreateAssetMenu(fileName = "DeploymentConfigurator", menuName = "Tomicz/Steam/Depoloyement Target")]
    public class DeploymentConfigurator : ScriptableObject
    {
        public BuildTarget BuildTarget => _buildTarget;
        public string Description => _description;
        public string AppName => _appName;
        public string SteamUsername => _steamUsername;
        public string sdkPath { get; set; }
        public string AppId => _appId;
        public string DepotId => _depotId;

        [SerializeField] private BuildTarget _buildTarget;

        [Header("App info")]
        [SerializeField] private string _appName = "";
        [SerializeField] private string _description = "";

        [Header("Steamworks info")]
        [SerializeField] private string _steamUsername;
        [SerializeField] private string _appId;
        [SerializeField] private string _depotId;

        public void OnBuildTargetClicked()
        {
            string outputPath = Path.Combine(sdkPath, "tools", "ContentBuilder", "content", $"{_buildTarget}", $"{_appName}{GetAppExecutionExtension()}");

            BuildPipeline.BuildPlayer(GetScenePaths(), outputPath, _buildTarget, BuildOptions.None);

            Debug.Log((int)_buildTarget);
            Debug.Log("Target successfully built.");
        }

        private string GetAppExecutionExtension()
        {
            if(_buildTarget == BuildTarget.StandaloneOSX)
            {
                return ".app";
            }
            else if(_buildTarget == BuildTarget.StandaloneWindows)
            {
                return ".exe";
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