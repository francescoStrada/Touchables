/*
 * @author Francesco Strada
 */

using UnityEngine;
using UnityEditor;

namespace Touchables.Editor
{
    [CustomEditor (typeof (ApplicationToken))]
    public class ApplicationTokenEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ApplicationToken appToken = (ApplicationToken)target;
            DrawDefaultInspector();
        }
    }
}
