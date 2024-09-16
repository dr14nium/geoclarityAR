using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ReportFormManager : MonoBehaviour
{
    public static ReportFormManager Instance;

    [Header("Form Elements")]
    public TMP_InputField ObjectIDField;
    public TMP_InputField UserIDField;
    public TMP_InputField NameField;
    public TMP_Dropdown CategoryDropdown;
    public TMP_InputField DescriptionField;

    [Header("UI Elements")]
    public Button SubmitButton;
    public Transform HistoryContentParent;
    public GameObject HistoryRowPrefab;
    public GameObject NoReportMessagePrefab;

    [Header("Month Navigation")]
    public Button NextButton;
    public Button PreviousButton;
    public Image NextButtonIcon;
    public Image PreviousButtonIcon;
    public TextMeshProUGUI MonthLabel;

    [Header("Close Buttons")]
    public Button CloseButtonReportForm;
    public Button CloseButtonHistoryForm;
    public GameObject FormDialog;

    [Header("Icons for statuses")]
    public Sprite SubmittedIcon;
    public Sprite RejectedIcon;
    public Sprite UnderReviewIcon;
    public Sprite AcceptedIcon;

    private Color submittedColor = new Color32(164, 171, 182, 255); 
    private Color rejectedColor = new Color32(255, 56, 56, 255);    
    private Color underReviewColor = new Color32(255, 179, 2, 255); 
    private Color acceptedColor = new Color32(86, 240, 0, 255);     

    private Color activeColor = new Color32(20, 85, 254, 255); 
    private Color inactiveColor = new Color32(164, 171, 182, 255); 

    private DateTime currentMonth;
    private DateTime minMonth;
    private DateTime maxMonth;
    private DateTime previousMonth = DateTime.MinValue; 

    private Dictionary<string, ReportStatus[]> reportCache = new Dictionary<string, ReportStatus[]>();

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

    private void Start()
    {
        SubmitButton.onClick.AddListener(OnSubmit);
        ObjectIDField.interactable = false; 

        CloseButtonReportForm.onClick.AddListener(CloseFormDialog);
        CloseButtonHistoryForm.onClick.AddListener(CloseFormDialog);

        maxMonth = DateTime.Now;
        currentMonth = maxMonth;  

        PreviousButton.onClick.AddListener(OnPreviousMonthClick);
        NextButton.onClick.AddListener(OnNextMonthClick);

        PreviousButton.enabled = false;
        UpdateMonthUI();
    }

    public void CloseFormDialog()
    {
        if (FormDialog != null)
        {
            FormDialog.SetActive(false);

            // Pastikan currentMonth di-reset saat form ditutup, sehingga ketika dibuka ulang, 
            // form selalu menampilkan bulan terbaru.
            currentMonth = maxMonth;
            previousMonth = DateTime.MinValue; // Reset previousMonth untuk force update ketika dibuka lagi
        }
    }

    public void OpenFormDialog()
    {
        if (FormDialog != null)
        {
            FormDialog.SetActive(true);

            ShowHistoryForCurrentMonth();
        }
    }

    public void SetObjectID(string objectID)
    {
        if (!string.IsNullOrEmpty(objectID))
        {
            ObjectIDField.text = objectID;
            reportCache.Clear();  

            StartCoroutine(GetEarliestMonth(objectID)); 
            StartCoroutine(GetAllReportStatusesForObjectID(objectID)); 
        }
    }

    private void OnSubmit()
    {
        string objectID = ObjectIDField.text;
        string userID = UserIDField.text;
        string name = NameField.text;
        int categoryID = CategoryDropdown.value;
        string description = DescriptionField.text;

        if (string.IsNullOrEmpty(objectID) || string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
        {
            SnackBarManager.Instance.ShowSnackBar("Please fill out all required fields!", true);
            return;
        }

        ReportData reportData = new ReportData
        {
            objectID = objectID,
            userID = userID,
            name = name,
            categoryID = categoryID,
            description = description
        };

        StartCoroutine(SendReportData(reportData));
    }

    private IEnumerator SendReportData(ReportData reportData)
    {
        string url = "https://be-express-ugm-190094631420.asia-southeast2.run.app/submitReport";
        string jsonData = JsonConvert.SerializeObject(reportData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            SnackBarManager.Instance.ShowSnackBar($"Error sending data: {request.error}", true);
        }
        else
        {
            SnackBarManager.Instance.ShowSnackBar("Report submitted successfully!", true);
        }
    }

    private void OnPreviousMonthClick()
    {
        if (currentMonth > minMonth)
        {
            currentMonth = currentMonth.AddMonths(-1);
            previousMonth = DateTime.MinValue; // Reset previousMonth to force UI update
            UpdateMonthUI();
        }
    }

    private void OnNextMonthClick()
    {
        if (currentMonth < maxMonth)
        {
            currentMonth = currentMonth.AddMonths(1);
            previousMonth = DateTime.MinValue; // Reset previousMonth to force UI update
            UpdateMonthUI();
        }
    }

    private void UpdateMonthUI()
    {
        if (currentMonth == previousMonth)
        {
            return;
        }

        previousMonth = currentMonth; 

        MonthLabel.text = currentMonth.ToString("MMM yyyy");

        string monthKey = currentMonth.ToString("yyyy-MM");

        if (reportCache.ContainsKey(monthKey) && reportCache[monthKey].Length > 0)
        {
            DisplayReportStatuses(reportCache[monthKey]);
        }
        else
        {
            ClearHistoryContent();
            DisplayNoReportsMessage();
        }

        if (minMonth != DateTime.MinValue && currentMonth <= minMonth)
        {
            PreviousButtonIcon.color = inactiveColor;
            PreviousButton.enabled = false;
        }
        else
        {
            PreviousButtonIcon.color = activeColor;
            PreviousButton.enabled = true;
        }

        if (currentMonth >= maxMonth)
        {
            NextButtonIcon.color = inactiveColor;
            NextButton.enabled = false;
        }
        else
        {
            NextButtonIcon.color = activeColor;
            NextButton.enabled = true;
        }
    }

    private void ClearHistoryContent()
    {
        foreach (Transform child in HistoryContentParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void DisplayNoReportsMessage()
    {
        GameObject noReportInstance = Instantiate(NoReportMessagePrefab, HistoryContentParent);
        TextMeshProUGUI noReportText = noReportInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (noReportText != null)
        {
            noReportText.text = "No reports available for this month.";
        }
    }

    private void DisplayReportStatuses(ReportStatus[] statuses)
    {
        ClearHistoryContent();

        if (statuses == null || statuses.Length == 0)
        {
            DisplayNoReportsMessage();
        }
        else
        {
            foreach (ReportStatus status in statuses)
            {
                GameObject rowInstance = Instantiate(HistoryRowPrefab, HistoryContentParent);
                Image background = rowInstance.transform.Find("Icon/Background").GetComponent<Image>();
                Image iconStatus = rowInstance.transform.Find("Icon/Background/IconStatus").GetComponent<Image>();
                TextMeshProUGUI categoryText = rowInstance.transform.Find("Content/Header/Category").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI dateText = rowInstance.transform.Find("Content/Header/Date").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI statusText = rowInstance.transform.Find("Content/Status").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI reasonText = rowInstance.transform.Find("Content/Reason").GetComponent<TextMeshProUGUI>();

                categoryText.text = status.category_name;
                dateText.text = status.created_at;
                statusText.text = status.status_desc;
                reasonText.text = status.reason;

                switch (status.status_desc.ToLower())
                {
                    case "submitted":
                        background.color = submittedColor;
                        iconStatus.sprite = SubmittedIcon;
                        break;
                    case "rejected":
                        background.color = rejectedColor;
                        iconStatus.sprite = RejectedIcon;
                        break;
                    case "under review":
                        background.color = underReviewColor;
                        iconStatus.sprite = UnderReviewIcon;
                        break;
                    case "accepted":
                        background.color = acceptedColor;
                        iconStatus.sprite = AcceptedIcon;
                        break;
                    default:
                        background.color = Color.white;
                        iconStatus.sprite = null;
                        break;
                }

                iconStatus.color = Color.white;
            }
        }
    }

    public void OpenHistoryForm()
    {
        if (FormDialog != null)
        {
            FormDialog.SetActive(true);

            // Reset currentMonth ke maxMonth setiap kali History Form dibuka
            currentMonth = maxMonth;

            UpdateMonthUI();
            ShowHistoryForCurrentMonth();
        }
    }

    private IEnumerator GetAllReportStatusesForObjectID(string objectID)
    {
        string url = "https://be-express-ugm-190094631420.asia-southeast2.run.app/getReportStatus";
        string jsonData = JsonConvert.SerializeObject(new { id = objectID, type = "single" });

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            SnackBarManager.Instance.ShowSnackBar($"Error fetching report statuses: {request.error}", true);
        }
        else
        {
            string json = request.downloadHandler.text;
            ReportStatus[] reportData = JsonConvert.DeserializeObject<ReportStatus[]>(json);

            if (reportData != null && reportData.Length > 0)
            {
                string monthKey = currentMonth.ToString("yyyy-MM");
                reportCache[monthKey] = reportData;

                ShowHistoryForCurrentMonth();
            }
        }
    }

    public void ShowHistoryForCurrentMonth()
    {
        string monthKey = currentMonth.ToString("yyyy-MM");

        if (reportCache.ContainsKey(monthKey) && reportCache[monthKey].Length > 0)
        {
            DisplayReportStatuses(reportCache[monthKey]);
        }
        else
        {
            ClearHistoryContent();
            DisplayNoReportsMessage();
        }

        UpdateMonthUI();
    }

    private IEnumerator GetEarliestMonth(string objectID)
    {
        string url = "http://192.168.1.13:3000/getEarliestMonth";
        string jsonData = JsonConvert.SerializeObject(new { objectID = objectID });

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
        }
        else
        {
            string responseText = request.downloadHandler.text;
            EarliestMonthResponse response = JsonConvert.DeserializeObject<EarliestMonthResponse>(responseText);
            if (DateTime.TryParse(response.earliestMonth, out minMonth))
            {
                UpdateMonthUI();
            }
        }
    }
}

[System.Serializable]
public class EarliestMonthResponse
{
    public string earliestMonth;
}

[System.Serializable]
public class ReportData
{
    public string objectID;
    public string userID;
    public string name;
    public int categoryID;
    public string description;
}

[System.Serializable]
public class ReportStatus
{
    public string status_desc;
    public string created_at;
    public string reason;
    public string category_name;
}