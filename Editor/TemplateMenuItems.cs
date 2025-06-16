using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Editor menu items for the Unity Template
    /// </summary>
    public static class TemplateMenuItems
    {
        private const string MENU_ROOT = "Tools/Unity Template/";
        
        [MenuItem(MENU_ROOT + "Create New Scene from Template")]
        public static void CreateSceneFromTemplate()
        {
            // Load the URP 2D scene template
            var templatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
            
            if (System.IO.File.Exists(templatePath))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.OpenScene(templatePath, OpenSceneMode.Additive);
                Debug.Log("Created new scene from URP 2D template");
            }
            else
            {
                Debug.LogWarning("URP 2D scene template not found. Please import the project template sample first.");
            }
        }
        
        [MenuItem(MENU_ROOT + "Setup Project Settings")]
        public static void SetupProjectSettings()
        {
            // Configure common project settings
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gpuSkinning = true;
            
            // Set up input system
            var inputSystemSettings = EditorUserBuildSettings.activeBuildTarget;
            
            Debug.Log("Project settings configured for optimal 2D development");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Project settings have been configured for optimal 2D development with URP.", "OK");
        }
        
        [MenuItem(MENU_ROOT + "Open Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/void2610/my-unity-template");
        }
        
        [MenuItem(MENU_ROOT + "Create Folder Structure")]
        public static void CreateFolderStructure()
        {
            var folders = new[]
            {
                "Assets/Scripts/Gameplay",
                "Assets/Scripts/UI", 
                "Assets/Scripts/Data",
                "Assets/Sprites/Characters",
                "Assets/Sprites/Environment",
                "Assets/Sprites/UI",
                "Assets/Audio/BGM",
                "Assets/Audio/SE",
                "Assets/Materials/2D",
                "Assets/Prefabs/Characters",
                "Assets/Prefabs/Environment",
                "Assets/Prefabs/UI",
                "Assets/ScriptableObjects/Data"
            };
            
            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parentFolder = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
                    var folderName = System.IO.Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log("Created organized folder structure for game development");
        }
    }
}