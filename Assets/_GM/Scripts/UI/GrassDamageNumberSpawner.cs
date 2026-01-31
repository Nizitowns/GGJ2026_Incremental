using DamageNumbersPro;
using UnityEngine;
public class GrassDamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private DamageNumber guiNumberPrefab; // GUI version prefab
    [SerializeField] private RectTransform rectParent;     // DamageNumbersRoot under Canvas
    [SerializeField] private Camera worldCamera;           // usually Camera.main

    public void SpawnAtWorld(Vector3 worldPos, float dmg)
    {
        if (!guiNumberPrefab || !rectParent) return;
        if (!worldCamera) worldCamera = Camera.main;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPos);

        // Convert screen point -> anchored position in rectParent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectParent, screen, null, out Vector2 anchoredPos
        );

        guiNumberPrefab.SpawnGUI(rectParent, anchoredPos, dmg);
    }
}
