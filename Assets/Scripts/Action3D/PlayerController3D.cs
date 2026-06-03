using UnityEngine;

namespace Sommoje.Action3D
{
    /// <summary>
    /// 클릭 이동 캐릭터(로스트아크/디아블로식). 좌클릭한 바닥 지점으로 이동.
    /// QWER은 스킬 전용이라 키 충돌 없음. 우클릭 드래그는 카메라(ThirdPersonCamera) 회전.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController3D : MonoBehaviour
    {
        public float walkSpeed = 2f;
        public float runSpeed = 5f;
        public float runDistance = 10f;   // 목적지가 이보다 멀면 달리기
        public float decelDist = 2.5f;    // 도착 직전 감속 거리
        public float jumpForce = 9f;
        public float gravity = -22f;
        public float rotSpeed = 900f;
        public float stopDist = 0.15f;

        CharacterController _cc;
        Camera _cam;
        Transform _marker;
        Vector3 _dest;
        bool _hasDest;
        bool _running;   // Space 토글 (기본 걷기)
        float _vy;

        public bool IsRunning => _running;

        void Start()
        {
            _cc = GetComponent<CharacterController>();
            _cam = Camera.main;
            _dest = transform.position;
            _marker = MakeMarker();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) TrySetDestination();
            if (Input.GetKeyDown(KeyCode.LeftShift)) _running = !_running;   // Shift = 걷기↔달리기 토글
            if (Input.GetKeyDown(KeyCode.Space) && _cc.isGrounded)          // Space = 점프
            {
                _vy = jumpForce;
                if (TryGetComponent<MixamoCharacter>(out var mc)) mc.Jump();
            }

            Vector3 horiz = Vector3.zero;
            if (_hasDest)
            {
                Vector3 to = _dest - transform.position; to.y = 0f;
                float remaining = to.magnitude;
                if (remaining <= stopDist) _hasDest = false;
                else
                {
                    Vector3 dir = to.normalized;
                    float ts = _running ? runSpeed : walkSpeed;   // Space 토글에 따라
                    if (remaining < decelDist) ts = Mathf.Lerp(walkSpeed * 0.4f, ts, remaining / decelDist);
                    horiz = dir * ts;
                    var target = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotSpeed * Time.deltaTime);
                }
            }

            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            _vy += gravity * Time.deltaTime;

            Vector3 vel = horiz; vel.y = _vy;
            _cc.Move(vel * Time.deltaTime);

            if (_marker != null)
            {
                _marker.gameObject.SetActive(_hasDest);
                if (_hasDest) _marker.position = _dest + Vector3.up * 0.03f;
            }
        }

        void TrySetDestination()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 300f))
            {
                _dest = hit.point;
                _hasDest = true;
            }
        }

        Transform MakeMarker()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = "ClickMarker";
            go.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);
            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.95f, 0.4f) };
            go.SetActive(false);
            return go.transform;
        }

        /// <summary>스킬 대시용 순간 이동.</summary>
        public void Dash(Vector3 dir, float dist)
        {
            _cc.Move(dir.normalized * dist);
        }

        /// <summary>지정 지점으로 이동 명령(적 클릭 시 다가가기 등).</summary>
        public void MoveTo(Vector3 p)
        {
            _dest = p;
            _hasDest = true;
        }
    }
}
