using HolenderGames.StatSystem;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private StatDB statDB;

    private void Awake()
    {
        GameData.Instance.Reset(statDB);
    }
}
