using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.Animations;
using UnityEditor.SceneManagement;
using VRCSDK2; // Assuming the VRC classes are in this namespace. Change to the correct namespace as needed.
using UnityEngine.Rendering; // For Light and ParticleSystemRenderer
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

public class ComponentFinder : EditorWindow
{
    private GameObject gameObjectToInspect;
    private Vector2 scrollPosition;
    private HashSet<System.Type> selectedTypes = new HashSet<System.Type>();
    private Dictionary<System.Type, Color> componentColors = new Dictionary<System.Type, Color>();
    private List<ComponentInfo> componentsFound = new List<ComponentInfo>();
    private bool searchForPrefabs = true;

    [MenuItem("Tools/Component Finder")]
    static void Init()
    {
        ComponentFinder window = (ComponentFinder)GetWindow(typeof(ComponentFinder));
        window.Show();
    }

    private void OnEnable()
    {
        // Previous component colors
        componentColors[typeof(AimConstraint)] = Color.magenta;
        componentColors[typeof(LookAtConstraint)] = Color.magenta;
        componentColors[typeof(ParentConstraint)] = Color.magenta;
        componentColors[typeof(PositionConstraint)] = Color.magenta;
        componentColors[typeof(RotationConstraint)] = Color.magenta;
        componentColors[typeof(ScaleConstraint)] = Color.magenta;
        componentColors[typeof(SkinnedMeshRenderer)] = Color.blue;
        componentColors[typeof(AudioSource)] = Color.yellow;
        componentColors[typeof(GameObject)] = Color.cyan;

        // New component colors
        componentColors[typeof(VRCPhysBoneCollider)] = Color.cyan;
        componentColors[typeof(VRCPhysBone)] = Color.cyan;
        componentColors[typeof(VRCContactSender)] = Color.cyan;
        componentColors[typeof(VRCContactReceiver)] = Color.cyan;
        componentColors[typeof(Light)] = Color.yellow;
        componentColors[typeof(ParticleSystemRenderer)] = Color.cyan;

        foreach (var componentType in componentColors.Keys)
        {
            if (componentType != typeof(GameObject) && EditorPrefs.GetBool(componentType.Name, true))
            {
                selectedTypes.Add(componentType);
            }
        }

        searchForPrefabs = EditorPrefs.GetBool("SearchForPrefabs", true);

        // Restore previously selected GameObject
        int lastGameObjectID = EditorPrefs.GetInt("LastGameObject", 0);
        if (lastGameObjectID != 0)
        {
            gameObjectToInspect = EditorUtility.InstanceIDToObject(lastGameObjectID) as GameObject;
        }
    }

    void OnGUI()
    {
        gameObjectToInspect = (GameObject)EditorGUILayout.ObjectField("Drag GameObject Here:", gameObjectToInspect, typeof(GameObject), true);

        // Store the currently selected GameObject's instance ID
        if (gameObjectToInspect != null)
        {
            EditorPrefs.SetInt("LastGameObject", gameObjectToInspect.GetInstanceID());
        }

        GUILayout.Label("Components to find:");

        // Previous checkboxes
        CheckboxForComponentType<AimConstraint>("Aim Constraints");
        CheckboxForComponentType<LookAtConstraint>("LookAt Constraints");
        CheckboxForComponentType<ParentConstraint>("Parent Constraints");
        CheckboxForComponentType<PositionConstraint>("Position Constraints");
        CheckboxForComponentType<RotationConstraint>("Rotation Constraints");
        CheckboxForComponentType<ScaleConstraint>("Scale Constraints");
        CheckboxForComponentType<SkinnedMeshRenderer>("Skinned Mesh Renderers");
        CheckboxForComponentType<AudioSource>("Audio Sources");

        // New checkboxes
        CheckboxForComponentType<VRCPhysBoneCollider>("PhysBone Collider");
        CheckboxForComponentType<VRCPhysBone>("PhysBone");
        CheckboxForComponentType<VRCContactSender>("Contact Sender");
        CheckboxForComponentType<VRCContactReceiver>("Contact Receiver");
        CheckboxForComponentType<Light>("Light");
        CheckboxForComponentType<ParticleSystemRenderer>("Particle System");

        searchForPrefabs = EditorGUILayout.Toggle("Prefabs", searchForPrefabs);
        EditorPrefs.SetBool("SearchForPrefabs", searchForPrefabs);

        if (GUILayout.Button("Find Components"))
        {
            FindComponents();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var componentInfo in componentsFound)
        {
            GUI.color = componentColors[componentInfo.componentType];
            string componentName = componentInfo.componentType == typeof(GameObject) ? "Prefab" : componentInfo.componentType.Name;

            // Use the new format for the button
            string pathName = componentInfo.gameObject.transform.parent == gameObjectToInspect.transform
                ? "AvatarRoot"
                : GetHighestParent(componentInfo.gameObject).name;
            if (GUILayout.Button($"{componentName} on {pathName}/{componentInfo.gameObject.name}"))
            {
                Selection.activeGameObject = componentInfo.gameObject;
            }
        }

        EditorGUILayout.EndScrollView();
        GUI.color = Color.white; // Reset color after you are done
    }

    private GameObject GetHighestParent(GameObject childObject)
    {
        var currentParent = childObject.transform.parent;
        while (currentParent.parent != gameObjectToInspect.transform)
        {
            currentParent = currentParent.parent;
        }
        return currentParent.gameObject;
    }

    private void CheckboxForComponentType<T>(string label) where T : Component
    {
        bool wasSelected = selectedTypes.Contains(typeof(T));
        bool isSelected = EditorGUILayout.Toggle(label, wasSelected);
        if (isSelected)
        {
            selectedTypes.Add(typeof(T));
        }
        else
        {
            selectedTypes.Remove(typeof(T));
        }
        EditorPrefs.SetBool(typeof(T).Name, isSelected);
    }

    private void FindComponents()
    {
        componentsFound.Clear();

        if (gameObjectToInspect != null)
        {
            foreach (var selectedType in selectedTypes)
            {
                var components = gameObjectToInspect.GetComponentsInChildren(selectedType);
                foreach (var component in components)
                {
                    componentsFound.Add(new ComponentInfo
                    {
                        gameObject = component.gameObject,
                        componentType = selectedType
                    });
                }
            }

            if (searchForPrefabs)
            {
                var allChildren = gameObjectToInspect.GetComponentsInChildren<Transform>();
                foreach (var child in allChildren)
                {
                    // Only add the root object of each prefab to the componentsFound list
                    if (PrefabUtility.IsPartOfPrefabInstance(child) &&
                        (child.parent == null || !PrefabUtility.IsPartOfPrefabInstance(child.parent)))
                    {
                        componentsFound.Add(new ComponentInfo
                        {
                            gameObject = child.gameObject,
                            componentType = typeof(GameObject) // representing prefab
                        });
                    }
                }
            }
        }
    }

    private class ComponentInfo
    {
        public GameObject gameObject;
        public System.Type componentType;
    }
}
