using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
public class TimelineScaleEffect : MonoBehaviour
{
    [Header("Refs")]
    public ScrollRect scroll;              // 你的 Scroll View（Vertical = ✓）
    public RectTransform viewport;         // Scroll View 的 Viewport
    public RectTransform content;          // Viewport/Content

    [Header("Scale Settings")]
    [Range(0.5f, 1.5f)] public float maxScale = 1.0f; // 正中央的缩放
    [Range(0.2f, 1.2f)] public float minScale = 0.75f;// 远离中心的缩放
    [Range(0f, 1f)]     public float minAlpha = 0.35f;// 最小透明度（可选）
    public bool alsoFade = true;                       // 是否做淡入淡出
    public AnimationCurve falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("影响范围系数：0.6 表示以 0.6*Viewport高度为主要影响区间")]
    [Range(0.2f, 1.5f)] public float rangeByViewportHeight = 0.6f;

    [Header("Snap Settings")]
    public bool enableAutoSnap = true;                // 松手后自动吸附
    [Tooltip("低于此速度视为停止（像素/秒）")]
    public float snapVelocityThreshold = 80f;
    [Tooltip("吸附动画时长")]
    public float snapDuration = 0.25f;
    public AnimationCurve snapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool snapping;
    Coroutine snapCo;

    void Reset()
    {
        scroll = GetComponentInParent<ScrollRect>();
        if (scroll)
        {
            viewport = scroll.viewport;
            content  = scroll.content;
        }
    }

    void OnEnable()
    {
        if (scroll) scroll.onValueChanged.AddListener(OnScroll);
        UpdateScales();
    }

    void OnDisable()
    {
        if (scroll) scroll.onValueChanged.RemoveListener(OnScroll);
    }

    void LateUpdate()
    {
        UpdateScales();

        // 自动吸附：不在吸附过程中、允许吸附、竖向速度低于阈值、且确实可滚动（内容高于视口）
        if (enableAutoSnap && !snapping && scroll && Mathf.Abs(scroll.velocity.y) < snapVelocityThreshold)
        {
            if (content && viewport && content.rect.height > viewport.rect.height + 1f)
            {
                SnapToNearest();
            }
        }
    }

    void OnScroll(Vector2 _) => UpdateScales();

    void UpdateScales()
    {
        if (!viewport || !content) return;

        // Viewport 的几何中心（本地坐标）
        Vector2 vpCenterLocal = viewport.rect.center;
        // 影响范围
        float influenceRange = Mathf.Max(1f, viewport.rect.height * rangeByViewportHeight);

        for (int i = 0; i < content.childCount; i++)
        {
            var card = content.GetChild(i) as RectTransform;
            if (!card) continue;

            // 卡片几何中心 → 世界坐标 → Viewport 本地
            Vector3 cardCenterWorld = card.TransformPoint(card.rect.center);
            Vector3 cardInVpLocal   = viewport.InverseTransformPoint(cardCenterWorld);

            // 与视口中心的竖直距离
            float dist = Mathf.Abs(cardInVpLocal.y - vpCenterLocal.y);
            float t = Mathf.Clamp01(1f - dist / influenceRange); // 0..1，越靠中心越接近1
            float k = falloff.Evaluate(t);

            float s = Mathf.Lerp(minScale, maxScale, k);
            card.localScale = new Vector3(s, s, 1f);

            if (alsoFade)
            {
                float a = Mathf.Lerp(minAlpha, 1f, k);
                var cg = card.GetComponent<CanvasGroup>();
                if (!cg) cg = card.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = a; // CanvasGroup.blocksRaycasts 默认 true，点击无影响
            }
        }
    }

    // —— 吸附到最近的卡片（让该卡片几何中心对齐到 Viewport 几何中心）——
    public void SnapToNearest()
    {
        if (!viewport || !content) return;

        // 1) 计算最近卡片
        Vector2 vpCenterLocal = viewport.rect.center;
        RectTransform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < content.childCount; i++)
        {
            var card = content.GetChild(i) as RectTransform;
            if (!card || !card.gameObject.activeInHierarchy) continue;

            Vector3 cardCenterWorld = card.TransformPoint(card.rect.center);
            Vector3 cardInVpLocal   = viewport.InverseTransformPoint(cardCenterWorld);

            float dist = Mathf.Abs(cardInVpLocal.y - vpCenterLocal.y);
            if (dist < bestDist)
            {
                bestDist = dist;
                best     = card;
            }
        }
        if (!best) return;

        // 2) 计算需要移动的位移（在 Viewport 本地空间下）
        Vector3 bestCenterWorld = best.TransformPoint(best.rect.center);
        Vector3 bestInVpLocal   = viewport.InverseTransformPoint(bestCenterWorld);
        float   deltaY          = bestInVpLocal.y - vpCenterLocal.y; // 正值=卡片在上方，需要下移内容

        // 3) 用协程平滑移动 content.anchoredPosition
        if (snapCo != null) StopCoroutine(snapCo);
        snapCo = StartCoroutine(CoSnapByDelta(-deltaY)); // 内容应往反方向移动
    }

    IEnumerator CoSnapByDelta(float deltaY)
    {
        snapping = true;

        // 关闭当前惯性，避免打架
        if (scroll) scroll.velocity = Vector2.zero;

        Vector2 from = content.anchoredPosition;
        Vector2 to   = from + new Vector2(0f, deltaY);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, snapDuration);
            float e = snapEase.Evaluate(Mathf.Clamp01(t));
            content.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        content.anchoredPosition = to;

        snapping = false;
        snapCo = null;
    }
}
