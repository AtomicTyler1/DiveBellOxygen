using HarmonyLib;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Zorro.Settings;

namespace DiveBellOxygen
{
    [ContentWarningPlugin("com.atomic.refillableoxygen", "1.1.0", false)]
    internal class ContentPrioritization
    {
        static ContentPrioritization() { new GameObject().AddComponent<Plugin>(); }
    }

    [ContentWarningSetting]
    public class RefillRate : FloatSetting, IExposedSetting
    {
        public override void ApplyValue() => Debug.Log($"Rate is now: {Value}");
        protected override float GetDefaultValue() => 1;
        protected override float2 GetMinMaxValue() => new(0, 5);
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;
        public string GetDisplayName() => "Oxygen refill rate";
    }

    [ContentWarningSetting]
    public class OpenDoor : BoolSetting, IExposedSetting
    {
        public override void ApplyValue() => Debug.Log($"Rate is now: {Value}");
        protected override bool GetDefaultValue() => true;
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;
        public string GetDisplayName() => "Divebell has to be closed to refill oxygen";
    }

    [ContentWarningSetting]
    public class LoseOxygenIfDivebellOpen : BoolSetting, IExposedSetting
    {
        public override void ApplyValue() => Debug.Log($"LoseOxygenIfDivebellClosed is now: {Value}");
        protected override bool GetDefaultValue() => false;
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;
        public string GetDisplayName() => "Lose oxygen if divebell is open.";
    }

    [ContentWarningSetting]
    public class OxygenLimit : FloatSetting, IExposedSetting
    {
        public override void ApplyValue() => Debug.Log($"Oxygen Refill Limit is now: {Value}");
        protected override float GetDefaultValue() => 100;
        protected override float2 GetMinMaxValue() => new(0, 100);
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;
        public string GetDisplayName() => "Oxygen refill limit (Refills to this amount only)";
    }



    class Plugin : MonoBehaviour
    {
        static OpenDoor? openDoor;
        static RefillRate? refillRate;
        static OxygenLimit? oxygenLimit;
        static LoseOxygenIfDivebellOpen? loseOxygenIfDivebellOpen;
        static float Percentage;

        void Awake()
        {
            Debug.Log("Refillable oxygen for divebell loaded! If you find out this mod doesn't work contact @atomictyler on discord!");
        }

        [HarmonyPatch(typeof(DivingBell), "Update")]
        public static class DivingBell_Update_Patched
        {
            public static void Postfix(DivingBell __instance)
            {
                openDoor ??= GameHandler.Instance.SettingsHandler.GetSetting<OpenDoor>();
                loseOxygenIfDivebellOpen ??= GameHandler.Instance.SettingsHandler.GetSetting<LoseOxygenIfDivebellOpen>();
                if (__instance.onSurface || openDoor.Value == true && !__instance.door.IsFullyClosed())
                {
                    return;                 
                }

                ICollection<Player> collection = __instance.playerDetector.CheckForPlayers();
                foreach (Player item in collection)
                {
                    if (loseOxygenIfDivebellOpen.Value == true && !__instance.door.IsFullyClosed() && !__instance.onSurface && openDoor.Value)
                    {
                        Player.PlayerData data = item.data;
                        data.remainingOxygen -= 10 * Time.deltaTime;
                        return;
                    }

                    if (item.data.remainingOxygen < item.data.maxOxygen)
                    {
                        refillRate ??= GameHandler.Instance.SettingsHandler.GetSetting<RefillRate>();
                        oxygenLimit ??= GameHandler.Instance.SettingsHandler.GetSetting<OxygenLimit>();
                        Player.PlayerData data = item.data;
                        Percentage = data.remainingOxygen / data.maxOxygen * 100 + 1;
                        if (oxygenLimit.Value > Percentage)
                        {
                            data.remainingOxygen += refillRate.Value * Time.deltaTime;
                        }
                    }
                }
            }
        }
    }
}
