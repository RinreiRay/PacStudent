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

    private Camera mainCamera;

    void Start()
    {
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
    }


    void Update()
    {

    }

    void LateUpdate()
    {

    }
}