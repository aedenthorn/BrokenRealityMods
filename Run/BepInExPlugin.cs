using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace RunMod
{
    [BepInPlugin("aedenthorn.Run", "Run Mod", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<float> runSpeedMult;
        public static ConfigEntry<float> walkSpeedMult;
        public static ConfigEntry<string> runKey;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            
            runSpeedMult = Config.Bind<float>("Options", "RunSpeedMult", 5, "Run speed multiplier.");
            walkSpeedMult = Config.Bind<float>("Options", "WalkSpeedMult", 1, "Walk speed multiplier.");
            runKey = Config.Bind<string>("Options", "RunKey", "left shift", "Hold key to run.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(CharacterController), nameof(CharacterController.Move))]
        static class CharacterController_Move_Patch
        {
            static void Prefix(ref Vector3 motion)
            {
                if (!modEnabled.Value)
                    return;

                if (CheckKeyHeld(runKey.Value))
                {
                    Dbgl("Running");
                    motion *= runSpeedMult.Value;
                }
                else 
                    motion *= walkSpeedMult.Value;


            }

        }
        public static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }
        public static bool CheckKeyHeld(string value, bool req = true)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return !req;
            }
        }
    }
}
