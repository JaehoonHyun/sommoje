using UnityEngine;

namespace Sommoje.Action3D
{
    /// <summary>Mixamo 휴머노이드 모델의 Animator를 구동. 이동→걷기, Attack()→공격.</summary>
    public class MixamoCharacter : MonoBehaviour
    {
        public Animator animator;

        Vector3 _lastPos;
        float _speed;

        void Start()
        {
            _lastPos = transform.position;
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (animator == null) return;
            Vector3 p = transform.position;
            Vector3 d = p - _lastPos; d.y = 0f; _lastPos = p;
            float raw = d.magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
            _speed = Mathf.Lerp(_speed, raw, 12f * Time.deltaTime);   // 부드럽게
            animator.SetFloat("Speed", _speed);
        }

        public void Attack()
        {
            if (animator != null) animator.SetTrigger("Attack");
        }

        public void Jump()
        {
            if (animator == null) return;
            foreach (var p in animator.parameters)   // Jump 파라미터 있을 때만 (없으면 물리 점프만)
                if (p.name == "Jump") { animator.SetTrigger("Jump"); return; }
        }
    }
}
