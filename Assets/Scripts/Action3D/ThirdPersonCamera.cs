using UnityEngine;

namespace Sommoje.Action3D
{
    /// <summary>오버숄더 3인칭 카메라. 캐릭터 바로 뒤+어깨 너머. 우클릭 드래그로 회전.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 3.8f;
        public float pitch = 14f;
        public float shoulder = 0.7f;   // 어깨 오프셋(우측)
        public float height = 1.5f;
        public float mouseSpeed = 4f;
        public float followLerp = 16f;

        float _yaw;

        void Start()
        {
            if (target == null)
            {
                var p = Object.FindFirstObjectByType<PlayerController3D>();
                if (p != null) target = p.transform;
            }
            // 직렬화본을 오버숄더 값으로 덮어쓰기
            _yaw = 0f;
            pitch = 14f;
            distance = 3.8f;
            shoulder = 0.7f;
            height = 1.5f;
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (Input.GetMouseButton(1))   // 우클릭 드래그로 회전 (탭은 SkillSystem이 공격으로 처리)
                _yaw += Input.GetAxis("Mouse X") * mouseSpeed;

            var rot = Quaternion.Euler(pitch, _yaw, 0f);
            Vector3 focus = target.position + Vector3.up * height + (rot * Vector3.right) * shoulder;
            Vector3 desired = focus - (rot * Vector3.forward) * distance;

            transform.position = Vector3.Lerp(transform.position, desired, followLerp * Time.deltaTime);
            transform.rotation = rot;
        }
    }
}
