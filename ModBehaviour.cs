using System;
using HarmonyLib;
using Duckov.Modding;
using LongerBuff.Data;
using LongerBuff.Localization;
using LongerBuff.Settings;
using LongerBuff.ModSettingsApi;
using UnityEngine;

namespace LongerBuff
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static ModBehaviour Instance { get; private set; }
        
        private const string HarmonyId = "com.LOVEIII486.LongerBuff"; 
        private const string LogTag = "[LongerBuff]";
        
        private Harmony _harmony;
        private bool _isPatched = false;

        private void OnEnable()
        {
            Instance = this;
            
            if (HarmonyLoad.LoadHarmony() == null)
            {
                Debug.LogError($"{LogTag} 模组启动失败: 缺少 Harmony 依赖。");
                return;
            }
            
            InitializeHarmonyPatches();
            
            Debug.Log($"{LogTag} 模组已启用");
        }
        
        protected override void OnAfterSetup()
        {
            base.OnAfterSetup();
            
            InitializeLocalization();
            
            if (ModSettingsApi.ModSettingAPI.Init(info))
            {
                LongerBuffConfig.Load();
                SettingsUI.Register(); 
                Debug.Log($"{LogTag} 配置系统初始化完成");
            }
            else
            {
                Debug.LogError($"{LogTag} ModSetting 依赖缺失或初始化失败！");
            }
            
            KnownBuffDatabase.SyncWithGame();
            KnownBuffDatabase.GetAllowedExtensionBuffIds();
        }

        private void OnDisable()
        {
            CleanupLocalization();
            CleanupHarmonyPatches();

            Instance = null;
            Debug.Log($"{LogTag} 模组已禁用");
        }

        #region Harmony Management

        private void InitializeHarmonyPatches()
        {
            if (_isPatched) return;
            
            try
            {
                if (_harmony == null)
                {
                    _harmony = new Harmony(HarmonyId);
                }
                _harmony.PatchAll();
                _isPatched = true;
                Debug.Log($"{LogTag} Harmony 补丁应用成功");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogTag} Harmony 补丁应用失败: {ex}");
            }
        }

        private void CleanupHarmonyPatches()
        {
            if (!_isPatched || _harmony == null) return;

            try
            {
                _harmony.UnpatchAll(_harmony.Id);
                _isPatched = false;
                Debug.Log($"{LogTag} Harmony 补丁已移除");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogTag} 移除 Harmony 补丁时发生错误: {ex}");
            }
        }

        #endregion
        #region Localization

        private void InitializeLocalization()
        {
            LocalizationManager.Initialize(info.path);
            SodaCraft.Localizations.LocalizationManager.OnSetLanguage += OnLanguageChanged;
        }

        private void CleanupLocalization()
        {
            SodaCraft.Localizations.LocalizationManager.OnSetLanguage -= OnLanguageChanged;
            LocalizationManager.Cleanup();
        }

        private void OnLanguageChanged(SystemLanguage lang)
        {
            LocalizationManager.Refresh();
            SettingsUI.Register();
        }

        #endregion
    }
}