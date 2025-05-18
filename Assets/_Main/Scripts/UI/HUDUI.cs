using TMPro;
using UnityEngine;

public class HUDUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;

    public void UpdateAmmoText(int currentAmmo, int totalAmmo)
    {
         ammoText.text = currentAmmo.ToString() + "/" + totalAmmo.ToString();
    }
}
