using TMPro;
using UnityEngine;
using HolenderGames.Currencies;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void Awake()
    {
        if (!text)
            text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        CurrencyManager.Instance.StartListening(CurrencyType.Gold, OnGoldChanged);
        OnGoldChanged(CurrencyManager.Instance.GetCurrencyCount(CurrencyType.Gold)); // set immediately
    }

    private void OnDisable()
    {
        CurrencyManager.Instance.StopListening(CurrencyType.Gold, OnGoldChanged);
    }

    private void OnGoldChanged(int gold)
    {
        text.text = gold.ToString();
    }
}
