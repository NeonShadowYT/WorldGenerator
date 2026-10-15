#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NeonImperium.WorldGenerations
{
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MinMaxRangeAttribute rangeAttribute = (MinMaxRangeAttribute)attribute;
            
            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 range = property.vector2Value;
                float min = range.x;
                float max = range.y;
                
                EditorGUI.BeginChangeCheck();
                
                Rect controlRect = EditorGUI.PrefixLabel(position, new GUIContent(label.text, label.tooltip));
                
                const float fieldWidth = 60f;
                const float spacing = 6f;
                float minRequiredWidth = fieldWidth * 2f + spacing * 2f + 20f;
                bool useTwoLines = controlRect.width < minRequiredWidth;
                
                if (useTwoLines)
                {
                    float y = position.y + EditorGUIUtility.singleLineHeight + 2f;
                    float totalWidth = position.width;
                    float fieldWidthTwo = Mathf.Min(fieldWidth, 50f);
                    float sliderWidth = totalWidth - fieldWidthTwo * 2f - spacing * 2f;
                    if (sliderWidth < 20f) sliderWidth = 20f;
                    
                    Rect minFieldRect = new Rect(position.x, y, fieldWidthTwo, EditorGUIUtility.singleLineHeight);
                    Rect sliderRect = new Rect(position.x + fieldWidthTwo + spacing, y, sliderWidth, EditorGUIUtility.singleLineHeight);
                    Rect maxFieldRect = new Rect(position.x + totalWidth - fieldWidthTwo, y, fieldWidthTwo, EditorGUIUtility.singleLineHeight);
                    
                    min = EditorGUI.FloatField(minFieldRect, min);
                    max = EditorGUI.FloatField(maxFieldRect, max);
                    EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, rangeAttribute.Min, rangeAttribute.Max);
                }
                else
                {
                    Rect minFieldRect = new Rect(controlRect.x, controlRect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
                    Rect sliderRect = new Rect(controlRect.x + fieldWidth + spacing, controlRect.y, controlRect.width - fieldWidth * 2f - spacing * 2f, EditorGUIUtility.singleLineHeight);
                    Rect maxFieldRect = new Rect(controlRect.x + controlRect.width - fieldWidth, controlRect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
                    
                    min = EditorGUI.FloatField(minFieldRect, min);
                    max = EditorGUI.FloatField(maxFieldRect, max);
                    EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, rangeAttribute.Min, rangeAttribute.Max);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    range.x = Mathf.Clamp(min, rangeAttribute.Min, rangeAttribute.Max);
                    range.y = Mathf.Clamp(max, rangeAttribute.Min, rangeAttribute.Max);
                    if (range.x > range.y) range.x = range.y;
                    property.vector2Value = range;
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use MinMaxRange with Vector2.");
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float viewWidth = EditorGUIUtility.currentViewWidth;
            float labelWidth = EditorGUIUtility.labelWidth;
            float controlWidth = viewWidth - labelWidth - 20f;
            if (controlWidth < 200f)
                return EditorGUIUtility.singleLineHeight * 2f + 2f;
            else
                return EditorGUIUtility.singleLineHeight;
        }
    }

    [CustomPropertyDrawer(typeof(MinMaxRangeIntAttribute))]
    public class MinMaxRangeIntDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MinMaxRangeIntAttribute rangeAttribute = (MinMaxRangeIntAttribute)attribute;
            
            if (property.propertyType == SerializedPropertyType.Vector2Int)
            {
                Vector2Int range = property.vector2IntValue;
                int min = range.x;
                int max = range.y;
                
                EditorGUI.BeginChangeCheck();
                
                Rect controlRect = EditorGUI.PrefixLabel(position, new GUIContent(label.text, label.tooltip));
                
                const float fieldWidth = 60f;
                const float spacing = 6f;
                float minRequiredWidth = fieldWidth * 2f + spacing * 2f + 20f;
                bool useTwoLines = controlRect.width < minRequiredWidth;
                
                if (useTwoLines)
                {
                    float y = position.y + EditorGUIUtility.singleLineHeight + 2f;
                    float totalWidth = position.width;
                    float fieldWidthTwo = Mathf.Min(fieldWidth, 50f);
                    float sliderWidth = totalWidth - fieldWidthTwo * 2f - spacing * 2f;
                    if (sliderWidth < 20f) sliderWidth = 20f;
                    
                    Rect minFieldRect = new Rect(position.x, y, fieldWidthTwo, EditorGUIUtility.singleLineHeight);
                    Rect sliderRect = new Rect(position.x + fieldWidthTwo + spacing, y, sliderWidth, EditorGUIUtility.singleLineHeight);
                    Rect maxFieldRect = new Rect(position.x + totalWidth - fieldWidthTwo, y, fieldWidthTwo, EditorGUIUtility.singleLineHeight);
                    
                    min = EditorGUI.IntField(minFieldRect, min);
                    max = EditorGUI.IntField(maxFieldRect, max);
                    
                    float minFloat = min;
                    float maxFloat = max;
                    EditorGUI.MinMaxSlider(sliderRect, ref minFloat, ref maxFloat, rangeAttribute.Min, rangeAttribute.Max);
                    
                    min = Mathf.RoundToInt(minFloat);
                    max = Mathf.RoundToInt(maxFloat);
                }
                else
                {
                    Rect minFieldRect = new Rect(controlRect.x, controlRect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
                    Rect sliderRect = new Rect(controlRect.x + fieldWidth + spacing, controlRect.y, controlRect.width - fieldWidth * 2f - spacing * 2f, EditorGUIUtility.singleLineHeight);
                    Rect maxFieldRect = new Rect(controlRect.x + controlRect.width - fieldWidth, controlRect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
                    
                    min = EditorGUI.IntField(minFieldRect, min);
                    max = EditorGUI.IntField(maxFieldRect, max);
                    
                    float minFloat = min;
                    float maxFloat = max;
                    EditorGUI.MinMaxSlider(sliderRect, ref minFloat, ref maxFloat, rangeAttribute.Min, rangeAttribute.Max);
                    
                    min = Mathf.RoundToInt(minFloat);
                    max = Mathf.RoundToInt(maxFloat);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    range.x = Mathf.Clamp(min, rangeAttribute.Min, rangeAttribute.Max);
                    range.y = Mathf.Clamp(max, rangeAttribute.Min, rangeAttribute.Max);
                    if (range.x > range.y) range.x = range.y;
                    property.vector2IntValue = range;
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use MinMaxRangeInt with Vector2Int.");
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float viewWidth = EditorGUIUtility.currentViewWidth;
            float labelWidth = EditorGUIUtility.labelWidth;
            float controlWidth = viewWidth - labelWidth - 20f;
            if (controlWidth < 200f)
                return EditorGUIUtility.singleLineHeight * 2f + 2f;
            else
                return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif