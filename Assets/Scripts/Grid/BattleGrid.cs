using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sommoje.Battle
{
    /// <summary>전장의 기후. 월드맵 위도에 따라 결정된다(북/남극=Cold, 적도=Hot).</summary>
    public enum Climate { Cold, Temperate, Hot }

    /// <summary>
    /// 전투 Scene의 격자 토대. 타일맵을 코드로 생성하고,
    /// 셀 ↔ 월드좌표 변환, 셀 강조(이동범위 등) 기능을 제공한다.
    /// [ExecuteAlways] 라서 Play 전 편집 화면에서도 격자가 보인다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class BattleGrid : MonoBehaviour
    {
        [Header("격자 크기 (셀 단위)")]
        public int width = 12;
        public int height = 8;

        [Header("기후 / 지구 느낌")]
        public Climate climate = Climate.Temperate;

        [Tooltip("켜면 전장 안에서도 위도 그라데이션을 보여준다(아래/위=극지방 눈, 가운데=적도 사막). " +
                 "실전에서는 보통 끄고 월드맵 위치의 단일 기후를 쓴다.")]
        public bool latitudeGradient = false;

        Grid _grid;
        Tilemap _ground;     // 바닥 지형
        Tilemap _overlay;    // 강조 표시(이동범위/공격범위)
        Tile _cellTile;

        public int Width => width;
        public int Height => height;

        void OnEnable() => Rebuild();

#if UNITY_EDITOR
        void OnValidate()
        {
            // 인스펙터에서 값 바꾸면 갱신 (OnValidate 중 직접 생성 금지 → 지연 호출)
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) Rebuild();
            };
        }
#endif

        // ───────────────────────── 생성 ─────────────────────────

        public void Rebuild()
        {
            BuildHierarchy();
            GenerateCellTile();
            PaintGround();
        }

        void BuildHierarchy()
        {
            _grid = GetComponent<Grid>();
            if (_grid == null) _grid = gameObject.AddComponent<Grid>();
            _grid.cellSize = new Vector3(1f, 1f, 0f);

            _ground = GetOrCreateTilemap("Ground", sortingOrder: 0);
            _overlay = GetOrCreateTilemap("Overlay", sortingOrder: 1);
            _ground.ClearAllTiles();
            _overlay.ClearAllTiles();
        }

        Tilemap GetOrCreateTilemap(string layerName, int sortingOrder)
        {
            var existing = transform.Find(layerName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(layerName);
                go.transform.SetParent(transform, false);
                // 코드로 매번 다시 만드는 자식 → 씬에 저장하지 않는다(중복 방지)
                go.hideFlags = HideFlags.DontSave;
            }
            var tm = go.GetComponent<Tilemap>();
            if (tm == null) tm = go.AddComponent<Tilemap>();
            var tr = go.GetComponent<TilemapRenderer>();
            if (tr == null) tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortingOrder;
            return tm;
        }

        /// <summary>흰색 + 어두운 테두리를 가진 1셀 스프라이트를 코드로 생성한다(격자선 효과).</summary>
        void GenerateCellTile()
        {
            const int S = 32;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var fill = Color.white;
            var border = new Color(0f, 0f, 0f, 0.25f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool edge = x == 0 || y == 0 || x == S - 1 || y == S - 1;
                    tex.SetPixel(x, y, edge ? border : fill);
                }
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), pixelsPerUnit: S);
            _cellTile = ScriptableObject.CreateInstance<Tile>();
            _cellTile.hideFlags = HideFlags.DontSave;
            _cellTile.sprite = sprite;
            _cellTile.colliderType = Tile.ColliderType.None;
        }

        void PaintGround()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var p = new Vector3Int(x, y, 0);
                    _ground.SetTile(p, _cellTile);
                    _ground.SetTileFlags(p, TileFlags.None);   // 셀별 색칠 허용
                    _ground.SetColor(p, GroundColor(x, y));
                }
        }

        // ───────────────────────── 기후 색 ─────────────────────────

        Climate ClimateAt(int y)
        {
            if (!latitudeGradient) return climate;
            float t = height <= 1 ? 0.5f : (float)y / (height - 1); // 0=아래 .. 1=위
            float distFromEquator = Mathf.Abs(t - 0.5f) * 2f;       // 0=적도 .. 1=극지방
            if (distFromEquator > 0.66f) return Climate.Cold;
            if (distFromEquator < 0.33f) return Climate.Hot;
            return Climate.Temperate;
        }

        Color GroundColor(int x, int y)
        {
            Color baseCol = ClimateAt(y) switch
            {
                Climate.Cold => new Color(0.80f, 0.88f, 0.95f),  // 눈/얼음
                Climate.Hot  => new Color(0.90f, 0.82f, 0.55f),  // 사막
                _            => new Color(0.55f, 0.78f, 0.45f),  // 평원
            };
            float shade = ((x + y) & 1) == 0 ? 1f : 0.92f;       // 체커보드 명암
            return baseCol * shade;
        }

        // ───────────────────────── 공개 API (다음 단계용) ─────────────────────────

        public bool InBounds(Vector2Int c) =>
            c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

        public Vector3 CellCenterWorld(Vector2Int c) =>
            _grid.GetCellCenterWorld(new Vector3Int(c.x, c.y, 0));

        public Vector2Int WorldToCell(Vector3 world)
        {
            var c = _grid.WorldToCell(world);
            return new Vector2Int(c.x, c.y);
        }

        public void ClearHighlights() => _overlay.ClearAllTiles();

        /// <summary>지정한 셀들을 색으로 강조(이동/공격 범위 표시에 사용).</summary>
        public void Highlight(IEnumerable<Vector2Int> cells, Color color)
        {
            foreach (var c in cells)
            {
                if (!InBounds(c)) continue;
                var p = new Vector3Int(c.x, c.y, 0);
                _overlay.SetTile(p, _cellTile);
                _overlay.SetTileFlags(p, TileFlags.None);
                _overlay.SetColor(p, color);
            }
        }
    }
}
