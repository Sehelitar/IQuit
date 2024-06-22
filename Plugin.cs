using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace IQuit;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static PluginConfigStruct GameConfig { get; private set; }
    private static Harmony _globalHarmony = null!;
    
    private void Awake()
    {
        Log = Logger;

        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        
        // Apply Evaisa's NetworkPatcher
        PatchNetwork();

        _globalHarmony = new Harmony(PluginInfo.PLUGIN_NAME);
        _globalHarmony.PatchAll();
        
        GameConfig = new PluginConfigStruct
        {
            AllowOthers = Config.Bind("General", "AllowOthers", false, "Allow non-host players to quit."),
            FastReset = Config.Bind("General", "FastReset", true, "Instantly reset your game and bypass the eject sequence.")
        };
    }
    
    private static void PatchNetwork()
    {
        try
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes =
                        method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length <= 0) continue;
                    Log.LogInfo("Initialize network patch for " + type.FullName);
                    method.Invoke(null, null);
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }
    
}

internal struct PluginConfigStruct
{
    public ConfigEntry<bool> AllowOthers;
    public ConfigEntry<bool> FastReset;
}