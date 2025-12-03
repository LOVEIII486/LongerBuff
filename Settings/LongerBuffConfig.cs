using System;
using System.Collections.Generic;
using System.Linq;
using ItemStatsSystem;
using LongerBuff.ModSettingsApi;
using UnityEngine;

namespace LongerBuff.Settings
{
    public static class LongerBuffConfig
    {
        public const string Key_BuffDurationMultiplier = "BuffDurationMultiplier";
        public const string Key_AllowBuffStack = "AllowBuffStack";
        // 自定义ID列表的Key
        public const string Key_CustomExtendedBuffIds = "CustomExtendedBuffIds";
        
        private const float Default_BuffMultiplier = 1.0f;
        private const bool Default_AllowStack = true;
        
        
        public const float MaxBuffDuration = 86400f; 
        
        private static float _buffDurationMultiplier = Default_BuffMultiplier;
        private static bool _allowBuffStack = Default_AllowStack;

        // 用户自定义白名单 ID 字符串
        public static string CustomExtendedBuffIds = ""; 
        
        private static HashSet<int> _cachedCustomIds;

        public static event Action OnConfigChanged;

        /// <summary>
        /// Buff持续时间倍率
        /// </summary>
        public static float BuffDurationMultiplier
        {
            get => _buffDurationMultiplier;
            set
            {
                if (Math.Abs(_buffDurationMultiplier - value) > 0.001f)
                {
                    _buffDurationMultiplier = value;
                    OnConfigChanged?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// 是否允许buff时长叠加
        /// </summary>
        public static bool AllowBuffStack 
        { 
            get => _allowBuffStack;
            set
            {
                if (_allowBuffStack != value)
                {
                    _allowBuffStack = value;
                    OnConfigChanged?.Invoke();
                }
            }
        }
        
        public static void Load()
        {
            if (ModSettingAPI.GetSavedValue(Key_BuffDurationMultiplier, out float savedMulti))
            {
                if (savedMulti >= 1.0f && savedMulti <= 20.0f) _buffDurationMultiplier = savedMulti;
            }

            if (ModSettingAPI.GetSavedValue(Key_AllowBuffStack, out bool savedStack))
            {
                _allowBuffStack = savedStack;
            }
            
            if (ModSettingAPI.GetSavedValue(Key_CustomExtendedBuffIds, out string savedIds))
            {
                CustomExtendedBuffIds = savedIds;
                ClearCache(); // 读取新值后清理缓存
            }
        }
        
        /// <summary>
        /// 获取用户自定义的白名单 ID 集合
        /// </summary>
        public static HashSet<int> GetCustomWhiteListIds()
        {
            if (_cachedCustomIds != null) return _cachedCustomIds;

            _cachedCustomIds = new HashSet<int>();
            
            if (string.IsNullOrWhiteSpace(CustomExtendedBuffIds)) 
                return _cachedCustomIds;
            
            var ids = CustomExtendedBuffIds.Split(new[] { ',', ' ', '，', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var idStr in ids)
            {
                if (int.TryParse(idStr, out int id))
                {
                    _cachedCustomIds.Add(id);
                }
            }
            return _cachedCustomIds;
        }
        
        public static void ClearCache()
        {
            _cachedCustomIds = null;
        }
    }
}