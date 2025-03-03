using UnityEngine;
using System.Collections;

public class UIFollowMouseJoystick : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas canvas;

    private float moveRadius = 0.01f;

    // 记录摇杆初始位置（相对于父节点），作为圆心
    private Vector2 centerPosition;

    private int last = -1;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform.parent == null)
        {
            Debug.LogError("摇杆没有父节点，请将摇杆挂在有效的UI容器下。");
            return;
        }
        // 使用摇杆直接所在的父节点作为参考
        parentRectTransform = rectTransform.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>();

        // 记录初始位置（相对于父节点）作为圆心
        centerPosition = rectTransform.anchoredPosition;
    }

    void Start()
    {
        // 启动随机切换子节点的协程
        StartCoroutine(RandomSwitchChild());
    }

    void Update()
    {
        Vector2 localPoint;
        // 如果Canvas不是屏幕空间Overlay，则需要传入worldCamera，否则传null即可
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // 将鼠标屏幕坐标转换为父节点局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRectTransform,
            Input.mousePosition,
            cam,
            out localPoint
        );

        // 计算鼠标位置与初始中心点之间的偏移（父节点坐标系下）
        Vector2 offset = localPoint - centerPosition;

        // 更新摇杆的位置（相对于父节点）
        rectTransform.anchoredPosition = centerPosition + offset * moveRadius;
    }

    IEnumerator RandomSwitchChild()
    {
        while (true)
        {
            int childCount = transform.childCount;
            if (childCount > 0)
            {
                int randomIndex = Random.Range(0, childCount);
                while(randomIndex == last)
                {
                    randomIndex = Random.Range(0, childCount);
                }
                last = randomIndex;
                // 遍历所有子节点，根据索引设置激活状态
                for (int i = 0; i < childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(i == randomIndex);
                }
            }
            yield return new WaitForSeconds(3f); // 每秒切换一次
        }
    }
}
