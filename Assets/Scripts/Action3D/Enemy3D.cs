using System.Collections;
using UnityEngine;

namespace Sommoje.Action3D
{
    /// <summary>플레이어를 천천히 쫓는 더미 적. 스킬에 맞으면 HP 감소, 0이면 사라짐.</summary>
    public class Enemy3D : MonoBehaviour
    {
        public float maxHp = 24f;
        public float hp = 24f;
        public float speed = 2.2f;
        public float stopRange = 1.6f;

        Transform _player;
        Renderer[] _rends;
        Color[] _base;

        void Start()
        {
            var p = Object.FindFirstObjectByType<PlayerController3D>();
            if (p != null) _player = p.transform;
            _rends = GetComponentsInChildren<Renderer>();
            _base = new Color[_rends.Length];
            for (int i = 0; i < _rends.Length; i++) _base[i] = _rends[i].material.color;
            hp = maxHp;
        }

        void Update()
        {
            if (_player == null) return;
            Vector3 d = _player.position - transform.position;
            d.y = 0f;
            if (d.magnitude > stopRange)
            {
                transform.position += d.normalized * speed * Time.deltaTime;
                transform.forward = d.normalized;
            }

            // 지형 높이에 붙기 (캡슐 절반높이 ≈ 1)
            var t = Terrain.activeTerrain;
            if (t != null)
            {
                var p = transform.position;
                p.y = t.SampleHeight(p) + t.transform.position.y + 1f;
                transform.position = p;
            }
        }

        public void TakeDamage(float dmg)
        {
            hp -= dmg;
            if (gameObject.activeInHierarchy) StartCoroutine(HitFlash());
            if (hp <= 0f) Destroy(gameObject);
        }

        IEnumerator HitFlash()
        {
            if (_rends == null) yield break;
            foreach (var r in _rends) if (r != null) r.material.color = Color.white;
            transform.localScale *= 1.12f;
            yield return new WaitForSeconds(0.08f);
            for (int i = 0; i < _rends.Length; i++) if (_rends[i] != null) _rends[i].material.color = _base[i];
            transform.localScale /= 1.12f;
        }

        // 적 머리 위 체력바 (캐릭터 폭, 얇게)
        void OnGUI()
        {
            if (hp <= 0f) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 head = transform.position + Vector3.up * 1.3f;
            Vector3 sp = cam.WorldToScreenPoint(head);
            if (sp.z <= 0f) return;

            Vector3 spR = cam.WorldToScreenPoint(head + cam.transform.right * 0.5f);
            float w = Mathf.Clamp(Mathf.Abs(spR.x - sp.x) * 2f, 22f, 80f);
            float h = 4f, x = sp.x - w / 2f, y = Screen.height - sp.y;

            var t = Texture2D.whiteTexture;
            var g = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f); GUI.DrawTexture(new Rect(x - 1.5f, y - 1.5f, w + 3f, h + 3f), t);
            GUI.color = new Color(0.06f, 0.06f, 0.08f, 0.9f); GUI.DrawTexture(new Rect(x, y, w, h), t);
            GUI.color = new Color(0.88f, 0.24f, 0.22f); GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(hp / maxHp), h), t);
            GUI.color = g;
        }
    }
}
