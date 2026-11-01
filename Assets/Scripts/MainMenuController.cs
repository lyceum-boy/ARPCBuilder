
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject helpPanel;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        mainMenuPanel.SetActive(true);
        helpPanel.SetActive(false);

        startButton.onClick.AddListener(StartTraining);
        helpButton.onClick.AddListener(ShowHelp);
        exitButton.onClick.AddListener(ExitApplication);
        backButton.onClick.AddListener(ShowMainMenu);
    }

    private void StartTraining()
    {
        SceneManager.LoadScene("ARAssembly");
    }

    private void ShowHelp()
    {
        mainMenuPanel.SetActive(false);
        helpPanel.SetActive(true);
    }

    private void ShowMainMenu()
    {
        helpPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void ExitApplication()
    {
        Application.Quit();
    }
}
