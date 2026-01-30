using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GrassPatch : MonoBehaviour
{
    [SerializeField] private float maxHP = 3f;

    public float HP
    {
        get; private set;
    }
    public float MaxHP => maxHP;

    public event Action<GrassPatch> Cut;

    public void Initialize(float hp)
    {
        maxHP = Mathf.Max(0.01f, hp);
        HP = maxHP;
        gameObject.SetActive(true);
    }

    public void ApplyDamage(float dmg)
    {
        if (!gameObject.activeInHierarchy)
            return;

        HP -= Mathf.Max(0f, dmg);
        if (HP <= 0f)
        {
            HP = 0f;
            Cut?.Invoke(this);
        }
    }
}
