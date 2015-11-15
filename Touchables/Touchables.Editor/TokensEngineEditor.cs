/*
 * @author Francesco Strada
 */

using UnityEditor;
using UnityEngine;

namespace Touchables.Editor
{
    [CustomEditor(typeof(TokensEngine))]
    public class TokensEngineEditor : UnityEditor.Editor
    {
        string[] tokenTypes = { "3 x 3", "4 x 4", "5 x 5" };
        string[] distanceMetrics = { "Pixels", "Centimeters" };

        public override void OnInspectorGUI()
        {
            TokensEngine tEngine = (TokensEngine)target;

            //DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();
            tEngine.TokenType = EditorGUILayout.Popup("Token Type: ", tEngine.TokenType, tokenTypes, EditorStyles.popup);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Operation Metrics", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            tEngine.MeanSquare = EditorGUILayout.Toggle("Mean Square", tEngine.MeanSquare);
            //EditorGUIUtility.labelWidth = 80;
            tEngine.ComputePixels = EditorGUILayout.Popup("Distances: ", tEngine.ComputePixels, distanceMetrics, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Token Update Thresholds", EditorStyles.boldLabel);
            tEngine.TranslationThr = EditorGUILayout.Slider("Translation", tEngine.TranslationThr, 0.1f, 5.0f);
            tEngine.RotationThr = EditorGUILayout.Slider("Rotation", tEngine.RotationThr, 0.1f, 5.0f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            tEngine.Target60FPS = EditorGUILayout.Toggle("Target 60FPS", tEngine.Target60FPS);
            EditorGUILayout.EndHorizontal();



        }
    }
}
