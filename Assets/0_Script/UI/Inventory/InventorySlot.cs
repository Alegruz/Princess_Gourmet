using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEditor.Progress;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    [Header("UI Stuff to change")]
    [SerializeField] private TextMeshProUGUI itemNumberText;
    [SerializeField] private Image itemImage;

    [Header("Variables from the item")]
    public InventoryItem thisItem;
    public InventoryManager thisManager;
    public CraftingManager craftingManager;
    private AudioManager mAudioManager = null;

    public void Setup(InventoryItem newItem, InventoryManager newManager, CraftingManager inCraftingManager)
    {
        thisItem = newItem;
        thisManager = newManager;
        craftingManager = inCraftingManager;
        if (thisItem)
        {
            itemImage.sprite = thisItem.itemImage;
            itemNumberText.text = "" + thisItem.numberHeld;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (thisItem)
        {
            if (craftingManager != null)
            {
                if (craftingManager.isActiveAndEnabled)
                {
                    craftingManager.OnMouseDownItem(this);
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int clickCount = eventData.clickCount;

        if(thisItem)
        {
            if (clickCount == 1)
            {
                thisManager.SetupDescriptionAndButton(thisItem.itemDescription, thisItem.usable, thisItem);
                thisManager.SetupNameAndButton(thisItem.itemName, thisItem.usable, thisItem);
            }

            if (craftingManager == null)
            {
                if (mAudioManager == null)
                {
                    mAudioManager = FindObjectOfType<AudioManager>();
                }
                mAudioManager.Play("cursor");
            }
        }
    }
}
