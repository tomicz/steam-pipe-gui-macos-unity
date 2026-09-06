using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tomicz.Deployer
{
    /// <summary>
    /// Entry points for Unity batch mode, so a build can be produced and uploaded without the editor GUI:
    ///
    ///   Unity -batchmode -nographics -quit -projectPath . -logFile - \
    ///     -executeMethod Tomicz.Deployer.CommandLine.Build -deploymentTarget Assets/Deployment/MacOS.asset
    ///
    /// Every method reads the target asset from -deploymentTarget and exits Unity with code 1 on failure.
    /// Messages are prefixed with [SteamDeployer] so they are easy to find in the log.
    /// </summary>
    public static class CommandLine
    {
        private const string Prefix = "[SteamDeployer] ";

        /// <summary>Validates the target, writes the VDF scripts and builds the player.</summary>
        public static void Build()
        {
            Run(configurator => Deployer.Build(configurator));
        }

        /// <summary>
        /// Checks that a build exists, rewrites the VDF scripts, deletes the IL2CPP folder and writes the upload script.
        /// </summary>
        public static void PrepareUpload()
        {
            Run(configurator => Deployer.PrepareUpload(configurator) && ReportUploadScript(configurator));
        }

        /// <summary>Build followed by PrepareUpload in one editor session.</summary>
        public static void BuildAndPrepareUpload()
        {
            Run(configurator => Deployer.Build(configurator) && Deployer.PrepareUpload(configurator) && ReportUploadScript(configurator));
        }

        /// <summary>
        /// Creates the target asset at -deploymentTarget, or updates it when it already exists. Only the fields passed
        /// as arguments change:
        ///   -buildTarget StandaloneOSX|StandaloneWindows|StandaloneWindows64|StandaloneLinux64
        ///   -appName -description -steamUsername -appId -setLiveBranch -sdkPath   (strings)
        ///   -appendVersion -developmentBuild -deleteDoNotShipFolder             (true|false)
        ///   -depot id[:localPath]   (repeat for several depots; replaces the whole list)
        /// </summary>
        public static void CreateTarget()
        {
            Dictionary<string, List<string>> args = ParseArguments();
            string path = GetSingle(args, "deploymentTarget");

            if (string.IsNullOrEmpty(path))
            {
                Fail("-deploymentTarget <Assets/Folder/Name.asset> is required.");
                return;
            }

            DeploymentConfigurator configurator = AssetDatabase.LoadAssetAtPath<DeploymentConfigurator>(path);

            if (configurator == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.Refresh();
                configurator = ScriptableObject.CreateInstance<DeploymentConfigurator>();
                AssetDatabase.CreateAsset(configurator, path);
                Log($"Created {path}");
            }
            else
            {
                Log($"Updating {path}");
            }

            SerializedObject serialized = new SerializedObject(configurator);

            if (TryGetSingle(args, "buildTarget", out string buildTargetName))
            {
                if (!Enum.TryParse(buildTargetName, true, out BuildTarget buildTarget))
                {
                    Fail($"Unknown build target '{buildTargetName}'. Use StandaloneOSX, StandaloneWindows, StandaloneWindows64 or StandaloneLinux64.");
                    return;
                }

                serialized.FindProperty("_buildTarget").intValue = (int)buildTarget;
            }

            SetString(serialized, args, "appName", "_appName");
            SetString(serialized, args, "description", "_description");
            SetString(serialized, args, "steamUsername", "_steamUsername");
            SetString(serialized, args, "appId", "_appId");
            SetString(serialized, args, "setLiveBranch", "_setLiveBranch");
            SetString(serialized, args, "sdkPath", "_sdkPath");
            SetBool(serialized, args, "appendVersion", "_appendVersionToDescription");
            SetBool(serialized, args, "developmentBuild", "_developmentBuild");
            SetBool(serialized, args, "deleteDoNotShipFolder", "_deleteDoNotShipFolder");

            if (args.TryGetValue("depot", out List<string> depots))
            {
                SerializedProperty list = serialized.FindProperty("_depots");
                list.arraySize = depots.Count;

                for (int i = 0; i < depots.Count; i++)
                {
                    string[] parts = depots[i].Split(new[] { ':' }, 2);
                    SerializedProperty element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_depotId").stringValue = parts[0];
                    element.FindPropertyRelative("_localPath").stringValue = parts.Length > 1 ? parts[1] : "*";
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configurator);
            AssetDatabase.SaveAssets();

            Log($"Saved {path}\n{Describe(configurator)}");
            Log(configurator.Validate() ? "Target is complete and ready to build." : "Target saved, but it is not ready to build yet. See the errors above.");
        }

        private static void Run(Func<DeploymentConfigurator, bool> step)
        {
            DeploymentConfigurator configurator = LoadTarget();

            if (configurator == null)
            {
                return;
            }

            if (!step(configurator))
            {
                Fail("Failed. See the errors above.");
                return;
            }

            Log("Done.");
        }

        private static DeploymentConfigurator LoadTarget()
        {
            string path = GetSingle(ParseArguments(), "deploymentTarget");

            if (string.IsNullOrEmpty(path))
            {
                Fail("-deploymentTarget <Assets/Folder/Name.asset> is required.");
                return null;
            }

            DeploymentConfigurator configurator = AssetDatabase.LoadAssetAtPath<DeploymentConfigurator>(path);

            if (configurator == null)
            {
                Fail($"No deployment target found at {path}.");
                return null;
            }

            Log($"Using {path}\n{Describe(configurator)}");
            return configurator;
        }

        private static bool ReportUploadScript(DeploymentConfigurator configurator)
        {
            string scriptPath = Deployer.GetUploadScriptPath(configurator);
            Log($"Upload script written to {scriptPath}\nRun it with: sh '{scriptPath}'");
            return true;
        }

        private static string Describe(DeploymentConfigurator configurator)
        {
            List<string> depots = new List<string>();

            foreach (Depot depot in configurator.Depots)
            {
                depots.Add($"{depot.DepotId} ({depot.LocalPath})");
            }

            return $"  Build target:    {configurator.BuildTarget}\n" +
                   $"  App name:        {configurator.AppName}\n" +
                   $"  Description:     {configurator.BuildDescription}\n" +
                   $"  Steam username:  {configurator.SteamUsername}\n" +
                   $"  App ID:          {configurator.AppId}\n" +
                   $"  Depots:          {string.Join(", ", depots)}\n" +
                   $"  Set live branch: {(string.IsNullOrEmpty(configurator.SetLiveBranch) ? "(none)" : configurator.SetLiveBranch)}\n" +
                   $"  SDK path:        {configurator.SdkPath}\n" +
                   $"  Executable:      {configurator.ExecutablePath}";
        }

        // Collects "-key value" pairs. A key without a value gets "true". Keys may repeat.
        private static Dictionary<string, List<string>> ParseArguments()
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string[] argv = Environment.GetCommandLineArgs();

            for (int i = 0; i < argv.Length; i++)
            {
                if (!IsKey(argv[i]))
                {
                    continue;
                }

                string key = argv[i].Substring(1);
                string value = "true";

                if (i + 1 < argv.Length && !IsKey(argv[i + 1]))
                {
                    value = argv[++i];
                }

                if (!result.TryGetValue(key, out List<string> values))
                {
                    values = new List<string>();
                    result[key] = values;
                }

                values.Add(value);
            }

            return result;
        }

        private static bool IsKey(string argument)
        {
            return argument.Length > 1 && argument.StartsWith("-", StringComparison.Ordinal);
        }

        private static string GetSingle(Dictionary<string, List<string>> args, string key)
        {
            return args.TryGetValue(key, out List<string> values) ? values[values.Count - 1] : null;
        }

        private static bool TryGetSingle(Dictionary<string, List<string>> args, string key, out string value)
        {
            value = GetSingle(args, key);
            return value != null;
        }

        private static void SetString(SerializedObject serialized, Dictionary<string, List<string>> args, string key, string property)
        {
            if (TryGetSingle(args, key, out string value))
            {
                serialized.FindProperty(property).stringValue = value;
            }
        }

        private static void SetBool(SerializedObject serialized, Dictionary<string, List<string>> args, string key, string property)
        {
            if (!TryGetSingle(args, key, out string value))
            {
                return;
            }

            if (!bool.TryParse(value, out bool parsed))
            {
                Fail($"-{key} must be true or false, got '{value}'.");
                return;
            }

            serialized.FindProperty(property).boolValue = parsed;
        }

        private static void Log(string message)
        {
            Debug.Log(Prefix + message);
        }

        private static void Fail(string message)
        {
            Debug.LogError(Prefix + message);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
