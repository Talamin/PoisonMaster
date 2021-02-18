﻿using System;
using System.ComponentModel;
using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using wManager.Plugin;
using wManager.Wow.Helpers;
using PoisonMaster;
using System.Drawing;
using Timer = robotManager.Helpful.Timer;
using wManager;

public class Main : IPlugin
{
    private readonly BackgroundWorker _pulseThread = new BackgroundWorker();
    private static string Name = "Wholesome  Manager";
    private static bool IsLaunched;

    private Timer stateAddTimer;

    // Custom states
    public static State buyPoisonState = new BuyPoisonState();
    public static State buyArrowsState = new BuyAmmoState();
    public static State buyFoodState = new BuyFoodState();
    public static State buyDrinkState = new BuyDrinkState();
    public static State repairState = new RepairState();
    public static State sellItemsState = new SellItemsState();

    public void Initialize()
    {
        try
        {
            PluginSettings.Load();
            EventsLua.AttachEventLua("PLAYER_EQUIPMENT_CHANGED", m => Helpers.GetRangedWeaponType());
            FiniteStateMachineEvents.OnRunState += StateAddEventHandler;
            IsLaunched = true;
            _pulseThread.RunWorkerAsync();

            if (PluginSettings.CurrentSetting.AutoBuyWater || PluginSettings.CurrentSetting.AutobuyFood)
                wManagerSetting.CurrentSetting.TryToUseBestBagFoodDrink = false;

            Helpers.GetRangedWeaponType();
        }
        catch (Exception ex)
        {
            LoggerError("Something gone wrong!" + ex);
        }
    }

    public void Dispose()
    {
        IsLaunched = false;
        FiniteStateMachineEvents.OnRunState -= StateAddEventHandler;
        _pulseThread.Dispose();
        Logger("Plugin was terminated!");
    }

    public void Settings()
    {
        PluginSettings.Load();
        PluginSettings.CurrentSetting.ShowConfiguration();
        PluginSettings.CurrentSetting.Save();
    }

    private void StateAddEventHandler(Engine engine, State state, CancelEventArgs canc)
    {
        AddStates(engine);
    }

    private void AddStates(Engine engine)
    {
        if (engine.States.Count <= 5)
        {
           if (stateAddTimer == null)
                Helpers.SoftRestart(); // hack to wait for correct engine to trigger
            return;
        }

        if (stateAddTimer == null)
            stateAddTimer = new Timer();

        if (stateAddTimer.IsReady && engine != null)
		{
            stateAddTimer = new Timer(3000);

            Helpers.AddState(engine, buyPoisonState, "To Town");
            Helpers.AddState(engine, buyArrowsState, "Buying Poison");
            Helpers.AddState(engine, buyFoodState, "Buying Arrows and Bullets");
            Helpers.AddState(engine, buyDrinkState, "Buying Food");
            Helpers.AddState(engine, repairState, "Buying Drink");
            Helpers.AddState(engine, sellItemsState, "Repair Run");
		}
	}

    public static void Logger(string message)
    {
        Logging.Write($"[{Name}]: { message}", Logging.LogType.Normal, Color.ForestGreen);
    }
    public static void LoggerError(string message)
    {
        Logging.Write($"[{Name}]: { message}", Logging.LogType.Normal, Color.Red);
    }
}