using UnityEngine;

namespace Sommoje.Action3D
{
    /// <summary>물 머티리얼의 노멀맵 오프셋을 흘려 물결이 움직이게 한다(하늘 반사가 일렁임).</summary>
    public class WaterAnimator : MonoBehaviour
    {
        public Vector2 speed = new(0.025f, 0.018f);

        Material _mat;

        void Start()
        {
            var r = GetComponent<Renderer>();
            if (r != null) _mat = r.material;   // 인스턴스
        }

        void Update()
        {
            if (_mat == null) return;
            var o = _mat.GetTextureOffset("_BumpMap") + speed * Time.deltaTime;
            o.x %= 1f; o.y %= 1f;
            _mat.SetTextureOffset("_BumpMap", o);
        }
    }
}
