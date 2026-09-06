using System;
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

            DeploymentConfigurator configurator = (DeploymentConfigurator)target;

            GUILayout.Space(10);
            DrawSdkPathField();
            GUILayout.Space(10);

            if (GUILayout.Button("Generate Build"))
            {
                Defer(() => Deployer.Build(configurator));
            }

            if (GUILayout.Button("Upload"))
            {
                Defer(() => Upload(configurator));
            }

            if (GUILayout.Button("Build and Upload"))
            {
                Defer(() =>
                {
                    if (Deployer.Build(configurator))
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

        private static void Upload(DeploymentConfigurator configurator)
        {
            if (Deployer.PrepareUpload(configurator))
            {
                Deployer.RunUploadInTerminal(configurator);
            }
        }
    }
}
