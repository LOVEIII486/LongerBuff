using System;
using System.Collections.Generic;
using System.Linq;
using Duckov.Buffs;
using UnityEngine;

namespace LongerBuff.Utilities
{
    /// <summary>
    /// Buff辅助工具类
    /// </summary>
    public static class BuffHelper
    {
        private const string LogTag = "[LongerBuff.BuffHelper]";

        /// <summary>
        /// 遍历并获取游戏中所有已注册的Buff
        /// </summary>
        public static List<BuffInfo> GetAllRegisteredBuffs()
        {
            var buffList = new List<BuffInfo>();
            
            try
            {
                var allBuffs = Resources.FindObjectsOfTypeAll<Buff>();
                
                foreach (var buff in allBuffs)
                {
                    if (buff.gameObject.scene.name != null) continue;
                    
                    buffList.Add(new BuffInfo
                    {
                        ID = buff.ID,
                        Name = buff.name,
                        DisplayName = buff.DisplayName,
                        DisplayNameKey = buff.DisplayNameKey,
                        TotalLifeTime = buff.TotalLifeTime,
                        LimitedLifeTime = buff.LimitedLifeTime,
                        MaxLayers = buff.MaxLayers,
                        ExclusiveTag = buff.ExclusiveTag
                    });
                }
                buffList = buffList.OrderBy(b => b.ID).ToList();
                Debug.Log($"{LogTag} 资源扫描完成: 共找到 {buffList.Count} 个Buff预制体");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogTag} 遍历Buff资源时发生错误: {ex}");
            }
            
            return buffList;
        }
        
        /// <summary>
        /// 根据ID查找Buff信息
        /// </summary>
        public static BuffInfo FindBuffByID(int id)
        {
            return GetAllRegisteredBuffs().FirstOrDefault(b => b.ID == id);
        }
    }
    
    /// <summary>
    /// 运行时Buff信息数据类
    /// </summary>
    public class BuffInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DisplayNameKey { get; set; }
        public float TotalLifeTime { get; set; }
        public bool LimitedLifeTime { get; set; }
        public int MaxLayers { get; set; }
        public Buff.BuffExclusiveTags ExclusiveTag { get; set; }
    }
}