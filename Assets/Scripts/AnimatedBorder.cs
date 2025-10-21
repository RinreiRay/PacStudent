using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedBorder : MonoBehaviour
{
    public Canvas borderCanvas;
    public Canvas titleCanvas;
    public RectTransform titleElement;
    public GameObject dotPrefab;
    public int numberOfDots = 20;
    public float animationSpeed = 50f;
    public float borderOffset = 10f;
    public float yOffset = 0f;
    public Vector2 dotScale = Vector2.one;

    private List<GameObject> dots = new List<GameObject>();
    private List<float> dotPositions = new List<float>();
    private float totalPerimeter;
    private Vector2[] cornerPoints = new Vector2[4];
    private Camera uiCamera;

    // Start is called before the first frame update
    void Start()
    {
        if (titleCanvas == null && titleElement != null)
        {
            titleCanvas = titleElement.GetComponentInParent<Canvas>();
        }

        uiCamera = titleCanvas != null ? titleCanvas.worldCamera : null;

        SetupBorderCanvas();
        CalculateBorderPath();
        SpawnDots();
    }

    // Update is called once per frame
    void Update()
    {
        AnimateDots();
    }

    void SetupBorderCanvas()
    {
        if (borderCanvas == null)
        {
            GameObject canvasGO = new GameObject("BorderCanvas");
            borderCanvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        borderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        borderCanvas.sortingOrder = 10;

        CanvasScaler scaler = borderCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    void CalculateBorderPath()
    {
        if (titleElement == null || borderCanvas == null) return;

        Vector2 titleLocalPos = ConvertToLocalPosition(titleElement);
        Vector2 adjustedTitlePos = new Vector2(titleLocalPos.x, titleLocalPos.y + yOffset);
        Vector2 titleSize = ConvertSizeToLocalCanvas(titleElement);

        float halfWidth = (titleSize.x / 2) + borderOffset;
        float halfHeight = (titleSize.y / 2) + borderOffset;

        cornerPoints[0] = new Vector2(adjustedTitlePos.x - halfWidth, adjustedTitlePos.y + halfHeight);
        cornerPoints[1] = new Vector2(adjustedTitlePos.x + halfWidth, adjustedTitlePos.y + halfHeight);
        cornerPoints[2] = new Vector2(adjustedTitlePos.x + halfWidth, adjustedTitlePos.y - halfHeight);
        cornerPoints[3] = new Vector2(adjustedTitlePos.x - halfWidth, adjustedTitlePos.y - halfHeight);

        totalPerimeter = (titleSize.x + 2 * borderOffset) * 2 + (titleSize.y + 2 * borderOffset) * 2;
    }

    Vector2 ConvertToLocalPosition(RectTransform sourceRect)
    {
        Vector3[] worldCorners = new Vector3[4];
        sourceRect.GetWorldCorners(worldCorners);
        Vector3 worldCenter = (worldCorners[0] + worldCorners[2]) / 2f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            borderCanvas.GetComponent<RectTransform>(),
            screenPoint,
            borderCanvas.worldCamera,
            out localPoint
        );

        return localPoint;
    }

    Vector2 ConvertSizeToLocalCanvas(RectTransform sourceRect)
    {
        float titleCanvasScale = titleCanvas.scaleFactor;
        float borderCanvasScale = borderCanvas.scaleFactor;
        float scaleFactor = titleCanvasScale / borderCanvasScale;

        return sourceRect.rect.size * scaleFactor;
    }

    void SpawnDots()
    {
        foreach (GameObject dot in dots)
        {
            if (dot != null) DestroyImmediate(dot);
        }
        dots.Clear();
        dotPositions.Clear();

        for (int i = 0; i < numberOfDots; i++)
        {
            GameObject dot = Instantiate(dotPrefab, borderCanvas.transform);
            dots.Add(dot);

            RectTransform dotRect = dot.GetComponent<RectTransform>();
            if (dotRect != null)
            {
                dotRect.localScale = new Vector3(dotScale.x, dotScale.y, 1f);
            }

            float position = (float)i / numberOfDots;
            dotPositions.Add(position);
            UpdateDotPosition(dot, position);
        }
    }

    void AnimateDots()
    {
        if (titleElement == null || dots.Count == 0) return;

        CalculateBorderPath();

        for (int i = 0; i < dots.Count; i++)
        {
            dotPositions[i] += (animationSpeed / totalPerimeter) * Time.deltaTime;

            if (dotPositions[i] >= 1f)
                dotPositions[i] -= 1f;

            UpdateDotPosition(dots[i], dotPositions[i]);
        }
    }

    void UpdateDotPosition(GameObject dot, float normalizedPosition)
    {
        Vector2 localPosition = GetPositionOnBorder(normalizedPosition);
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        if (dotRect != null)
        {
            dotRect.anchoredPosition = localPosition;
        }
    }

    Vector2 GetPositionOnBorder(float normalizedPosition)
    {
        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        float sideLength = 0.25f;

        if (normalizedPosition <= sideLength)
        {
            float t = normalizedPosition / sideLength;
            return Vector2.Lerp(cornerPoints[0], cornerPoints[1], t);
        }
        else if (normalizedPosition <= sideLength * 2)
        {
            float t = (normalizedPosition - sideLength) / sideLength;
            return Vector2.Lerp(cornerPoints[1], cornerPoints[2], t);
        }
        else if (normalizedPosition <= sideLength * 3)
        {
            float t = (normalizedPosition - sideLength * 2) / sideLength;
            return Vector2.Lerp(cornerPoints[2], cornerPoints[3], t);
        }
        else
        {
            float t = (normalizedPosition - sideLength * 3) / sideLength;
            return Vector2.Lerp(cornerPoints[3], cornerPoints[0], t);
        }
    }
}