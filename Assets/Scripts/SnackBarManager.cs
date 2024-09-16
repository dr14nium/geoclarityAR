using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SnackBarManager : MonoBehaviour
{
    public static SnackBarManager Instance;

    [Header("UI Elements")]
    public GameObject SnackBarPanel;
    public Text SnackBarText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowSnackBar(string message, bool show)
    {
        if (SnackBarPanel != null && SnackBarText != null)
        {
            SnackBarText.text = message;
            SnackBarPanel.SetActive(show);

            if (show)
            {
                Invoke(nameof(HideSnackBar), 3.0f);
            }
        }
    }

    private void HideSnackBar()
    {
        if (SnackBarPanel != null)
        {
            SnackBarPanel.SetActive(false);
        }
    }

    public bool IsShowing()
    {
        return SnackBarPanel.activeSelf;
    }
}
