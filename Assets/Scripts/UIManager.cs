using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    [SerializeField] private RectTransform loadingPanel;
    private Camera mainCamera;

    void Start()
    {
        if (loadingPanel != null)
        {
            loadingPanel.sizeDelta = new Vector2(Screen.width, Screen.height);
            loadingPanel.anchoredPosition = new Vector2(0.0f, -Screen.height);
        }

        mainCamera = Camera.main;
    }

    public void LoadFirstLevel()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        StartCoroutine(LoadFirstLevelSequence());
    }

    private IEnumerator LoadFirstLevelSequence()
    {
        ShowLoadingScreen();

        yield return new WaitForSeconds(1.0f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        /*if (scene.buildIndex == 1)
        {
            GameObject quitButtonObject = GameObject.FindGameObjectWithTag("QuitButton");

            if (quitButtonObject != null)
            {
                Button quitButton = quitButtonObject.GetComponent<Button>();

                if (quitButton != null)
                {
                    quitButton.onClick.AddListener(QuitGame);
                }
            }
        }*/

        mainCamera = Camera.main;
        StartCoroutine(HideLoadingScreenSequence());
    }

    private IEnumerator HideLoadingScreenSequence()
    {
        yield return new WaitForSeconds(1.0f);
        HideLoadingScreen();
    }

    public void ShowLoadingScreen()
    {
        if (loadingPanel != null)
        {
            StartCoroutine(ShowLoadingScreenCoroutine());
        }
    }

    private IEnumerator ShowLoadingScreenCoroutine()
    {
        Vector2 targetPosition = new Vector2(0.0f, 0.0f);
        Vector2 startPosition = loadingPanel.anchoredPosition;
        float duration = 0.5f;
        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            loadingPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        loadingPanel.anchoredPosition = targetPosition;
    }

    public void HideLoadingScreen()
    {
        if (loadingPanel != null)
        {
            StartCoroutine(HideLoadingScreenCoroutine());
        }
    }

    private IEnumerator HideLoadingScreenCoroutine()
    {
        Vector2 targetPosition = new Vector2(0.0f, -Screen.height);
        Vector2 startPosition = loadingPanel.anchoredPosition;
        float duration = 0.5f;
        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            loadingPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        loadingPanel.anchoredPosition = targetPosition;
        loadingPanel.anchoredPosition = new Vector2(0.0f, -2000.0f);
    }

    void Update()
    {

    }

    void LateUpdate()
    {

    }
}