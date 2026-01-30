using System;
using UnityEngine;

public class GoldWallet : MonoBehaviour
{
    public static GoldWallet Instance
    {
        get; private set;
    }

    [SerializeField] private int startingGold = 0;

    public int Gold
    {
        get; private set;
    }
    public event Action<int> GoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Gold = Mathf.Max(0, startingGold);
        GoldChanged?.Invoke(Gold);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;
        Gold += amount;
        GoldChanged?.Invoke(Gold);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return true;
        if (Gold < amount)
            return false;

        Gold -= amount;
        GoldChanged?.Invoke(Gold);
        return true;
    }
}
