using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryUIState { ItemSelection, PartySelection, Busy}
public class InventoryUI : MonoBehaviour
{
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [SerializeField] Image itemIcon;
    [SerializeField] Text itemDescription;

    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    [SerializeField] PartyScreen partyScreen;

    const int itemsInViewport = 8;
    List<ItemSlotUI> slotUIList;
    InventoryUIState state;
    Inventory inventory;
    RectTransform itemListRect;
    int selectedItem = 0;
    private void Awake()
    {
        inventory = Inventory.GetInventory();
        itemListRect = itemList.GetComponent<RectTransform>();
    }
    private void Start()
    {
        UpdateItemList();

        inventory.OnUpdated += UpdateItemList;
    }
    void UpdateItemList()
    {
        // Clear all the existing items;
        foreach(Transform child in itemList.transform)
        {
            Destroy(child.gameObject);
        }
        slotUIList = new List<ItemSlotUI>();
        foreach (var itemSlot in inventory.Slots)
        {
            var slotUIobj = Instantiate(itemSlotUI, itemList.transform);
            slotUIobj.SetData(itemSlot);

            slotUIList.Add(slotUIobj);
        }
        UpdateItemSelection();
    }
    public void HandleUpdate(Action onBack)
    {
        if(state == InventoryUIState.ItemSelection)
        {
            int preSelection = selectedItem;

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ++selectedItem;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                --selectedItem;
            }

            selectedItem = Mathf.Clamp(selectedItem, 0, inventory.Slots.Count - 1);

            if (preSelection != selectedItem)
                UpdateItemSelection();
            if (Input.GetKeyDown(KeyCode.Z))
            {
                OpenPartyScreen();
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                onBack?.Invoke();
            }
        }
        else if(state == InventoryUIState.PartySelection)
        {
            Action onSelect = () =>
            {
                StartCoroutine(UseItem());
            };

            Action onBackPattyScreen = () =>
            {
                ClosePartyScreen();
            };
            partyScreen.HandleUpdate(onSelect, onBackPattyScreen);
        }
    }

    void UpdateItemSelection()
    {
        for (int i = 0; i < slotUIList.Count; i++)
        {
            if (i == selectedItem)
                slotUIList[i].NameText.color = GlobalSettings.i.HighlightedColor;
            else
                slotUIList[i].NameText.color = Color.black;
        }

        var slots = inventory.Slots[selectedItem].Item;

        itemIcon.sprite = slots.Icon;
        itemDescription.text = slots.Description;

        HandleScrolling();
    }
    
    IEnumerator UseItem()
    {
        state = InventoryUIState.Busy;

        var useItem = inventory.UseItem(selectedItem, partyScreen.SelectedMember);

        if(useItem != null)
        {
            yield return DialogManager.Instance.ShowDialogText($"The player use {useItem.Name}");
        }
        else
        {
            yield return DialogManager.Instance.ShowDialogText($"It won't have any affect");
        }

        ClosePartyScreen();
    }

    void HandleScrolling()
    {

        if (slotUIList.Count <= itemsInViewport) return;
        float scrollPos = Mathf.Clamp(selectedItem - itemsInViewport/2 ,0, selectedItem) * slotUIList[0].Height;
        itemListRect.localPosition = new Vector2(itemListRect.localPosition.x, scrollPos);

        bool showUpArrow = selectedItem > itemsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);

        bool showDownArrow = selectedItem + itemsInViewport / 2 < slotUIList.Count;
        downArrow.gameObject.SetActive(showDownArrow);

    }
    void OpenPartyScreen()
    {
        state = InventoryUIState.PartySelection;
        partyScreen.gameObject.SetActive(true);
    }
    void ClosePartyScreen()
    {
        state = InventoryUIState.ItemSelection;
        partyScreen.gameObject.SetActive(false);
    }
}

