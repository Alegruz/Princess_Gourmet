using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CraftingManager : MonoBehaviour
{
    private InventorySlot currentItemSlot;
    public Image customCursor;

    //public Slot[] craftingSlots;
    private GameObject craftingMagicCircle;
    public List<GameObject> craftingMagicCircles;

    public List<InventorySlot> itemSlotList;
    private int numCurrentItemSlots = 0;
    public string[] recipes;
    public InventoryItem[] recipeResults;
    public Slot resultSlot;

    private InventorySlot magicCircleItemSlot;

    private List<GameObject> clonedInventorySlotGameObjects;
    private GameObject clonedMagicCircleSlotGameObject;

    private void Start()
    {
        itemSlotList = new List<InventorySlot>();
        clonedInventorySlotGameObjects = new List<GameObject>();

        int maxIngredients = 0;
        foreach (GameObject craftingMagicCircleTemplate in craftingMagicCircles)
        {
            if (craftingMagicCircleTemplate.transform.childCount > maxIngredients)
            {
                craftingMagicCircle = craftingMagicCircleTemplate;
                maxIngredients = craftingMagicCircleTemplate.transform.childCount;
            }
        }

        for (int i = 0; i < craftingMagicCircle.transform.childCount; i++)
        {
            Transform childTransform = craftingMagicCircle.transform.GetChild(i);
            Slot childSlot = childTransform.gameObject.GetComponent<Slot>();
            if (childSlot == null)
            {
                Debug.Log($"마법진의 {i} 번째 slot이 null입니다!!");
                Debug.Break();
            }
            childSlot.index = i;
            itemSlotList.Add(null);
        }
    }

    private void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            if(currentItemSlot != null)
            {
                bool bIgnoreUpdate = false;

                // 이미 MagicCircle이 설정된 상태에서 MagicCircle을 새로 설정해준다면, 현재 설정된 마법진과 재료들을 rollback 해줘야 한다
                if (clonedMagicCircleSlotGameObject != null)
                {
                    InventorySlot clonedSlot = clonedMagicCircleSlotGameObject.GetComponent<InventorySlot>();
                    // 이미 마법진 설정 되어 있나?
                    if (magicCircleItemSlot != null)
                    {
                        // 근데 지금 설정하려는 게 마법진이야?
                        if (currentItemSlot.thisItem.itemType == ItemType.MagicCircle)
                        {
                            // 근데 이미 설정된 거랑 같은 마법진 아냐?
                            if (magicCircleItemSlot.thisItem != currentItemSlot.thisItem)
                            {
                                // 다른 거야~
                                OnClose();
                            }
                            else
                            {
                                // 앗 같은 거 맞네;; ㅈㅅ;;
                                bIgnoreUpdate = true;
                            }
                        }
                    }
                }

                // 드래그 앤 드롭 끝. 이제 커서에 이미지 따라다니면 안됏!
                customCursor.gameObject.SetActive(false);

                if (bIgnoreUpdate == false)
                {
                    GameObject clonedGameObject = currentItemSlot.thisManager.MakeNewInventorySlot();
                    InventorySlot newSlot = clonedGameObject.GetComponent<InventorySlot>();
                    if (newSlot)
                    {
                        newSlot.Setup(currentItemSlot.thisItem, currentItemSlot.thisManager, this);
                    }

                    // 마법진이 가득 찼거나 하면 마법진에 아이템 못 넣을 수도 있으니 bHasAddedItemToMagicCircle 불리언 변수 추가
                    bool bHasAddedItemToMagicCircle = false;
                    if (newSlot.thisItem.itemType == ItemType.Ingredient)
                    {
                        Slot nearestSlot = null;
                        float shortestDistance = float.MaxValue;

                        for (int i = 0; i < craftingMagicCircle.transform.childCount; i++)
                        {
                            Transform childTransform = craftingMagicCircle.transform.GetChild(i);
                            Slot childSlot = childTransform.gameObject.GetComponent<Slot>();
                            if (childSlot == null)
                            {
                                Debug.Log($"마법진의 {i} 번째 slot이 null입니다!!");
                                Debug.Break();
                            }

                            if (childSlot.item != null)
                            {
                                continue;
                            }

                            float dist = Vector2.Distance(Input.mousePosition, childSlot.transform.position);

                            if (dist < shortestDistance)
                            {
                                shortestDistance = dist;
                                nearestSlot = childSlot;
                            }
                        }

                        if (nearestSlot != null)
                        {
                            nearestSlot.gameObject.SetActive(true);
                            nearestSlot.GetComponent<Image>().sprite = newSlot.thisItem.itemImage;

                            // Alpha 조절로 slot에 item 보이도록
                            Color prevColor = nearestSlot.GetComponent<Image>().color;
                            prevColor.a = 1.0f;
                            nearestSlot.GetComponent<Image>().color = prevColor;

                            nearestSlot.item = newSlot.thisItem;
                            itemSlotList[nearestSlot.index] = newSlot;
                            ++numCurrentItemSlots;

                            clonedInventorySlotGameObjects.Add(clonedGameObject);

                            bHasAddedItemToMagicCircle = true;
                        }
                        // 이미 slot이 전부 다 차서 nearestSlot이 없다면?
                        else
                        {
                            Destroy(clonedGameObject);
                            clonedGameObject = null;
                        }
                    }
                    else if (newSlot.thisItem.itemType == ItemType.MagicCircle)
                    {
                        magicCircleItemSlot = newSlot;

                        if (magicCircleItemSlot.thisItem.magicCircleImageOrNull == null)
                        {
                            Debug.Log($"Item[{magicCircleItemSlot.thisItem.itemName}]에 Magic Circle 스프라이트가 설정이 안 되어 있습니다");
                            Debug.Break();
                        }

                        if (craftingMagicCircle == null || 
                            craftingMagicCircle.transform.childCount != magicCircleItemSlot.thisItem.numIngredients)
                        {
                            craftingMagicCircle = null;
                            foreach (GameObject craftingMagicCircleTemplate in craftingMagicCircles)
                            {
                                if (craftingMagicCircleTemplate.transform.childCount == magicCircleItemSlot.thisItem.numIngredients)
                                {
                                    craftingMagicCircle = craftingMagicCircleTemplate;
                                    break;
                                }
                            }
                        }

                        if (craftingMagicCircle == null)
                        {
                            Debug.Log($"사용하려는 마법진의 재료 개수는 {magicCircleItemSlot.thisItem.numIngredients} 개인데, 이에 해당하는 Magic Circle Template이 Crafting Manager에 등록되지 않았습니다!!");
                            Debug.Break();
                        }

                        Image magicCircle = craftingMagicCircle.GetComponent<Image>();
                        magicCircle.sprite = magicCircleItemSlot.thisItem.magicCircleImageOrNull;
                        Color prevMagicCircleColor = magicCircle.color;
                        prevMagicCircleColor.a = 1.0f;
                        magicCircle.color = prevMagicCircleColor;

                        clonedMagicCircleSlotGameObject = clonedGameObject;

                        bHasAddedItemToMagicCircle = true;
                    }

                    //아이템 사용시 횟수 감소
                    if (bHasAddedItemToMagicCircle == true)
                    {
                        currentItemSlot.thisItem.DecreaseAmount(1);
                    }
                }

                currentItemSlot.thisManager.ClearInventorySlots();
                currentItemSlot.thisManager.MakeInventorySlots();
                currentItemSlot = null;
            }
        }
    }

    public void OnClose()
    {
        rollback();

        // 메모리 누수 막아야!
        foreach (GameObject clonedInventorySlotGameObject in clonedInventorySlotGameObjects)
        {
            Destroy(clonedInventorySlotGameObject);
        }
        clonedInventorySlotGameObjects.Clear();

        if (clonedMagicCircleSlotGameObject != null)
        {
            Destroy(clonedMagicCircleSlotGameObject);
            clonedMagicCircleSlotGameObject = null;
        }
    }

    void CheckForCreatedRecipes()
    {
        resultSlot.gameObject.SetActive(false);
        resultSlot.item = null;

        string currentRecipeString = "";
        foreach (InventorySlot itemSlot in itemSlotList)
        {
            if (itemSlot == null)
            {
                continue;
            }

            InventoryItem item = itemSlot.thisItem;
            if(item != null)
            {
                currentRecipeString += item.itemName;
            }
            else
            {
                currentRecipeString += "null";
            }
        }

        for (int i = 0; i < recipes.Length; i++)
        {
            if(recipes[i] == currentRecipeString)
            {
                resultSlot.gameObject.SetActive(true);
                //resultSlot.GetComponent<Image>().sprite = recipeResults[i].GetComponent<Image>().sprite;
                resultSlot.item = recipeResults[i];
            }
        }
    }

    public void OnClickSlot(Slot slot)
    {
        OnMouseDownItem(itemSlotList[slot.index]);
        slot.item = null;
        slot.gameObject.SetActive(false);
        itemSlotList[slot.index] = null;
    }

    public void OnMouseDownItem(InventorySlot itemSlot)
    {
        if(currentItemSlot == null)
        {
            bool canAddItems = numCurrentItemSlots < 4 && magicCircleItemSlot != null;
            bool isIngredient = itemSlot.thisManager.inventoryType == InventoryType.Ingredients && itemSlot.thisItem.itemType == ItemType.Ingredient;

            bool canAddMagicCircle = magicCircleItemSlot != null && magicCircleItemSlot.thisItem != itemSlot.thisItem;
            bool isMagicCircle = itemSlot.thisManager.inventoryType == InventoryType.MagicCircle && itemSlot.thisItem.itemType == ItemType.MagicCircle;
            if ((canAddItems && isIngredient) || isMagicCircle)
            {
                currentItemSlot = itemSlot;

                customCursor.sprite = currentItemSlot.thisItem.itemImage;
                customCursor.gameObject.SetActive(true);
            }
        }
    }

    private void rollback()
    {
        for (int slotIndex = 0; slotIndex < itemSlotList.Count; slotIndex++)
        {
            // 아이템 다시 인벤토리로 돌려 보내야 해
            if (itemSlotList[slotIndex] != null)
            {
                itemSlotList[slotIndex].thisItem.numberHeld++;
            }
            itemSlotList[slotIndex] = null;

            Transform childTransform = craftingMagicCircle.transform.GetChild(slotIndex);
            Slot childSlot = childTransform.gameObject.GetComponent<Slot>();
            if (childSlot == null)
            {
                Debug.Log($"마법진의 {slotIndex} 번째 slot이 null입니다!!");
                Debug.Break();
            }

            childSlot.item = null;

            // 종료할 땐 다시 alpha 값 0으로해서 안 보이도록
            Color prevColor = childSlot.GetComponent<Image>().color;
            prevColor.a = 0.0f;
            childSlot.GetComponent<Image>().color = prevColor;
        }
        numCurrentItemSlots = 0;

        if (magicCircleItemSlot != null)
        {
            // 아이템 다시 인벤토리로 돌려 보내야 해
            magicCircleItemSlot.thisItem.numberHeld++;

            // 종료할 땐 다시 alpha 값 0으로해서 안 보이도록
            Image magicCircle = craftingMagicCircle.GetComponent<Image>();
            Color prevMagicCircleColor = magicCircle.color;
            prevMagicCircleColor.a = 0.0f;
            magicCircle.color = prevMagicCircleColor;
        }
        magicCircleItemSlot = null;
        craftingMagicCircle = null;
    }
}
