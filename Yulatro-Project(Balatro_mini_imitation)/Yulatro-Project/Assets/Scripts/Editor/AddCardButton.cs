using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(HorizontalCardHolder))]
    public class AddCardButton : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            HorizontalCardHolder cardHolder = (HorizontalCardHolder)target;
            if (GUILayout.Button("Add Card"))
            {
                GameObject obj=(GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Card.prefab",typeof(GameObject));
                cardHolder.AddCard(obj);
            }
        }
    }
}