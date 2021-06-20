using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [SerializeField] Image itemIcon;
    [SerializeField] Text itemDescription;
 
    List<ItemSlotUI> slotUIList;
    Inventory inventory;
    int selectedItem = 0;
    private void Awake()
    {
        inventory = Inventory.GetInventory();
    }
    private void Start()
    {
        UpdateItemList();   
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

        if (Input.GetKeyDown(KeyCode.X))
        {
            onBack?.Invoke();
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
    }
}
