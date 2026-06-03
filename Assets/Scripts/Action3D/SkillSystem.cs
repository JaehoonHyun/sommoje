using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sommoje.Action3D
{
    /// <summary>
    /// QWER 스킬 시스템. 기력(마나, 시간에 따라 회복)을 소모해 스킬 사용.
    /// Q 대시 / W 화살 / E 강타(주변) / R 궁극기(광역). 하단에 스킬바 + 기력바(IMGUI).
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        class Skill
        {
            public string name; public KeyCode key; public float cost, cooldown, cdLeft;
            public Skill(string n, KeyCode k, float c, float cd) { name = n; key = k; cost = c; cooldown = cd; }
        }

        public float maxMana = 10f;
        public float mana = 6f;
        public float manaRegen = 1.6f;

        public float maxHealth = 100f;
        public float health = 100f;   // 초당

        PlayerController3D _player;
        List<Skill> _skills;

        void Start()
        {
            _player = GetComponent<PlayerController3D>();
            if (_player == null) _player = Object.FindFirstObjectByType<PlayerController3D>();
            _skills = new List<Skill>
            {
                new("대시",   KeyCode.Q, 2f, 1.5f),
                new("화살",   KeyCode.W, 2f, 0.8f),
                new("강타",   KeyCode.E, 4f, 3.5f),
                new("궁극기", KeyCode.R, 8f, 10f),
            };
        }

        float _basicCd;

        void Update()
        {
            mana = Mathf.Min(maxMana, mana + manaRegen * Time.deltaTime);
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].cdLeft > 0f) _skills[i].cdLeft -= Time.deltaTime;
                if (Input.GetKeyDown(_skills[i].key)) TryCast(i);
            }

            if (_basicCd > 0f) _basicCd -= Time.deltaTime;

            // 우클릭: 탭(짧고 안 움직임)=기본공격 / 드래그=카메라 회전(ThirdPersonCamera 담당)
            if (Input.GetMouseButtonDown(1))
            {
                _rmbDownPos = Input.mousePosition;
                _rmbDownTime = Time.time;
                _rmbTarget = EnemyUnderCursor();
            }
            if (Input.GetMouseButtonUp(1))
            {
                bool quick = Time.time - _rmbDownTime < 0.25f;
                bool still = (Input.mousePosition - _rmbDownPos).sqrMagnitude < 36f;   // <6px
                if (quick && still && _rmbTarget != null) BasicAttack(_rmbTarget);
            }
        }

        Vector3 _rmbDownPos;
        float _rmbDownTime;
        Enemy3D _rmbTarget;

        Enemy3D EnemyUnderCursor()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 200f)) return hit.collider.GetComponentInParent<Enemy3D>();
            return null;
        }

        void BasicAttack(Enemy3D e)
        {
            if (_basicCd > 0f || _player == null || e == null) return;
            var t = _player.transform;
            var dir = e.transform.position - t.position; dir.y = 0f;
            float dist = dir.magnitude;

            if (dist > 4f) { _player.MoveTo(e.transform.position); return; }   // 멀면 다가가기

            _basicCd = 0.55f;
            if (dir.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(dir);
            if (_player.TryGetComponent<MixamoCharacter>(out var mc)) mc.Attack();
            e.TakeDamage(5f);
        }

        void TryCast(int i)
        {
            var s = _skills[i];
            if (s.cdLeft > 0f || mana < s.cost || _player == null) return;
            mana -= s.cost;
            s.cdLeft = s.cooldown;
            StartCoroutine(Cast(s.name));
        }

        IEnumerator Cast(string name)
        {
            Transform t = _player.transform;
            if (_player.TryGetComponent<CharacterAnimator>(out var anim)) anim.Attack();
            if (_player.TryGetComponent<MixamoCharacter>(out var mc)) mc.Attack();
            switch (name)
            {
                case "대시":
                    for (float e = 0f; e < 0.12f; e += Time.deltaTime)
                    { _player.Dash(t.forward, 6f * Time.deltaTime / 0.12f); yield return null; }
                    break;

                case "화살":
                    yield return ProjectileFx(t.position + Vector3.up * 1f + t.forward, t.forward, 6f, new Color(1f, 0.85f, 0.3f));
                    break;

                case "강타":
                    DamageInRadius(t.position, 4f, 9f);
                    yield return RingFx(t.position, 4f, new Color(1f, 0.5f, 0.2f));
                    break;

                case "궁극기":
                    DamageInRadius(t.position, 8f, 18f);
                    yield return RingFx(t.position, 8f, new Color(0.5f, 0.4f, 1f));
                    break;
            }
        }

        // ───────────────────────── 효과 ─────────────────────────

        void DamageInRadius(Vector3 c, float r, float dmg)
        {
            foreach (var e in Object.FindObjectsByType<Enemy3D>(FindObjectsSortMode.None))
            {
                var p = e.transform.position; p.y = c.y;
                if (Vector3.Distance(p, c) <= r) e.TakeDamage(dmg);
            }
        }

        IEnumerator ProjectileFx(Vector3 from, Vector3 dir, float dmg, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.45f;
            go.GetComponent<Renderer>().material = Mat(col);

            float life = 2f, spd = 20f; bool hit = false;
            while (life > 0f && !hit)
            {
                life -= Time.deltaTime;
                go.transform.position += dir * spd * Time.deltaTime;
                foreach (var e in Object.FindObjectsByType<Enemy3D>(FindObjectsSortMode.None))
                    if (Vector3.Distance(e.transform.position + Vector3.up, go.transform.position) < 1.1f)
                    { e.TakeDamage(dmg); hit = true; break; }
                yield return null;
            }
            Destroy(go);
        }

        IEnumerator RingFx(Vector3 pos, float radius, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos + Vector3.up * 0.05f;
            go.GetComponent<Renderer>().material = Mat(col);

            float t = 0f, dur = 0.35f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(0.5f, radius * 2f, t / dur);
                go.transform.localScale = new Vector3(s, 0.03f, s);
                yield return null;
            }
            Destroy(go);
        }

        Material Mat(Color c)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = c;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.6f);
            return m;
        }

        // ───────────────────────── HUD (IMGUI, 상품화 스타일) ─────────────────────────

        static Texture2D _white, _round;
        static readonly Color[] SkillColors =
        {
            new(0.35f, 0.85f, 1f),   // Q 시안
            new(1f, 0.85f, 0.35f),   // W 노랑
            new(1f, 0.55f, 0.25f),   // E 주황
            new(0.7f, 0.45f, 1f),    // R 보라
        };

        void OnGUI()
        {
            DrawFloatingBars();
            DrawSkillBar();
            GUI.color = Color.white;
        }

        // 캐릭터 머리 위 체력/기력 바 (캐릭터 폭에 맞춰 얇게)
        void DrawFloatingBars()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 head = transform.position + Vector3.up * 1.2f;
            Vector3 sp = cam.WorldToScreenPoint(head);
            if (sp.z <= 0f) return;

            // 캐릭터 폭(반폭 0.45유닛)을 화면 픽셀로 환산
            Vector3 spR = cam.WorldToScreenPoint(head + cam.transform.right * 0.45f);
            float w = Mathf.Clamp(Mathf.Abs(spR.x - sp.x) * 2f, 26f, 90f);

            float hpH = 4f, mpH = 3f, gap = 1.5f;
            float x = sp.x - w / 2f, y = Screen.height - sp.y;
            Bar(new Rect(x, y, w, hpH), health / maxHealth, new Color(0.88f, 0.24f, 0.22f));
            Bar(new Rect(x, y + hpH + gap, w, mpH), mana / maxMana, new Color(0.30f, 0.70f, 1f));
        }

        void DrawSkillBar()
        {
            if (_skills == null) return;
            int n = _skills.Count;
            float sz = 38f, gap = 6f;
            float total = n * sz + (n - 1) * gap;
            float sx = Screen.width - total - 18f;   // 우측 하단
            float sy = Screen.height - sz - 14f;

            for (int i = 0; i < n; i++)
            {
                var s = _skills[i];
                var box = new Rect(sx + i * (sz + gap), sy, sz, sz);
                var col = SkillColors[i % SkillColors.Length];
                bool ready = s.cdLeft <= 0f && mana >= s.cost;

                if (ready) Draw(Expand(box, 4f), Round(), new Color(col.r, col.g, col.b, 0.5f));   // 글로우
                Draw(box, Round(), new Color(0.10f, 0.12f, 0.17f, 0.95f));   // 슬롯

                var keyCol = ready ? col : new Color(col.r, col.g, col.b, 0.5f);
                Label(box, s.key.ToString(), 18, FontStyle.Bold, keyCol);    // QWER 글자만

                if (s.cdLeft > 0f)
                {
                    float r = s.cdLeft / s.cooldown;   // 아래→위 차오름
                    Draw(new Rect(box.x, box.y, box.width, box.height * r), White(), new Color(0, 0, 0, 0.62f));
                    Label(box, $"{s.cdLeft:0.0}", 14, FontStyle.Bold, Color.white);
                    continue;
                }
                if (mana < s.cost) Draw(box, White(), new Color(0.25f, 0f, 0f, 0.45f));   // 기력 부족
            }
        }

        void Bar(Rect r, float ratio, Color fill)
        {
            Draw(Expand(r, 1.5f), White(), new Color(0, 0, 0, 0.75f));               // 테두리
            Draw(r, White(), new Color(0.06f, 0.06f, 0.08f, 0.9f));                  // 배경
            Draw(new Rect(r.x, r.y, r.width * Mathf.Clamp01(ratio), r.height), White(), fill);
        }

        static void Draw(Rect r, Texture2D t, Color c)
        {
            var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, t); GUI.color = g;
        }

        static void Label(Rect r, string txt, int size, FontStyle fs, Color col)
        {
            var st = new GUIStyle(GUI.skin.label)
            { fontSize = size, fontStyle = fs, alignment = TextAnchor.MiddleCenter, normal = { textColor = col } };
            GUI.Label(r, txt, st);
        }

        static Rect Expand(Rect r, float p) => new(r.x - p, r.y - p, r.width + 2 * p, r.height + 2 * p);

        static Texture2D White()
        {
            if (_white != null) return _white;
            _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply();
            return _white;
        }

        static Texture2D Round()
        {
            if (_round != null) return _round;
            const int S = 64; const float rad = 14f;
            _round = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Max(rad - x, x - (S - 1 - rad), 0f);
                    float dy = Mathf.Max(rad - y, y - (S - 1 - rad), 0f);
                    float a = Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    _round.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            _round.Apply();
            return _round;
        }
    }
}
