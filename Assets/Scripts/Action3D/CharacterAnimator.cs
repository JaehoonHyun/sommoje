using System.Collections;
using UnityEngine;

namespace Sommoje.Action3D
{
    /// <summary>
    /// 도형 조립 캐릭터의 팔/다리를 코드로 애니메이션.
    /// 이동 중이면 팔다리 스윙(걷기), 정지하면 원위치. Attack() 호출 시 오른팔 휘두름.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public Transform armL, armR, legL, legR;
        public float walkAmp = 48f;
        public float walkFreq = 9f;
        public float smooth = 14f;

        Vector3 _lastPos;
        float _phase;
        float _attack;   // 남은 공격모션 시간

        void Start() => _lastPos = transform.position;

        void Update()
        {
            Vector3 p = transform.position;
            Vector3 d = p - _lastPos; d.y = 0f; _lastPos = p;
            float speed = d.magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
            bool moving = speed > 0.5f;

            if (moving) _phase += walkFreq * Time.deltaTime;
            float swing = moving ? Mathf.Sin(_phase) * walkAmp : 0f;

            SetPivot(legL, swing);
            SetPivot(legR, -swing);
            SetPivot(armL, swing);

            if (_attack > 0f)
            {
                _attack -= Time.deltaTime;
                float a = Mathf.Sin(Mathf.Clamp01(1f - _attack / 0.3f) * Mathf.PI) * 115f;
                SetPivot(armR, -a, instant: true);   // 공격은 빠르게
            }
            else
            {
                SetPivot(armR, -swing);
            }
        }

        void SetPivot(Transform t, float angle, bool instant = false)
        {
            if (t == null) return;
            var target = Quaternion.Euler(angle, 0f, 0f);
            t.localRotation = instant ? target
                : Quaternion.Lerp(t.localRotation, target, smooth * Time.deltaTime);
        }

        public void Attack() => _attack = 0.3f;
    }
}
