using UnityEngine;
using System.Collections;

public class UIFollowMouseJoystick : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas canvas;

    private float moveRadius = 0.01f;

    // ��¼ҡ�˳�ʼλ�ã�����ڸ��ڵ㣩����ΪԲ��
    private Vector2 centerPosition;

    private int last = -1;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform.parent == null)
        {
            Debug.LogError("ҡ��û�и��ڵ㣬�뽫ҡ�˹�����Ч��UI�����¡�");
            return;
        }
        // ʹ��ҡ��ֱ�����ڵĸ��ڵ���Ϊ�ο�
        parentRectTransform = rectTransform.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>();

        // ��¼��ʼλ�ã�����ڸ��ڵ㣩��ΪԲ��
        centerPosition = rectTransform.anchoredPosition;
    }

    void Start()
    {
        // ��������л��ӽڵ��Э��
        StartCoroutine(RandomSwitchChild());
    }

    void Update()
    {
        Vector2 localPoint;
        // ���Canvas������Ļ�ռ�Overlay������Ҫ����worldCamera������null����
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // �������Ļ����ת��Ϊ���ڵ�ֲ�����
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRectTransform,
            Input.mousePosition,
            cam,
            out localPoint
        );

        // �������λ�����ʼ���ĵ�֮���ƫ�ƣ����ڵ�����ϵ�£�
        Vector2 offset = localPoint - centerPosition;

        // ����ҡ�˵�λ�ã�����ڸ��ڵ㣩
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
                // ���������ӽڵ㣬�����������ü���״̬
                for (int i = 0; i < childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(i == randomIndex);
                }
            }
            yield return new WaitForSeconds(3f); // ÿ���л�һ��
        }
    }
}
