using UnityEditor;
using UnityEngine;

namespace Codice.UI
{
    internal static class DrawActionButton
    {
        internal static bool For(string buttonText)
        {
            GUIContent buttonContent = new GUIContent(buttonText);

            GUIStyle buttonStyle = EditorStyles.miniButton;

            Rect rt = GUILayoutUtility.GetRect(
                buttonContent, buttonStyle,
                GUILayout.MinWidth(UnityConstants.REGULAR_BUTTON_WIDTH));

            return GUI.Button(rt, buttonText, buttonStyle);
        }
    }
}
