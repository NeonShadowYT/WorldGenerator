using UnityEngine;

namespace NeonImperium.WorldGeneration
{
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }
        public string Format { get; private set; }
        
        public MinMaxRangeAttribute(float min, float max, string format = "F1")
        {
            Min = min;
            Max = max;
            Format = format;
        }
    }
    
    public class MinMaxRangeIntAttribute : PropertyAttribute
    {
        public int Min { get; private set; }
        public int Max { get; private set; }
        
        public MinMaxRangeIntAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}