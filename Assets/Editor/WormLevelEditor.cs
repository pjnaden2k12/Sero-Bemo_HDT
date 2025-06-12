using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WormLevelData))]
public class WormLevelEditor : Editor
{
    int generateCount = 3;
    Direction generateDirection = Direction.Left;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WormLevelData data = (WormLevelData)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Auto Generate Segments", EditorStyles.boldLabel);

        generateCount = EditorGUILayout.IntField("Body Count", generateCount);
        generateDirection = (Direction)EditorGUILayout.EnumPopup("Direction", generateDirection);

        if (GUILayout.Button("Generate Segments"))
        {
            data.segments.Clear();
            for (int i = 0; i < generateCount; i++)
            {
                data.segments.Add(new WormBodySegmentData(i + 1, generateDirection));
            }

            EditorUtility.SetDirty(data);
            Debug.Log($"Generated {generateCount} segments in direction {generateDirection}");
        }
    }
}
