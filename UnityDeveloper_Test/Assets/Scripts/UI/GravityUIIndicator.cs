using UnityEngine;
using TMPro;

/// <summary>
/// Displays current gravity selection direction on screen UI.
/// </summary>
public class GravityUIIndicator : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;   // parent panel
    [SerializeField] private TextMeshProUGUI label;  // direction label text

    public void Show(string directionName)
    {
        panelRoot.SetActive(true);
        label.text = $"Gravity → {directionName}";
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }
}