using HarmonyLib;
using Duckov.Buffs;
using LongerBuff.Settings;
using LongerBuff.Data;
using UnityEngine;
using System.Collections.Generic;

namespace LongerBuff.Patches
{
    [HarmonyPatch(typeof(CharacterBuffManager), "AddBuff")]
    public static class BuffDurationPatch
    {
        private const string LogTag = "[LongerBuff.Patch]";

        private static readonly AccessTools.FieldRef<Buff, float> TotalLifeTimeRef = 
            AccessTools.FieldRefAccess<Buff, float>("totalLifeTime");

        /// <summary>
        /// 前置补丁：用于捕获"加Buff之前"的剩余时间
        /// </summary>
        /// <param name="__instance">Buff管理器实例</param>
        /// <param name="buffPrefab">即将添加的Buff预制体</param>
        /// <param name="__state">用于传递给Postfix的状态变量</param>
        [HarmonyPrefix]
        public static void AddBuff_Prefix(CharacterBuffManager __instance, Buff buffPrefab, out float __state)
        {
            __state = 0f;

            if (buffPrefab == null || __instance.Buffs == null) return;

            // 寻找玩家身上是否已经存在这个ID的Buff
            for (int i = 0; i < __instance.Buffs.Count; i++)
            {
                if (__instance.Buffs[i].ID == buffPrefab.ID)
                {
                    // 记录下当前的剩余时间
                    __state = TotalLifeTimeRef(__instance.Buffs[i]);
                    break;
                }
            }
        }

        /// <summary>
        /// 后置补丁：应用时间修改
        /// </summary>
        /// <param name="__state">从Prefix传来的"旧时间"</param>
        [HarmonyPostfix]
        public static void AddBuff_Postfix(CharacterBuffManager __instance, Buff buffPrefab, float __state)
        {
            if (buffPrefab == null) return;
            
            var buffs = __instance.Buffs;
            if (buffs == null || buffs.Count == 0) return;
            
            Buff targetBuff = null;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].ID == buffPrefab.ID)
                {
                    targetBuff = buffs[i];
                    break;
                }
            }
            
            if (targetBuff == null || !targetBuff.LimitedLifeTime) return;
            if (!CheckAndRegisterBuff(targetBuff, buffPrefab))
            {
                return;
            }
            ref float lifeTimeField = ref TotalLifeTimeRef(targetBuff);
            

            float finalTime;
            
            if (LongerBuffConfig.EnableInfiniteDuration)
            {
                // 如果开启了无限模式，直接设为最大值86400秒
                // 无视叠加规则，直接拉满
                finalTime = LongerBuffConfig.MaxBuffDuration;
            }
            else
            {
                // 原有的逻辑：计算倍率和叠加
                float singleShotDuration = buffPrefab.TotalLifeTime * LongerBuffConfig.BuffDurationMultiplier;
                
                if (LongerBuffConfig.AllowBuffStack)
                {
                    finalTime = __state + singleShotDuration;
                }
                else
                {
                    finalTime = singleShotDuration;
                }
            }

            finalTime = Mathf.Min(finalTime, LongerBuffConfig.MaxBuffDuration);
            lifeTimeField = finalTime;
            
            // Debug.Log($"{LogTag} [生效] Buff: {targetBuff.DisplayName} ({targetBuff.ID}) " +
            //           $"旧时间:{__state:F1} + 增量:{singleShotDuration:F1} -> 最终:{finalTime:F1}");
        }
        
        /// <summary>
        /// 检查Buff是否在白名单，如果不在数据库中则动态注册
        /// </summary>
        private static bool CheckAndRegisterBuff(Buff runtimeBuff, Buff prefabBuff)
        {
            int buffId = runtimeBuff.ID;
            
            if (LongerBuffConfig.GetCustomBlacklistIds().Contains(buffId))
            {
                return false;
            }
            
            if (KnownBuffDatabase.TryGetBuff(buffId, out var info))
            {
                if (info.AllowExtension) return true;
                if (LongerBuffConfig.GetCustomWhiteListIds().Contains(buffId)) return true;
                return false;
            }
            
            bool userWantsToExtend = LongerBuffConfig.GetCustomWhiteListIds().Contains(buffId);

            ModBuffRegistry.RegisterNewBuff(
                id: buffId,
                internalName: prefabBuff.name,
                displayName: prefabBuff.DisplayName,
                duration: prefabBuff.TotalLifeTime,
                maxStack: prefabBuff.MaxLayers,
                exclusionTag: prefabBuff.ExclusiveTag.ToString(),
                allowExtension: userWantsToExtend,
                isBeneficial: false
            );

            if (userWantsToExtend)
            {
                Debug.Log($"{LogTag} 动态注册并延长用户指定的 Mod Buff: {prefabBuff.DisplayName} ({buffId})");
            }
            
            return userWantsToExtend;
        }
    }
}