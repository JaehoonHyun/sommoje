using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sommoje.Battle
{
    /// <summary>유닛 생성 정의.</summary>
    public struct UnitDef
    {
        public string name; public Vector2Int cell; public Team team; public Color color;
        public int move, hp, atk, atkRange; public bool ranged; public string sprite;
        public string skillName; public int skillCost;
    }

    /// <summary>
    /// 전투 진행/입력/턴/전투해결 + 기력(주사위) 시스템.
    /// 매 플레이어 턴 주사위로 기력 획득 → 기력으로 공격/스킬 사용.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public static readonly Color PlayerColor   = Color.white;
        public static readonly Color RangedColor   = Color.white;
        public static readonly Color EnemyColor    = Color.white;
        public static readonly Color MoveHighlight = new(0.25f, 0.55f, 1f, 0.45f);
        public static readonly Color AtkHighlight  = new(0.95f, 0.30f, 0.25f, 0.55f);

        const int EnergyMax = 10;
        const int AtkCost = 1;

        BattleGrid _grid;
        readonly List<Unit> _units = new();

        Unit _selected;
        bool _movedThisSelection;
        readonly List<Vector2Int> _reachable = new();
        readonly List<Unit> _targets = new();

        Team _turn = Team.Player;
        int _round = 1;
        bool _busy;
        string _result;

        int _energy;
        int _dice;        // 마지막 주사위 눈
        bool _rolling;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            if (Object.FindFirstObjectByType<BattleGrid>() == null) return;
            if (Object.FindFirstObjectByType<BattleController>() != null) return;
            new GameObject("BattleController").AddComponent<BattleController>();
        }

        void Start()
        {
            _grid = Object.FindFirstObjectByType<BattleGrid>();
            SpawnDemo(_grid, _units);
            StartTurn(Team.Player);
        }

        // ───────────────────────── 턴 ─────────────────────────

        void StartTurn(Team team)
        {
            if (_result != null) return;
            _turn = team;
            foreach (var u in _units)
                if (u.team == team) u.SetActed(false);
            Deselect();

            if (team == Team.Player) StartCoroutine(RollDice());
            else StartCoroutine(EnemyTurn());
        }

        IEnumerator RollDice()
        {
            _busy = true; _rolling = true;
            float t = 0f;
            while (t < 0.6f) { t += 0.06f; _dice = Random.Range(1, 7); yield return new WaitForSeconds(0.06f); }
            _dice = Random.Range(1, 7);
            _energy = Mathf.Min(EnergyMax, _energy + _dice);
            _rolling = false;
            yield return new WaitForSeconds(0.25f);
            _busy = false;
        }

        void EndTurn()
        {
            if (_result != null) return;
            if (_turn == Team.Player) StartTurn(Team.Enemy);
            else { _round++; StartTurn(Team.Player); }
        }

        void CheckPlayerTurnEnd()
        {
            if (_turn != Team.Player) return;
            foreach (var u in _units)
                if (u.team == Team.Player && !u.HasActed) return;
            EndTurn();
        }

        IEnumerator EnemyTurn()
        {
            _busy = true;
            yield return new WaitForSeconds(0.35f);

            foreach (var e in _units.FindAll(u => u.team == Team.Enemy))
            {
                if (_result != null) break;
                var target = NearestPlayer(e);
                if (target != null)
                {
                    MoveToward(e, target);
                    yield return new WaitForSeconds(0.2f);
                    if (Manhattan(e.Cell, target.Cell) <= e.attackRange)
                        yield return AttackRoutine(e, target, e.attackPower);
                }
                e.SetActed(true);
                yield return new WaitForSeconds(0.2f);
            }

            _busy = false;
            EndTurn();
        }

        Unit NearestPlayer(Unit from)
        {
            Unit best = null; int bestD = int.MaxValue;
            foreach (var u in _units)
                if (u.team == Team.Player)
                {
                    int d = Manhattan(from.Cell, u.Cell);
                    if (d < bestD) { bestD = d; best = u; }
                }
            return best;
        }

        void MoveToward(Unit mover, Unit target)
        {
            var blocked = Occupied(except: mover);
            Vector2Int best = mover.Cell; int bestD = Manhattan(mover.Cell, target.Cell);
            foreach (var c in Reachable(_grid, blocked, mover.Cell, mover.moveRange))
            {
                int d = Manhattan(c, target.Cell);
                if (d < bestD) { bestD = d; best = c; }
            }
            mover.PlaceAt(_grid, best);
        }

        // ───────────────────────── 입력 ─────────────────────────

        void Update()
        {
            if (_grid == null || _result != null || _busy || _turn != Team.Player) return;
            if (!Input.GetMouseButtonDown(0)) return;

            var m = Input.mousePosition;
            var gp = new Vector2(m.x, Screen.height - m.y);
            if (_endTurnRect.Contains(gp) || _waitRect.Contains(gp) || _skillRect.Contains(gp)) return;

            var cell = _grid.WorldToCell(Camera.main.ScreenToWorldPoint(m));
            if (!_grid.InBounds(cell)) { if (!_movedThisSelection) Deselect(); return; }

            // 1) 사거리 안 적 클릭 → 기본 공격(기력 1)
            var targetUnit = TargetAt(cell);
            if (_selected != null && targetUnit != null)
            {
                if (_energy >= AtkCost)
                    StartCoroutine(PlayerAttack(_selected, targetUnit, AtkCost, _selected.attackPower));
                return;
            }

            // 2) 이동 전이면 이동 가능 빈 칸 → 이동(무료)
            if (_selected != null && !_movedThisSelection &&
                _reachable.Contains(cell) && UnitAt(cell) == null)
            {
                _selected.PlaceAt(_grid, cell);
                _movedThisSelection = true;
                ShowOptions(_selected, canMove: false);
                return;
            }

            // 3) 이동 전이면 다른 내 유닛 선택/해제
            if (!_movedThisSelection)
            {
                var clicked = UnitAt(cell);
                if (clicked != null && clicked.team == Team.Player && !clicked.HasActed)
                    Select(clicked);
                else
                    Deselect();
            }
        }

        void Select(Unit u)
        {
            _selected = u;
            _movedThisSelection = false;
            ShowOptions(u, canMove: true);
        }

        void ShowOptions(Unit u, bool canMove)
        {
            _grid.ClearHighlights();
            _reachable.Clear();
            _targets.Clear();

            if (canMove)
            {
                _reachable.AddRange(Reachable(_grid, Occupied(except: u), u.Cell, u.moveRange));
                _grid.Highlight(_reachable, MoveHighlight);
            }

            var targetCells = new List<Vector2Int>();
            foreach (var other in _units)
                if (other.team != u.team && Manhattan(u.Cell, other.Cell) <= u.attackRange)
                {
                    _targets.Add(other);
                    targetCells.Add(other.Cell);
                }
            _grid.Highlight(targetCells, AtkHighlight);
        }

        void Deselect()
        {
            _selected = null;
            _movedThisSelection = false;
            _reachable.Clear();
            _targets.Clear();
            if (_grid != null) _grid.ClearHighlights();
        }

        Unit ClosestTarget(Unit from)
        {
            Unit best = null; int bestD = int.MaxValue;
            foreach (var t in _targets)
            {
                int d = Manhattan(from.Cell, t.Cell);
                if (d < bestD) { bestD = d; best = t; }
            }
            return best;
        }

        // ───────────────────────── 전투 연출/해결 ─────────────────────────

        IEnumerator PlayerAttack(Unit attacker, Unit target, int cost, int damage)
        {
            _busy = true;
            _energy = Mathf.Max(0, _energy - cost);
            yield return AttackRoutine(attacker, target, damage);
            attacker.SetActed(true);
            _busy = false;
            Deselect();
            if (_result == null) CheckPlayerTurnEnd();
        }

        IEnumerator AttackRoutine(Unit attacker, Unit target, int damage)
        {
            if (attacker == null || target == null) yield break;

            if (attacker.ranged) yield return Projectile(attacker.transform.position, target.transform.position, new Color(1f, 0.85f, 0.3f));
            else yield return Lunge(attacker, target.transform.position);

            bool died = target.TakeDamage(damage);
            yield return Flash(target);

            if (died)
            {
                _units.Remove(target);
                if (target != null) Destroy(target.gameObject);
            }
            CheckBattleEnd();
        }

        IEnumerator Lunge(Unit attacker, Vector3 targetPos)
        {
            Vector3 home = attacker.transform.position;
            Vector3 tip = Vector3.Lerp(home, targetPos, 0.4f);
            yield return MoveOver(attacker.transform, home, tip, 0.09f);
            yield return MoveOver(attacker.transform, tip, home, 0.09f);
        }

        IEnumerator Projectile(Vector3 from, Vector3 to, Color col)
        {
            var go = new GameObject("Projectile");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UnitSprite.Circle();
            sr.color = col;
            sr.sortingOrder = 8;
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.35f;

            float t = 0f, dur = 0.22f;
            while (t < dur)
            {
                t += Time.deltaTime;
                go.transform.position = Vector3.Lerp(from, to, t / dur);
                yield return null;
            }
            Destroy(go);
        }

        IEnumerator Flash(Unit u)
        {
            if (u == null) yield break;
            var sr = u.GetComponent<SpriteRenderer>();
            Color baseC = sr.color;
            for (int i = 0; i < 2; i++)
            {
                sr.color = Color.red;
                yield return new WaitForSeconds(0.05f);
                if (u == null) yield break;
                sr.color = baseC;
                yield return new WaitForSeconds(0.05f);
            }
        }

        static IEnumerator MoveOver(Transform tr, Vector3 a, Vector3 b, float dur)
        {
            float t = 0f;
            while (t < dur) { t += Time.deltaTime; tr.position = Vector3.Lerp(a, b, t / dur); yield return null; }
            tr.position = b;
        }

        void CheckBattleEnd()
        {
            if (_result != null) return;
            bool anyP = _units.Exists(u => u.team == Team.Player);
            bool anyE = _units.Exists(u => u.team == Team.Enemy);
            if (!anyE) _result = "승리!";
            else if (!anyP) _result = "패배...";
            if (_result != null) Deselect();
        }

        // ───────────────────────── 헬퍼 ─────────────────────────

        Unit UnitAt(Vector2Int cell)
        {
            foreach (var u in _units) if (u.Cell == cell) return u;
            return null;
        }

        Unit TargetAt(Vector2Int cell)
        {
            foreach (var t in _targets) if (t.Cell == cell) return t;
            return null;
        }

        HashSet<Vector2Int> Occupied(Unit except)
        {
            var s = new HashSet<Vector2Int>();
            foreach (var u in _units) if (u != except) s.Add(u.Cell);
            return s;
        }

        static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // ───────────────────────── UI (IMGUI) ─────────────────────────

        Rect _endTurnRect, _waitRect, _skillRect;

        void OnGUI()
        {
            var head = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            string who = _turn == Team.Player ? "플레이어 턴" : "적 턴";
            GUI.Label(new Rect(20, 14, 600, 34), $"Round {_round} — {who}", head);

            // 기력 게이지 + 주사위
            var sub = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            string dice = _rolling ? $"🎲 {_dice}…" : $"🎲 {_dice}";
            GUI.Label(new Rect(20, 50, 600, 30), $"기력 {_energy}/{EnergyMax}    {dice}", sub);

            if (_result != null)
            {
                var big = new GUIStyle(GUI.skin.label)
                { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), _result, big);
                return;
            }

            var btn = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            bool myTurn = _turn == Team.Player && !_busy;

            _endTurnRect = new Rect(Screen.width - 240, Screen.height - 92, 220, 68);
            GUI.enabled = myTurn;
            if (GUI.Button(_endTurnRect, "End Turn", btn)) EndTurn();

            // 유닛 선택 중 추가 버튼들
            _waitRect = new Rect(Screen.width - 240, Screen.height - 168, 220, 68);
            _skillRect = new Rect(Screen.width - 240, Screen.height - 244, 220, 68);

            if (_selected != null && myTurn)
            {
                if (GUI.Button(_waitRect, "대기 (Wait)", btn))
                {
                    _selected.SetActed(true);
                    Deselect();
                    CheckPlayerTurnEnd();
                }

                // 스킬: 사거리 안 적 있고 기력 충분할 때
                bool canSkill = _targets.Count > 0 && _energy >= _selected.skillCost;
                GUI.enabled = canSkill;
                if (GUI.Button(_skillRect, $"{_selected.skillName} ({_selected.skillCost}기력)", btn))
                {
                    var tgt = ClosestTarget(_selected);
                    if (tgt != null)
                        StartCoroutine(PlayerAttack(_selected, tgt, _selected.skillCost, _selected.SkillDamage));
                }
            }
            GUI.enabled = true;
        }

        // ───────────────────────── 배치/탐색 ─────────────────────────

        public static void SpawnDemo(BattleGrid grid, List<Unit> into)
        {
            var w = Color.white;
            UnitDef[] defs =
            {
                new() { name="전사",  cell=new(2,3), team=Team.Player, color=w, move=4, hp=14, atk=5, atkRange=1, ranged=false, sprite="warrior", skillName="강타",   skillCost=3 },
                new() { name="궁수",  cell=new(3,5), team=Team.Player, color=w, move=4, hp=10, atk=4, atkRange=3, ranged=true,  sprite="archer",  skillName="꿰뚫기", skillCost=3 },
                new() { name="슬라임", cell=new(9,4), team=Team.Enemy,  color=w, move=3, hp=11, atk=4, atkRange=1, ranged=false, sprite="slime" },
                new() { name="게",    cell=new(8,2), team=Team.Enemy,  color=w, move=3, hp=11, atk=4, atkRange=1, ranged=false, sprite="crab"  },
            };
            foreach (var d in defs) into.Add(SpawnUnit(grid, d));
        }

        public static Unit SpawnUnit(BattleGrid grid, UnitDef d)
        {
            var go = new GameObject(d.name);
            var u = go.AddComponent<Unit>();
            u.Init(grid, d.cell, d.team, d.color, d.move, d.hp, d.atk, d.atkRange, d.ranged, d.sprite);
            if (!string.IsNullOrEmpty(d.skillName)) u.skillName = d.skillName;
            if (d.skillCost > 0) u.skillCost = d.skillCost;
            return u;
        }

        public static List<Vector2Int> Reachable(BattleGrid grid, HashSet<Vector2Int> blocked, Vector2Int start, int range)
        {
            var result = new List<Vector2Int>();
            var dist = new Dictionary<Vector2Int, int> { { start, 0 } };
            var q = new Queue<Vector2Int>();
            q.Enqueue(start);

            Vector2Int[] dirs = { new(0, 1), new(0, -1), new(1, 0), new(-1, 0) };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                int cd = dist[c];
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (!grid.InBounds(n) || dist.ContainsKey(n) || blocked.Contains(n)) continue;
                    int nd = cd + 1;
                    if (nd > range) continue;
                    dist[n] = nd;
                    result.Add(n);
                    q.Enqueue(n);
                }
            }
            return result;
        }
    }
}
