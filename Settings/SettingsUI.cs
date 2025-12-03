using System.Collections.Generic;
using LongerBuff.Localization;
using LongerBuff.ModSettingsApi;
using LongerBuff.Utilities;
using UnityEngine;

namespace LongerBuff.Settings
{
    public static class SettingsUI
    {
        public static void Register()
        {
            if (!ModSettingAPI.IsInit) return;

            ModSettingAPI.Clear();
            
            ModSettingAPI.AddSlider(
                LongerBuffConfig.Key_BuffDurationMultiplier,
                LocalizationManager.GetText("Setting_BuffDurationMultiplier"),
                LongerBuffConfig.BuffDurationMultiplier,
                new Vector2(1.0f, 20.0f),
                (value) =>
                {
                    LongerBuffConfig.BuffDurationMultiplier = value;
                },
                1, 5
            );
            ModSettingAPI.AddToggle(
                LongerBuffConfig.Key_AllowBuffStack,
                LocalizationManager.GetText("Setting_AllowBuffStack"),
                LongerBuffConfig.AllowBuffStack,
                (value) =>
                {
                    LongerBuffConfig.AllowBuffStack = value;
                }
            );
            ModSettingAPI.AddInput(
                LongerBuffConfig.Key_CustomExtendedBuffIds,
                LocalizationManager.GetText("Setting_CustomExtendedBuffIds"),
                LongerBuffConfig.CustomExtendedBuffIds,
                200, 
                (value) =>
                {
                    LongerBuffConfig.CustomExtendedBuffIds = value;
                    LongerBuffConfig.ClearCache();
                }
            );
            ModSettingAPI.AddButton(
                "Btn_ExportDatabase", 
                LocalizationManager.GetText("Setting_ExportDesc"), 
                LocalizationManager.GetText("Setting_ExportBtnText"), 
                () =>
                {
                    BuffExporter.ExportDatabase();
                }
            );

            // 基础设置
            ModSettingAPI.AddGroup(
                "LongerBuff_MainGroup",
                LocalizationManager.GetText("Settings_Group_Title"), // Buff时长设置
                new List<string> 
                { 
                    LongerBuffConfig.Key_BuffDurationMultiplier,
                    LongerBuffConfig.Key_AllowBuffStack
                },
                0.8f, true, true
            );

            // 高级/自定义设置
            ModSettingAPI.AddGroup(
                "LongerBuff_AdvancedGroup",
                LocalizationManager.GetText("Settings_Group_Advanced"), // 高级设置
                new List<string> 
                { 
                    LongerBuffConfig.Key_CustomExtendedBuffIds,
                    "Btn_ExportDatabase"
                },
                0.8f, false, false
            );
        }
    }
}