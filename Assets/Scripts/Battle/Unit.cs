using UnityEngine;

namespace Sommoje.Battle
{
    public enum Team { Player, Enemy }

    /// <summary>전장 위의 한 캐릭터. 위치/이동력/팀/전투스탯/HP를 가진다.</summary>
    public class Unit : MonoBehaviour
    {
        public Vector2Int Cell;
        public int moveRange = 4;
        public Team team = Team.Player;
        public Color color = Color.white;

        public int MaxHp = 10;
        public int Hp { get; private set; }
        public int attackPower = 4;
        public int attackRange = 1;   // 맨해튼 거리
        public bool ranged = false;   // true면 투사체 연출

        public string skillName = "스킬";
        public int skillCost = 3;
        public int SkillDamage => attackPower + 4;

        public bool HasActed { get; private set; }

        SpriteRenderer _sr;
        Transform _hpFill;

        public void Init(BattleGrid grid, Vector2Int cell, Team team, Color col, int move,
                         int hp, int atk, int atkRange, bool ranged, string spriteName = null)
        {
            this.team = team;
            color = col;
            moveRange = move;
            MaxHp = hp; Hp = hp;
            attackPower = atk;
            attackRange = atkRange;
            this.ranged = ranged;

            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = UnitSprite.Load(spriteName);
            _sr.sortingOrder = 5;

            BuildHealthBar();
            SetActed(false);
            PlaceAt(grid, cell);
        }

        public void PlaceAt(BattleGrid grid, Vector2Int cell)
        {
            Cell = cell;
            transform.position = grid.CellCenterWorld(cell);
        }

        /// <summary>피해를 입는다. 죽으면 true.</summary>
        public bool TakeDamage(int dmg)
        {
            Hp = Mathf.Max(0, Hp - dmg);
            UpdateHealthBar();
            return Hp <= 0;
        }

        /// <summary>행동완료 표시(완료된 유닛은 어둡게).</summary>
        public void SetActed(bool acted)
        {
            HasActed = acted;
            if (_sr != null)
                _sr.color = acted ? color * new Color(0.45f, 0.45f, 0.45f, 1f) : color;
        }

        // ───────────────────────── 체력바 ─────────────────────────

        void BuildHealthBar()
        {
            var root = new GameObject("HP").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(-0.4f, 0.42f, 0f);   // 왼쪽 정렬 시작점

            MakeBar(root, new Color(0f, 0f, 0f, 0.65f), 6, 0.8f);  // 배경
            _hpFill = MakeBar(root, new Color(0.30f, 0.90f, 0.35f, 1f), 7, 0.8f).transform;
        }

        SpriteRenderer MakeBar(Transform parent, Color col, int order, float width)
        {
            var go = new GameObject("bar");
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(width, 0.12f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UnitSprite.Bar();   // 좌측 피벗
            sr.color = col;
            sr.sortingOrder = order;
            return sr;
        }

        void UpdateHealthBar()
        {
            if (_hpFill == null) return;
            float ratio = MaxHp <= 0 ? 0f : (float)Hp / MaxHp;
            _hpFill.localScale = new Vector3(0.8f * ratio, 0.12f, 1f);
        }
    }

    /// <summary>유닛용 스프라이트(리소스 로드 또는 코드 생성)를 캐싱한다.</summary>
    static class UnitSprite
    {
        static Sprite _circle, _bar;
        static readonly System.Collections.Generic.Dictionary<string, Sprite> _cache = new();

        /// <summary>Resources/Kenney/&lt;name&gt; 스프라이트 로드. 실패하면 원으로 폴백.</summary>
        public static Sprite Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return Circle();
            if (_cache.TryGetValue(name, out var s)) return s ? s : Circle();
            s = Resources.Load<Sprite>("Kenney/" + name);
            _cache[name] = s;
            return s ? s : Circle();
        }

        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            const int S = 48;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float cx = (S - 1) / 2f, cy = (S - 1) / 2f, r = S * 0.42f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(r - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), pixelsPerUnit: S);
            return _circle;
        }

        /// <summary>흰색 1유닛 사각형, 피벗 왼쪽(0,0.5) — 체력바 채움용.</summary>
        public static Sprite Bar()
        {
            if (_bar != null) return _bar;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var px = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(px);
            tex.Apply();
            _bar = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0f, 0.5f), pixelsPerUnit: 2);
            return _bar;
        }
    }
}
