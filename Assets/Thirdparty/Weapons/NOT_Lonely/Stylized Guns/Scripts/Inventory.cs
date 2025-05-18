namespace NL
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class Inventory : MonoBehaviour
    {
        private List<WeaponBase> weapons;
        private PlayerInput playerInput;
        public static int currentlyActiveItemIndex;
        public static WeaponBase currentlyActiveItem;

        private void Awake()
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }

        private void OnEnable()
        {
            weapons = GetComponentsInChildren<WeaponBase>(true).ToList();
            playerInput.OnWeaponChange += TryChangeWeaponByWheel;
        }

        private void OnDisable()
        {
            playerInput.OnWeaponChange -= TryChangeWeaponByWheel;
        }

        public void TryChangeWeaponByWheel(float direction)
        {
            if (direction > 0)
            {
                if (currentlyActiveItemIndex >= weapons.Count - 1)
                {
                    GetWeapon(0, direction);
                }
                else
                {
                    GetWeapon(currentlyActiveItemIndex + 1, direction);
                }
            }
            else if (direction < 0)
            {
                if (currentlyActiveItemIndex <= 0)
                    GetWeapon(weapons.Count - 1, direction);
                else
                    GetWeapon(currentlyActiveItemIndex - 1, direction);
            }
        }

        public void GetWeapon(int itemId, float direction)
        {
            if (currentlyActiveItemIndex != itemId)
            {
                currentlyActiveItem = weapons[itemId];

                currentlyActiveItem.gameObject.SetActive(true);

                for (int i = 0; i < weapons.Count; i++)
                {
                    if (weapons[i] != currentlyActiveItem) weapons[i].gameObject.SetActive(false);
                }
            }

            currentlyActiveItemIndex = itemId;
        }
    }
}