using UnityEngine;

namespace Sommoje.Battle
{
    /// <summary>
    /// 직교 카메라를 격자 전체가 항상 보이도록 자동 맞춤한다.
    /// 모바일의 다양한 화면 비율(가로 기준)에서 가로/세로 모두 들어오도록 orthographicSize를 잡는다.
    /// [ExecuteAlways] 라 편집 화면에서도 즉시 반영된다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [Tooltip("격자 바깥 여백(셀 단위)")]
        public float marginCells = 0.6f;

        Camera _cam;
        BattleGrid _grid;

        void OnEnable()
        {
            _cam = GetComponent<Camera>();
            Fit();
        }

        void Update() => Fit();   // Game 뷰 해상도/비율이 바뀌면 따라 맞춤

        public void Fit()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_grid == null) _grid = Object.FindFirstObjectByType<BattleGrid>();
            if (_grid == null || !_cam.orthographic) return;

            float w = _grid.Width + marginCells * 2f;
            float h = _grid.Height + marginCells * 2f;
            float aspect = _cam.aspect <= 0f ? 1f : _cam.aspect;

            // 세로로도, 가로로도 다 들어오는 크기 선택
            _cam.orthographicSize = Mathf.Max(h * 0.5f, (w * 0.5f) / aspect);

            float z = _cam.transform.position.z;
            _cam.transform.position = new Vector3(_grid.Width * 0.5f, _grid.Height * 0.5f, z < 0f ? z : -10f);
        }
    }
}
