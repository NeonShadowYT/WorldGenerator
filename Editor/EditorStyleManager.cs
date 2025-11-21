#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NeonImperium
{
    public class EditorStyleManager
    {
        public GUIStyle HeaderStyle { get; private set; }
        public GUIStyle ButtonStyle { get; private set; }
        public GUIStyle FoldoutStyle { get; private set; }
        public GUIStyle CenteredLabelStyle { get; private set; }
        public GUIStyle HelpBoxStyle { get; private set; }
        public GUIStyle MiniLabelStyle { get; private set; }
        public Texture2D SolidColorTexture { get; private set; }

        public void InitializeStyles()
        {
            HeaderStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };

            CenteredLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.BoldAndItalic,
                wordWrap = true
            };

            ButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(10, 10, 5, 5),
                fixedHeight = 30,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            FoldoutStyle ??= new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            HelpBoxStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                wordWrap = true,
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4),
                richText = true
            };

            MiniLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                padding = new RectOffset(2, 2, 0, 0)
            };
        }

        public void UpdateStyles(Color headerColor)
        {
            if (SolidColorTexture == null) SolidColorTexture = new Texture2D(1, 1);
            
            Color bgColor = new(headerColor.r, headerColor.g, headerColor.b, 0.2f);
            SolidColorTexture.SetPixel(0, 0, bgColor);
            SolidColorTexture.Apply();
            
            HeaderStyle.normal.textColor = headerColor;
            FoldoutStyle.normal.textColor = Color.Lerp(headerColor, Color.white, 0.1f);
        }
    }
}
#endif