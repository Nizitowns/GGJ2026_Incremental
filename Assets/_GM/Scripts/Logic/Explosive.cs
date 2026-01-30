using System;
using UnityEngine;

public class ExplosivePatch : MonoBehaviour
{
    public event Action<ExplosivePatch> Exploded;

    private float hp;

    public void Initialize(float startingHp)
    {
        hp = startingHp;
        gameObject.SetActive(true);
    }

    public void ApplyDamage(float dmg)
    {
        if (dmg <= 0f) return;

        hp -= dmg;
        if (hp <= 0f)
            Exploded?.Invoke(this);
    }
}
