using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GrassPatch : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private float maxHP = 3f;

    public float HP { get; private set; }
    public float MaxHP => maxHP;

    public event Action<GrassPatch> Cut;

    [Header("Electric")]
    [SerializeField] private GameObject electricVfx; // child GO (glow/particles), optional
    public bool IsElectric { get; private set; }
    [SerializeField] private GameObject beamVfx; // optional child for “beam grass” look
    public bool IsBeam { get; private set; }
    public void Initialize(float hp)
    {
        maxHP = Mathf.Max(0.01f, hp);
        HP = maxHP;

        SetElectric(false); // IMPORTANT: reset when pooled
        SetBeam(false);     // IMPORTANT: reset for pooling

        gameObject.SetActive(true);
    }
    public void SetBeam(bool on)
    {
        IsBeam = on;
        if (beamVfx) beamVfx.SetActive(on);
    }
    public void SetElectric(bool on)
    {
        IsElectric = on;
        if (electricVfx) electricVfx.SetActive(on);
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
