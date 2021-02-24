﻿using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;
using static PluginSettings;

public class BuyFoodState : State
{
    public override string DisplayName => "WV Buying Food";

    private static readonly Dictionary<int, HashSet<int>> FoodDictionary = new Dictionary<int, HashSet<int>>
    {
        { 85, new HashSet<int>{ 35953 } },
        { 75, new HashSet<int>{ 35953 } }, // Mead Basted Caribouhl au
        { 65, new HashSet<int>{ 29451, 29449, 29450, 29448, 29452, 29453 } }, // Clefthoof Ribs
        { 61, new HashSet<int>{ 27854, 27855, 27856, 27857, 27858, 27859 } }, // Smoked Talbuk Venison -- make sure this is only used in TBC
        { 45, new HashSet<int>{ 8952, 8950, 8932, 8948, 8957} }, // Roasted Quail
        { 35, new HashSet<int>{ 4599, 4601, 3927, 4608, 6887 } }, // Cured Ham Steak
        { 25, new HashSet<int>{ 3771, 4544, 1707, 4607, 4594, 4539 } }, // Wild Hog Shank
        { 20, new HashSet<int>{ 3770, 4542, 422, 4606, 4593, 4538 } }, // Mutton Chop
        { 10, new HashSet<int>{ 2287, 4541, 414, 4605, 4592, 4538} }, // Haunch of Meat
        { 5, new HashSet<int>{ 117, 4540, 2070, 4604, 787 , 4537} }, // Haunch of Meat
        { 0, new HashSet<int>{ 117, 4540, 2070, 4604, 787 , 4536} }, // Haunch of Meat
    };

    private readonly WoWLocalPlayer Me = ObjectManager.Me;
    private Timer StateTimer = new Timer();
    private DatabaseNPC FoodVendor;
    private int FoodIdToBuy;
    private string FoodNameToBuy;
    private int FoodAmountToBuy => wManagerSetting.CurrentSetting.FoodAmount;

    public override bool NeedToRun
    {
        get
        {
            if (!StateTimer.IsReady
                || Me.Level <= 3
                || !CurrentSetting.AutobuyFood
                || FoodAmountToBuy <= 0)
                return false;

            StateTimer = new Timer(5000);

            if (Me.Level > 10) // to be moved
                NPCBlackList.AddNPCListToBlacklist(new[] { 5871, 8307, 3489 });

            if (Helpers.OutOfFood())
            {
                SetFoodAndVendor();
                if (FoodVendor == null)
                {
                    Main.Logger("Couldn't find food vendor");
                    return false;
                }

                if (!Helpers.HaveEnoughMoneyFor(FoodAmountToBuy, FoodNameToBuy))
                    return false;

                return true;
            }
            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Buying {FoodAmountToBuy} x {FoodNameToBuy} at vendor {FoodVendor.Name}");

        if (Me.Position.DistanceTo(FoodVendor.Position) >= 10)
            GoToTask.ToPosition(FoodVendor.Position);

        if (Me.Position.DistanceTo(FoodVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(FoodVendor))
                return;

            wManagerSetting.CurrentSetting.FoodName = FoodNameToBuy;
            wManagerSetting.CurrentSetting.Save();
            ClearDoNotSellListFromFoods();
            Helpers.AddItemToDoNotSellList(FoodNameToBuy);

            List<string> allFoodNames = GetPotentialFoodNames();

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(FoodVendor.Position, FoodVendor.Id, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.OpenRecordVendorItems(allFoodNames)) // also checks if vendor window is open
                {
                    // Sell first
                    Helpers.SellItems(FoodVendor);
                    if (!Helpers.HaveEnoughMoneyFor(FoodAmountToBuy, FoodNameToBuy))
                    {
                        Main.Logger("Not enough money. Item prices sold by this vendor are now recorded.");
                        Helpers.CloseWindow();
                        return;
                    }
                    Helpers.BuyItem(FoodNameToBuy, FoodAmountToBuy, 5);
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);
                    if (ItemsManager.GetItemCountById((uint)FoodIdToBuy) >= FoodAmountToBuy)
                        return;
                }
            }
            Main.Logger($"Failed to buy {FoodNameToBuy}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(FoodVendor.Id);
        }
    }

    private List<string> GetPotentialFoodNames()
    {
        List<string> allFoods = new List<string>();
        foreach (KeyValuePair<int, HashSet<int>> foods in FoodDictionary)
            foreach (int foodToAdd in foods.Value)
                allFoods.Add(ItemsManager.GetNameById(foodToAdd));
        return allFoods;
    }

    private void ClearDoNotSellListFromFoods()
    {
        foreach (KeyValuePair<int, HashSet<int>> foodList in FoodDictionary)
            foreach (int food in foodList.Value)
                Helpers.RemoveItemFromDoNotSellList(ItemsManager.GetNameById(food));
    }

    private void SetFoodAndVendor()
    {
        FoodIdToBuy = 0;
        FoodVendor = null;

        foreach (int foodId in GetListUsableFood().First())
        {
            DatabaseNPC vendorWithThisFood = Database.GetFoodVendor(new HashSet<int>() { foodId });
            if (vendorWithThisFood != null)
            {
                if (FoodVendor == null || FoodVendor.Position.DistanceTo2D(Me.Position) > vendorWithThisFood.Position.DistanceTo2D(Me.Position))
                {
                    FoodIdToBuy = foodId;
                    FoodNameToBuy = ItemsManager.GetNameById(foodId);
                    FoodVendor = vendorWithThisFood;
                }
            }
        }
    }

    private List<HashSet<int>> GetListUsableFood()
    {
        List<HashSet<int>> listFood = new List<HashSet<int>>();
        foreach (KeyValuePair<int, HashSet<int>> food in FoodDictionary)
        {
            if (food.Key <= Me.Level)
                listFood.Add(food.Value);

        }
        return listFood;
    }
}

