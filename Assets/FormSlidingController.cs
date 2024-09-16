using UnityEngine;
using UnityEngine.UI;

public class FormSlidingController : MonoBehaviour
{
    public RectTransform content;   // Referensi ke Content
    public ScrollRect scrollRect;   // Referensi ke ScrollRect
    public CanvasGroup formReportGroup;  // CanvasGroup untuk FormReport
    public CanvasGroup formHistoryGroup; // CanvasGroup untuk FormHistory
    public float slideSpeed = 10f;  // Kecepatan sliding
    public float smoothTime = 0.3f; // Waktu yang dibutuhkan untuk perpindahan halus
    public float threshold = 0.5f;  // Batas minimum untuk sliding ke form lain

    private Vector2 velocity = Vector2.zero; // Digunakan untuk menyimpan kecepatan perpindahan
    private Vector2 formReportPos;  // Posisi untuk FormReport
    private Vector2 formHistoryPos; // Posisi untuk FormHistory
    private bool isDragging = false;
    private float targetPosition;   // Posisi target perpindahan horizontal

    private ReportFormManager reportFormManager; // Referensi ke ReportFormManager
    private bool isFirstTimeInHistory = true; // Flag untuk mendeteksi pertama kali masuk ke history

    void Start()
    {
        // Dapatkan ukuran layar
        float screenWidth = Screen.width;
        
        // Inisialisasi posisi FormReport dan FormHistory berdasarkan ukuran layar
        formReportPos = new Vector2(0, 0);
        formHistoryPos = new Vector2(-screenWidth, 0);  // Posisi FormHistory bergantung pada lebar layar

        // Hanya aktifkan scrolling horizontal, matikan scrolling vertikal
        scrollRect.vertical = false;
        scrollRect.horizontal = true;

        // Pastikan FormReport terlihat di awal
        SetFormVisibility(formReportGroup, true);
        SetFormVisibility(formHistoryGroup, false);

        // Dapatkan referensi ke ReportFormManager
        reportFormManager = ReportFormManager.Instance;
    }

    void Update()
    {
        if (!isDragging)
        {
            // SmoothDamp posisi konten ke form yang aktif
            if (scrollRect.horizontalNormalizedPosition < threshold)
            {
                targetPosition = formReportPos.x;
                content.anchoredPosition = Vector2.SmoothDamp(content.anchoredPosition, formReportPos, ref velocity, smoothTime);
                SetFormVisibility(formReportGroup, true);
                SetFormVisibility(formHistoryGroup, false);
            }
            else
            {
                targetPosition = formHistoryPos.x;
                content.anchoredPosition = Vector2.SmoothDamp(content.anchoredPosition, formHistoryPos, ref velocity, smoothTime);
                SetFormVisibility(formReportGroup, false);
                SetFormVisibility(formHistoryGroup, true);

                // Jika sudah pada FormHistory, cek apakah ini pertama kali masuk atau tidak
                if (reportFormManager != null)
                {
                    if (isFirstTimeInHistory)
                    {
                        reportFormManager.OpenHistoryForm(); // Reset bulan ke maxMonth dan update history
                        isFirstTimeInHistory = false; // Hanya reset pada kali pertama
                    }
                    else
                    {
                        reportFormManager.ShowHistoryForCurrentMonth(); // Memuat ulang history jika sudah masuk sebelumnya
                    }
                }
            }
        }
    }

    public void OnBeginDrag()
    {
        isDragging = true;
    }

    public void OnEndDrag()
    {
        isDragging = false;

        if (scrollRect.horizontalNormalizedPosition > 0.5f)
        {
            // Pindah ke FormHistory
            scrollRect.horizontalNormalizedPosition = 1f;
            SetFormVisibility(formReportGroup, false);
            SetFormVisibility(formHistoryGroup, true);

            if (reportFormManager != null)
            {
                // Saat membuka form history, pastikan form dialog dibuka ulang dengan data bulan yang benar
                reportFormManager.OpenFormDialog();
            }
        }
        else
        {
            // Pindah ke FormReport
            scrollRect.horizontalNormalizedPosition = 0f;
            SetFormVisibility(formReportGroup, true);
            SetFormVisibility(formHistoryGroup, false);
        }
    }

    // Fungsi untuk mengatur visibilitas form
    private void SetFormVisibility(CanvasGroup canvasGroup, bool isVisible)
    {
        canvasGroup.alpha = isVisible ? 1f : 0f; // Atur alpha (keterlihatan)
        canvasGroup.interactable = isVisible;    // Atur apakah bisa diinteraksi
        canvasGroup.blocksRaycasts = isVisible;  // Atur apakah menghalangi raycast
    }
}