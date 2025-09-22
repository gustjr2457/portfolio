using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "CurveParameters", menuName = "Hand Curve Parameters", order = 0)]
    public class CurveParameters : ScriptableObject
    {
        public AnimationCurve positioning;
        public float positioningInfluence = .1f;
        public AnimationCurve rotation;
        public float rotationInfluence = 10f;
        public float maxRotation = 20f;
    }
}