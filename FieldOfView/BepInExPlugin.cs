using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace FieldOfView
{
    [BepInPlugin("aedenthorn.FieldOfView", "Field of View", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> useScrollWheel;
        public static ConfigEntry<float> currentFOV;
        public static ConfigEntry<float> incrementFast;
        public static ConfigEntry<float> incrementNormal;
        public static ConfigEntry<string> modKeyNormal;
        public static ConfigEntry<string> modKeyFast;
        public static ConfigEntry<string> keyIncrease;
        public static ConfigEntry<string> keyDecrease;
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
            
            useScrollWheel = Config.Bind<bool>("Options", "UseScrollWheel", true, "Use scroll wheel to adjust FOV");
            currentFOV = Config.Bind<float>("Options", "CurrentFOV", 80f, "Current field of view.");
            incrementFast = Config.Bind<float>("Options", "IncrementFast", 5, "Fast increment speed.");
            incrementNormal = Config.Bind<float>("Options", "IncrementNormal", 1, "Normal increment speed.");
            modKeyNormal = Config.Bind<string>("Options", "ModKeyNormal", "left ctrl", "Modifier key to increment at normal speed.");
            modKeyFast = Config.Bind<string>("Options", "ModKeyFast", "left alt", "Modifier key to increment at fast speed.");
            keyIncrease = Config.Bind<string>("Options", "KeyIncrease", "", "Key to increase FOV.");
            keyDecrease = Config.Bind<string>("Options", "KeyDecrease", "", "Key to decrease FOV.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(EdgeLord), "Update")]
        static class EdgeLord_Update_Patch
        {
            static void Postfix(EdgeLord __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (
                    (useScrollWheel.Value && Input.mouseScrollDelta.y != 0 && (CheckKeyHeld(modKeyNormal.Value) || CheckKeyHeld(modKeyFast.Value))) ||
                    ((CheckKeyDown(keyIncrease.Value) || CheckKeyDown(keyDecrease.Value)) && (CheckKeyHeld(modKeyNormal.Value, false) || CheckKeyHeld(modKeyFast.Value, false)))
                )
                {
                    float change = CheckKeyHeld(modKeyFast.Value) ? incrementFast.Value : incrementNormal.Value;

                    if (Input.mouseScrollDelta.y > 0)
                        currentFOV.Value -= change;
                    else if (Input.mouseScrollDelta.y < 0)
                        currentFOV.Value += change;
                    else if (CheckKeyDown(keyIncrease.Value))
                        currentFOV.Value += change;
                    else if (CheckKeyDown(keyDecrease.Value))
                        currentFOV.Value -= change;

                    currentFOV.Value = Mathf.Clamp(currentFOV.Value, 1, 180);
                    //Dbgl($"scrolling {Input.mouseScrollDelta.y}; camera fov {settings.FieldOfView}");
                    Dbgl($"camera {Camera.main.gameObject.name} {Camera.main.fieldOfView}");
                }


                Camera.main.fieldOfView = currentFOV.Value;


            }

        }
        [HarmonyPatch(typeof(FOVLerper), "Update")]
        static class FOVLerper_Update_Patch
        {
            static void Prefix(FOVLerper __instance)
            {
                if (!modEnabled.Value)
                    return;
                __instance.maximum = Mathf.Clamp(currentFOV.Value, 1, 180);
                __instance.minimum = Mathf.Clamp(currentFOV.Value - 20, 1, 180);
            }

        }
        [HarmonyPatch(typeof(FOVLerper), "CorHookFOV")]
        static class FOVLerper_CorHookFOV_Patch
        {
            static bool Prefix(FOVLerper __instance)
            {
                if (!modEnabled.Value)
                    return true;
                context.StopAllCoroutines();
                context.StartCoroutine(context.CorHookFOV(__instance));
                return false;
            }

        }
        private IEnumerator CorHookFOV(FOVLerper fovLerper)
        {
            Dbgl("Lerping");
            typeof(FOVLerper).GetField("notnow", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(fovLerper, true);
            while (fovLerper.GetComponent<Camera>().fieldOfView < currentFOV.Value)
            {
                fovLerper.GetComponent<Camera>().fieldOfView += 1;
                yield return new WaitForSeconds(0.01f);
            }
            typeof(FOVLerper).GetField("t", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 0);
            fovLerper.maximum = Mathf.Clamp(currentFOV.Value, 1,180);
            fovLerper.minimum = Mathf.Clamp(currentFOV.Value - 20, 1, 180);
            typeof(FOVLerper).GetField("notnow", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(fovLerper, false);
            fovLerper.escalator = 4;
            yield break;
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
