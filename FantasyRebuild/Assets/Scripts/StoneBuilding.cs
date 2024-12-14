using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneBuilding : ResourceBuilding
{
    Inventory inventory;

    private void Awake()
    {
        //set inventory
        inventory = Inventory.instance;
    }

    public override void Collect()
    {
        inventory.AddStone(stock);

        base.Collect();
    }
}
