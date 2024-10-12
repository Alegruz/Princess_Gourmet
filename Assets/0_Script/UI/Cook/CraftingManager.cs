using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static CraftingManager;
public class CraftingManager : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public PauseCookManager pauseCookManager;

    [System.Serializable]
    public class Recipe
    {
        public List<InventoryItem> ingredients = new List<InventoryItem>();
        public InventoryItem result;
    };

    private InventorySlot currentItemSlot;
    public Image customCursor;

    //public Slot[] craftingSlots;
    private GameObject craftingMagicCircle;
    public List<GameObject> craftingMagicCircles;

    public List<InventorySlot> itemSlotList;
    private int numCurrentItemSlots = 0;
    public Recipe[] recipes;
    public Slot resultSlot;

    public Transform InventoryCanvas;
    public Transform CraftingCanvas;
    public Button cookingButton;

    private InventorySlot magicCircleItemSlot;

    private List<GameObject> clonedInventorySlotGameObjects;
    private GameObject clonedMagicCircleSlotGameObject;

    private class InternalRecipe
    {
        public int recipeIndex = 0;
        public int hashcode = 0;
    }

    private Dictionary<int, InternalRecipe> internalRecipeData;

    private struct HashCodeData
    {
        public int originalHashcode;
        public int count;
        public int hashcode;
    }

    private bool isDraggingFromCraftingCanvas;

    private void Start()
    {
        itemSlotList = new List<InventorySlot>();
        clonedInventorySlotGameObjects = new List<GameObject>();
        internalRecipeData = new Dictionary<int, InternalRecipe>();

        int maxIngredients = 0;
        foreach (GameObject craftingMagicCircleTemplate in craftingMagicCircles)
        {
            if (craftingMagicCircleTemplate.transform.childCount > maxIngredients)
            {
                if (craftingMagicCircle != null)
                {
                    craftingMagicCircle.SetActive(false);
                }
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
            childSlot.craftingManager = this;
            childSlot.index = i;
            itemSlotList.Add(null);
        }

        for(int i = 0; i < recipes.Count(); i++)
        {
            Recipe recipe = recipes[i];
            InternalRecipe internalRecipe = new InternalRecipe();
            internalRecipe.recipeIndex = i;
            Dictionary<string, HashCodeData> hashcodes = new Dictionary<string, HashCodeData>();
            foreach (InventoryItem ingredient in recipe.ingredients)
            {
                int hashcode = ingredient.itemName.GetHashCode();
                HashCodeData outHashcode;
                bool result = hashcodes.TryGetValue(ingredient.itemName, out outHashcode);
                if (result == true)
                {
                    ++outHashcode.count;
                    outHashcode.hashcode = outHashcode.originalHashcode ^ outHashcode.count.GetHashCode();
                    hashcodes[ingredient.itemName] = outHashcode;
                }
                else
                {
                    outHashcode.originalHashcode = hashcode;
                    outHashcode.count = 1;
                    outHashcode.hashcode = hashcode;
                    hashcodes.Add(ingredient.itemName, outHashcode);
                }
            }

            foreach (KeyValuePair<string, HashCodeData> pair in hashcodes)
            {
                HashCodeData data = pair.Value;
                if (internalRecipe.hashcode == 0)
                {
                    internalRecipe.hashcode = data.hashcode;
                }
                else
                {
                    internalRecipe.hashcode ^= data.hashcode;
                }
            }
            internalRecipeData.Add(internalRecipe.hashcode, internalRecipe);
        }
    }

    private void OnDestroy()
    {
        OnClose(false);
    }

    // Check if two RectTransforms overlap
    private static bool AreRectTransformsOverlapping(GameObject obj1, GameObject obj2)
    {
        RectTransform rect1 = obj1.GetComponent<RectTransform>();
        RectTransform rect2 = obj2.GetComponent<RectTransform>();

        if (rect1 == null || rect2 == null)
        {
            return false;
        }

        // Check if the RectTransform of obj1 contains the screen position of obj2's corners
        Vector3[] corners1 = new Vector3[4];
        rect1.GetWorldCorners(corners1);  // Get the corners of rect1 in world space

        // Check each corner of rect2 against the bounds of rect1
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = corners1[i];
            if (RectTransformUtility.RectangleContainsScreenPoint(rect2, corner))
            {
                return true;  // Overlap detected
            }
        }

        return false;  // No overlap detected
    }

    private void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            if(currentItemSlot != null)
            {
                if (isDraggingFromCraftingCanvas)
                {
                    currentItemSlot.thisItem.numberHeld++;
                }

                bool bIgnoreUpdate = false;
                bool isCursorOverlappingWithCraftingCanvas = AreRectTransformsOverlapping(customCursor.gameObject, CraftingCanvas.gameObject);
                bool isCursorOverlappingWithInventory = AreRectTransformsOverlapping(customCursor.gameObject, InventoryCanvas.gameObject);
                if (isCursorOverlappingWithCraftingCanvas == true)
                {
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
                                    InventoryManager inventoryManager = null;
                                    foreach (var itemSlot in itemSlotList)
                                    {
                                        if (itemSlot != null)
                                        {
                                            inventoryManager = itemSlot.thisManager;
                                            break;
                                        }
                                    }

                                    OnClose(false);
                                    if (inventoryManager != null)
                                    {
                                        inventoryManager.ClearInventorySlots();
                                        inventoryManager.MakeInventorySlots();
                                    }
                                }
                                else
                                {
                                    // 앗 같은 거 맞네;; ㅈㅅ;;
                                    bIgnoreUpdate = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    bIgnoreUpdate = true;
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
                            childSlot.craftingManager = this;

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
                            nearestSlot.craftingManager = this;
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
                            if (craftingMagicCircle != null)
                            {
                                craftingMagicCircle.SetActive(false);
                                craftingMagicCircle = null;
                            }
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
                            Debug.Log($"사용하려는 마법진의 재료 개수는 {magicCircleItemSlot.thisItem.numIngredients} 개인데, 이에 해당하는 Magic Circle Template이 Crafting Manager에 등록되지 않았습니다!! Template의 child game object에 있는 Slot의 개수가 원하는 ingredient 개수와 일치하는 지 확인해주세요!!");
                            Debug.Break();
                        }

                        craftingMagicCircle.SetActive(true);
                        Image magicCircle = craftingMagicCircle.GetComponent<Image>();
                        magicCircle.sprite = magicCircleItemSlot.thisItem.magicCircleImageOrNull;
                        Color prevMagicCircleColor = magicCircle.color;
                        prevMagicCircleColor.a = 1.0f;
                        magicCircle.color = prevMagicCircleColor;

                        clonedMagicCircleSlotGameObject = clonedGameObject;

                        bHasAddedItemToMagicCircle = true;
                    }

                    //아이템 사용시 횟수 감소
                    if (bHasAddedItemToMagicCircle == true && currentItemSlot.thisItem.itemType == ItemType.Ingredient)
                    {
                        currentItemSlot.thisItem.DecreaseAmount(1);
                        CheckForCreatedRecipes();
                    }
                }

                currentItemSlot.thisManager.ClearInventorySlots();
                currentItemSlot.thisManager.MakeInventorySlots();
                currentItemSlot = null;
            }
            isDraggingFromCraftingCanvas = false;
        }
    }

    public void OnClose(bool removeItems)
    {
        if (cookingButton != null)
        {
            cookingButton.gameObject.SetActive(false);
        }
        rollback(removeItems);

        // 메모리 누수 막아야!
        if (clonedInventorySlotGameObjects != null)
        {
            foreach (GameObject clonedInventorySlotGameObject in clonedInventorySlotGameObjects)
            {
                Destroy(clonedInventorySlotGameObject);
            }
            clonedInventorySlotGameObjects.Clear();
        }

        if (clonedMagicCircleSlotGameObject != null)
        {
            Destroy(clonedMagicCircleSlotGameObject);
            clonedMagicCircleSlotGameObject = null;
        }
    }

    void CheckForCreatedRecipes()
    {
        Dictionary<string, HashCodeData> hashcodes = new Dictionary<string, HashCodeData>();
        int numItemsInSlot = 0;
        foreach (InventorySlot itemSlot in itemSlotList)
        {
            if (itemSlot == null)
            {
                continue;
            }

            ++numItemsInSlot;
            InventoryItem item = itemSlot.thisItem;
            if(item != null)
            {
                int hashcode = item.itemName.GetHashCode();

                HashCodeData outHashcode;
                bool result = hashcodes.TryGetValue(item.itemName, out outHashcode);
                if (result == true)
                {
                    ++outHashcode.count;
                    outHashcode.hashcode = outHashcode.originalHashcode ^ outHashcode.count.GetHashCode();
                    hashcodes[item.itemName] = outHashcode;
                }
                else
                {
                    outHashcode.originalHashcode = hashcode;
                    outHashcode.count = 1;
                    outHashcode.hashcode = hashcode;
                    hashcodes.Add(item.itemName, outHashcode);
                }
            }
            else
            {
                Debug.Log($"Item slot에 있는 InventoryItem이 null입니다!!");
                Debug.DebugBreak();
            }
        }

        if (numItemsInSlot == magicCircleItemSlot.thisItem.numIngredients)
        {
            int hashcodeToInsert = 0;
            foreach (KeyValuePair<string, HashCodeData> pair in hashcodes)
            {
                HashCodeData data = pair.Value;
                if (hashcodeToInsert == 0)
                {
                    hashcodeToInsert = data.hashcode;
                }
                else
                {
                    hashcodeToInsert ^= data.hashcode;
                }
            }

            InternalRecipe internalRecipe = null;
            internalRecipeData.TryGetValue(hashcodeToInsert, out internalRecipe);
            if (internalRecipe != null)
            {
                Recipe recipe = recipes[internalRecipe.recipeIndex];

                resultSlot.GetComponent<Image>().sprite = recipe.result.itemImage;
                resultSlot.gameObject.SetActive(true);
                resultSlot.item = recipe.result;

                // 종료할 땐 다시 alpha 값 0으로해서 안 보이도록
                Color prevColor = resultSlot.GetComponent<Image>().color;
                prevColor.a = 1.0f;
                resultSlot.GetComponent<Image>().color = prevColor;

                cookingButton.gameObject.SetActive(true);
            }
            else
            {
                resultSlot.gameObject.SetActive(false);
                resultSlot.item = null;

                // 종료할 땐 다시 alpha 값 0으로해서 안 보이도록
                Color prevColor = resultSlot.GetComponent<Image>().color;
                prevColor.a = 0.0f;
                resultSlot.GetComponent<Image>().color = prevColor;

                cookingButton.gameObject.SetActive(false);
            }
        }
    }

    public void OnClickSlot(Slot slot)
    {
        OnMouseDownItem(itemSlotList[slot.index], false);
        slot.item = null;
        slot.gameObject.SetActive(false);
        itemSlotList[slot.index] = null;
        isDraggingFromCraftingCanvas = true;
    }

    public void OnMouseDownItem(InventorySlot itemSlot, bool isAdding = true)
    {
        if(currentItemSlot == null)
        {
            bool canAddItems = isAdding && magicCircleItemSlot != null && craftingMagicCircle != null && numCurrentItemSlots < craftingMagicCircle.transform.childCount;
            bool isIngredient = itemSlot.thisManager.inventoryType == InventoryType.Ingredients && itemSlot.thisItem.itemType == ItemType.Ingredient;

            bool canAddMagicCircle = magicCircleItemSlot != null && magicCircleItemSlot.thisItem != itemSlot.thisItem;
            bool isMagicCircle = itemSlot.thisManager.inventoryType == InventoryType.MagicCircle && itemSlot.thisItem.itemType == ItemType.MagicCircle;
            if ((canAddItems && isIngredient) || isMagicCircle || (isAdding == false && itemSlot != null))
            {
                currentItemSlot = itemSlot;

                customCursor.sprite = currentItemSlot.thisItem.itemImage;
                customCursor.gameObject.SetActive(true);
            }
        }
    }

    private void rollback(bool removeItems)
    {
        if (craftingMagicCircle != null)
        {
            for (int slotIndex = 0; slotIndex < craftingMagicCircle.transform.childCount; slotIndex++)
            {
                // 아이템 다시 인벤토리로 돌려 보내야 해
                if (itemSlotList[slotIndex] != null && removeItems == false)
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
                childSlot.craftingManager = this;

                // 종료할 땐 다시 alpha 값 0으로해서 안 보이도록
                Color prevChildSlotColor = childSlot.GetComponent<Image>().color;
                prevChildSlotColor.a = 0.0f;
                childSlot.GetComponent<Image>().color = prevChildSlotColor;
            }

            // 종료할 땐 다시 alpha 값 0으로해서 안 보이도록
            Image magicCircle = craftingMagicCircle.GetComponent<Image>();
            Color prevMagicCircleColor = magicCircle.color;
            prevMagicCircleColor.a = 0.0f;
            magicCircle.color = prevMagicCircleColor;

            resultSlot.gameObject.SetActive(false);
            resultSlot.item = null;

            // 종료할 땐 다시 alpha 값 0으로해서 안 보이도록
            Color prevColor = resultSlot.GetComponent<Image>().color;
            prevColor.a = 0.0f;
            resultSlot.GetComponent<Image>().color = prevColor;

            cookingButton.gameObject.SetActive(false);
        }
        numCurrentItemSlots = 0;
        magicCircleItemSlot = null;
        if (craftingMagicCircle != null)
        {
            craftingMagicCircle.SetActive(false);
            craftingMagicCircle = null;
        }
        craftingMagicCircle = null;
    }

    public void OnCook()
    {
        if (resultSlot != null && resultSlot.item != null)
        {
            if (playerInventory)
            {
                InventoryItem cookedItem = resultSlot.item;
                OnClose(true);

                if (playerInventory.myInventory.Contains(cookedItem))
                {
                    cookedItem.numberHeld += 1;
                }
                else
                {
                    playerInventory.myInventory.Add(cookedItem);
                    cookedItem.numberHeld = 1;
                }

                IPauseManager.SetPausable(true);
                pauseCookManager.ChangePause(false);
                IPauseManager.SetPausable(false);
            }
        }
    }
}
