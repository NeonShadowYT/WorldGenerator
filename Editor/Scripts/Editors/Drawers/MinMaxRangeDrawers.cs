#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NeonImperium.WorldGeneration
{
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rangeAttribute = (MinMaxRangeAttribute)attribute;
            
            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 range = property.vector2Value;
                float min = range.x;
                float max = range.y;
                
                EditorGUI.BeginChangeCheck();
                
                // Первая строка: основная метка с текущими значениями
                Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                string displayText = $"{label.text} ({min.ToString(rangeAttribute.Format)} - {max.ToString(rangeAttribute.Format)})";
                EditorGUI.LabelField(labelRect, displayText);
                
                // Вторая строка: поля ввода
                Rect minRect = new Rect(position.x, position.y + 20, position.width / 2 - 5, EditorGUIUtility.singleLineHeight);
                Rect maxRect = new Rect(position.x + position.width / 2 + 5, position.y + 20, position.width / 2 - 5, EditorGUIUtility.singleLineHeight);
                
                min = EditorGUI.FloatField(minRect, min);
                max = EditorGUI.FloatField(maxRect, max);
                
                // Третья строка: слайдер
                Rect sliderRect = new Rect(position.x, position.y + 40, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, rangeAttribute.Min, rangeAttribute.Max);
                
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
            return EditorGUIUtility.singleLineHeight * 3;
        }
    }

    [CustomPropertyDrawer(typeof(MinMaxRangeIntAttribute))]
    public class MinMaxRangeIntDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rangeAttribute = (MinMaxRangeIntAttribute)attribute;
            
            if (property.propertyType == SerializedPropertyType.Vector2Int)
            {
                Vector2Int range = property.vector2IntValue;
                int min = range.x;
                int max = range.y;
                
                EditorGUI.BeginChangeCheck();
                
                // Первая строка: основная метка с текущими значениями
                Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                string displayText = $"{label.text} ({min} - {max})";
                EditorGUI.LabelField(labelRect, displayText);
                
                // Вторая строка: поля ввода
                Rect minRect = new Rect(position.x, position.y + 20, position.width / 2 - 5, EditorGUIUtility.singleLineHeight);
                Rect maxRect = new Rect(position.x + position.width / 2 + 5, position.y + 20, position.width / 2 - 5, EditorGUIUtility.singleLineHeight);
                
                min = EditorGUI.IntField(minRect, min);
                max = EditorGUI.IntField(maxRect, max);
                
                // Третья строка: слайдер
                float minFloat = min;
                float maxFloat = max;
                Rect sliderRect = new Rect(position.x, position.y + 40, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.MinMaxSlider(sliderRect, ref minFloat, ref maxFloat, rangeAttribute.Min, rangeAttribute.Max);
                
                min = Mathf.RoundToInt(minFloat);
                max = Mathf.RoundToInt(maxFloat);
                
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
            return EditorGUIUtility.singleLineHeight * 3;
        }
    }
}
#endif