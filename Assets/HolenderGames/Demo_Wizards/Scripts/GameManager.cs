using HolenderGames.StatSystem;
using UnityEngine;

namespace HolenderGames.WizardDemo
{
    /// <summary>
    /// Initiates basic game params and stats
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private StatDB statDB;

        void Awake()
        {
            GameData.Instance.Reset(statDB);
        }

    }
}

