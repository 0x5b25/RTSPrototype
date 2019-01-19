using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Test))]
public class Testvis : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Fill original info"))
            (target as Test).Init();

        if (GUILayout.Button("Deep copy test"))
            ((Test)target).Inst();

        if(((Test)target).holder != null){
            EditorGUILayout.LabelField(((Test)target).holder.someStr);

            if (GUILayout.Button("Randomize content"))
                ((Test)target).holder.someStr = "Random number:" + Random.Range(0.1f, 7.5f);
        }
    }
}