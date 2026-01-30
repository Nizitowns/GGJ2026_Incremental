using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionTimerUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private TMP_Text text;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;     // enable this on session end
    [SerializeField] private bool hideButtonUntilEnd = true;

    [Header("Optional: Upgrade Panel")]
    [SerializeField] private GameObject upgradePanel;  // set active on session end (optional)

    [Header("Format")]
    [SerializeField] private bool showMilliseconds = false;

    [Header("Upgrade Button")]
    [SerializeField] private TMP_Text SessionEndSummary;     // enable this on session end
    [SerializeField] private GameObject SessionEndGo;  // set active on session end (optional)

    private void Awake()
    {
        if (!text)
            text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (session != null)
            session.SessionEnded += OnSessionEnded;

        if (upgradeButton != null)
        {
            upgradeButton.interactable = !hideButtonUntilEnd;
            upgradeButton.gameObject.SetActive(!hideButtonUntilEnd);
        }

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    private void OnDisable()
    {
        if (session != null)
            session.SessionEnded -= OnSessionEnded;
    }

    private void Update()
    {
        if (session == null || text == null)
            return;

        float t = Mathf.Max(0f, session.TimeRemaining);

        if (showMilliseconds)
            text.text = t.ToString("F1");          // 9.3
        else
            text.text = Mathf.CeilToInt(t).ToString(); // 10..1..0
    }

    private void OnSessionEnded()
    {
        Debug.Log("SessionEnded");
        // show 0 explicitly
        if (text != null)
            text.text = "0";

        // enable button
        if (upgradeButton != null)
        {
            upgradeButton.gameObject.SetActive(true);
            upgradeButton.interactable = true;
        }

        if (SessionEndSummary != null && SessionEndGo != null)
        {
            SessionEndGo.gameObject.SetActive(true);
            SessionEndSummary.text = "Session End";
        }

        // optional panel
        if (upgradePanel != null)
            upgradePanel.SetActive(true);
    }
}
