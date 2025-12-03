using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LongerBuff.Utilities; 

// 避免冲突，使用别名
using RuntimeBuffInfo = LongerBuff.Utilities.BuffInfo;
using DataBuffInfo = LongerBuff.Data.BuffInfo;

namespace LongerBuff.Data
{
    /// <summary>
    /// Buff 基础信息定义
    /// </summary>
    public class BuffInfo
    {
        public int Id { get; private set; }
        public string InternalName { get; private set; }
        public string DisplayName { get; private set; }
        public float Duration { get; private set; } // -1 代表无限
        public int MaxStack { get; private set; }
        public string ExclusionTag { get; private set; }
        
        /// <summary>
        /// 是否允许 Mod 修改/延长此 Buff 的时间 (默认为 True)
        /// </summary>
        public bool AllowExtension { get; set; }

        /// <summary>
        /// 是否为正面/增益效果 (默认为 False)
        /// </summary>
        public bool IsBeneficial { get; set; }

        public BuffInfo(int id, string internalName, string displayName, float duration, int maxStack, string exclusionTag = null, bool allowExtension = true, bool isBeneficial = false)
        {
            Id = id;
            InternalName = internalName;
            DisplayName = displayName;
            Duration = duration;
            MaxStack = maxStack;
            ExclusionTag = exclusionTag;
            AllowExtension = allowExtension;
            IsBeneficial = isBeneficial;
        }

        /// <summary>
        /// 更新数值 (用于从游戏运行时数据同步)
        /// </summary>
        public void UpdateRuntimeValues(float newDuration, int newMaxStack, string newExclusionTag)
        {
            // 只有当数值真的改变时才更新，避免无意义的操作
            if (!Mathf.Approximately(Duration, newDuration) || MaxStack != newMaxStack)
            {
                // Debug.Log($"[LongerBuff] 同步 Buff ID:{Id} ({DisplayName}) - 时长: {Duration}->{newDuration}, 层数: {MaxStack}->{newMaxStack}");
                Duration = newDuration;
                MaxStack = newMaxStack;
                if (!string.IsNullOrEmpty(newExclusionTag))
                {
                    ExclusionTag = newExclusionTag;
                }
            }
        }

        public bool IsInfinite => Duration < 0;
    }

    /// <summary>
    /// 动态 Buff 注册中心
    /// </summary>
    public static class ModBuffRegistry
    {
        public static bool RegisterNewBuff(int id, string internalName, string displayName, float duration, int maxStack, string exclusionTag = null, bool allowExtension = true, bool isBeneficial = false)
        {
            return KnownBuffDatabase.RegisterExternal(new DataBuffInfo(id, internalName, displayName, duration, maxStack, exclusionTag, allowExtension, isBeneficial));
        }

        public static bool Register(DataBuffInfo buffInfo)
        {
            return KnownBuffDatabase.RegisterExternal(buffInfo);
        }
    }

    /// <summary>
    /// 已知 Buff 数据库
    /// </summary>
    public static class KnownBuffDatabase
    {
        private const string LogTag = "[LongerBuff.BuffDatabase]";
        
        private static readonly Dictionary<int, DataBuffInfo> _buffs = new Dictionary<int, DataBuffInfo>();
        public static IReadOnlyList<DataBuffInfo> AllBuffs => _buffs.Values.ToList();

        // 标记是否已经完成了与游戏的同步
        private static bool _hasSyncedWithGame = false;

        static KnownBuffDatabase()
        {
            InitializeHardcodedBuffs();
        }

        /// <summary>
        /// 游戏运行时数据同步
        /// </summary>
        public static void SyncWithGame()
        {
            if (_hasSyncedWithGame) return;
            
            List<RuntimeBuffInfo> gameBuffs = BuffHelper.GetAllRegisteredBuffs();
            
            int updatedCount = 0;
            int newModCount = 0;

            foreach (var gameBuff in gameBuffs)
            {
                float runtimeDuration = gameBuff.LimitedLifeTime ? gameBuff.TotalLifeTime : -1.0f;
                string runtimeTag = gameBuff.ExclusiveTag.ToString();
                if (runtimeTag == "NotExclusive") runtimeTag = null;

                if (_buffs.TryGetValue(gameBuff.ID, out DataBuffInfo existingInfo))
                {
                    // 已知 Buff验证
                    existingInfo.UpdateRuntimeValues(runtimeDuration, gameBuff.MaxLayers, runtimeTag);
                    updatedCount++;
                }
                else
                {
                    // 未知 Buff自动注册
                    var newBuff = new DataBuffInfo(
                        gameBuff.ID, 
                        gameBuff.Name, 
                        gameBuff.DisplayName, 
                        runtimeDuration, 
                        gameBuff.MaxLayers, 
                        runtimeTag,
                        allowExtension: false, // 默认不延长
                        isBeneficial: false   // 默认非增益
                    );
                    _buffs.Add(gameBuff.ID, newBuff);
                    newModCount++;
                    Debug.Log($"[LongerBuff] 发现新 Mod Buff: ID {gameBuff.ID} - {gameBuff.DisplayName}");
                }
            }

            _hasSyncedWithGame = true;
            Debug.Log($"{LogTag} 同步完成。更新了 {updatedCount} 个已知 Buff，注册了 {newModCount} 个 Mod Buff。总计: {_buffs.Count}");
        }

        public static bool TryGetBuff(int id, out DataBuffInfo info)
        {
            return _buffs.TryGetValue(id, out info);
        }

        internal static bool RegisterExternal(DataBuffInfo info)
        {
            if (_buffs.ContainsKey(info.Id)) return false;
            _buffs.Add(info.Id, info);
            return true;
        }
        
        /// <summary>
        /// 获取所有允许延长的 Buff ID 列表
        /// </summary>
        /// <returns>允许延长的 Buff ID 列表</returns>
        public static List<int> GetAllowedExtensionBuffIds()
        {
            var list = new List<int>();
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{LogTag} === 当前允许延长的 Buff 列表 ===");

            foreach (var kvp in _buffs)
            {
                var buff = kvp.Value;
                if (buff.AllowExtension)
                {
                    list.Add(buff.Id);
                    sb.AppendLine($"ID: {buff.Id,-5} | {buff.DisplayName}");
                }
            }

            Debug.Log(sb.ToString());
            return list;
        }
        
        // 内部使用的添加方法
        private static void Add(int id, string name, string disp, float dur, int stack, string tag = null, bool allowExtension = false, bool isBeneficial = false)
        {
            if (!_buffs.ContainsKey(id))
            {
                _buffs.Add(id, new DataBuffInfo(id, name, disp, dur, stack, tag, allowExtension, isBeneficial));
            }
        }
        
        private static void InitializeHardcodedBuffs()
        {
            // ==========================================
            // 1. 负面状态类 (Debuff)
            // ==========================================
            #region 1. Negative Status
            Add(1001, "1001_Buff_BleedS", "出血", 25.0f, 3, "Bleeding");
            Add(1002, "1002_Buff_BleedUnlimit", "出血", -1.0f, 5, "Bleeding");
            Add(1003, "1003_Buff_BoneCrack", "骨折", -1.0f, 3, null);
            Add(1004, "1004_Buff_Wound", "创伤", -1.0f, 3, null);
            Add(1061, "1061_Buff_PoisonS", "中毒", 15.0f, 10, "Poison");
            Add(1122, "1122_Buff_PoisonLow", "弱毒", 14.0f, 15, "Poison");
            Add(1121, "1121_Buff_Burn", "点燃", 10.0f, 3, "Burning");
            Add(1125, "1125_Buff_BurnBig", "燃烧", 10.0f, 5, "Burning");
            Add(1071, "1071_Buff_Electric", "触电", 3.0f, 4, "Electric");
            Add(1073, "1073_Buff_ElectricGrenade", "触电", 3.0f, 1, null);
            Add(1111, "1111_Buff_Space", "扰动", 5.0f, 12, null);
            Add(1112, "1112_Buff_Space2", "扭曲", 5.0f, 12, null);
            Add(1117, "1117_Buff_SpaceGun", "碎裂", 5.0f, 12, "Space");
            Add(1123, "1123_Buff_Nauseous", "恶心", 10.0f, 1, "Nauseous");
            Add(1124, "1124_Buff_Ghost", "害怕", 12.0f, 8, "Ghost");
            Add(1081, "1081_Buff_Pain", "疼痛", 0.5f, 1, "Pain", false);
            Add(1041, "1041_Buff_Stun", "震慑", 7.0f, 1, "Stun");
            #endregion

            // ==========================================
            // 2. 状态免疫类 (有益)
            // ==========================================
            #region 2. Immunity
            Add(1019, "1019_buff_Injector_BleedResist", "出血免疫", 60.0f, 1, "Bleeding", true, true);
            Add(1491, "1491_buff_equip_BleedResist", "出血免疫", 1.5f, 1, "Bleeding", false, true);
            Add(1492, "1492_buff_equip_PoisonResist", "免疫中毒", 1.5f, 1, "Poison", false, true);
            Add(1493, "1493_buff_equip_ElecResist", "免疫感电", 1.5f, 1, "Electric", false, true);
            Add(1494, "1494_buff_equip_BurnResist", "免疫点燃", 1.5f, 1, "Burning", false, true);
            Add(1495, "1495_buff_equip_SpaceResist", "免疫碎裂", 1.5f, 1, "Space", false, true);
            Add(1496, "1496_buff_equip_NauseousResist", "免疫恶心", 1.5f, 1, "Nauseous", false, true);
            Add(1497, "1497_buff_equip_StunResist", "免疫震慑", 1.5f, 1, "Stun", false, true);
            Add(1498, "1498_buff_equip_GhostResist", "免疫害怕", 1.5f, 1, "Ghost", false, true);
            #endregion

            // ==========================================
            // 3. 基础属性类
            // ==========================================
            #region 3. Weight
            Add(1021, "1021_Buff_Weight_Light", "轻盈", -1.0f, 1, "Weight");
            Add(1022, "1022_Buff_Weight_Heavy", "负重", -1.0f, 1, "Weight");
            Add(1023, "1023_Buff_Weight_SuperHeavy", "超重", -1.0f, 1, "Weight");
            Add(1024, "1024_Buff_Weight_Overweight", "无法承受", -1.0f, 1, "Weight");
            #endregion

            // ==========================================
            // 4. 生存类
            // ==========================================
            #region 4. Survival
            Add(1, "0001_Buff_Thirsty", "脱水", -1.0f, 1, "Thirsty");
            Add(1032, "1032_Buff_Starve", "饥饿", -1.0f, 3, "Starve");
            #endregion

            // ==========================================
            // 5. 增益药剂类 (有益)
            // ==========================================
            #region 5. Injectors
            Add(1011, "1011_Buff_AddSpeed", "加速", 120.0f, 1, null, true, true);
            Add(1014, "1014_Buff_InjectorStamina", "持久", 120.0f, 1, null, true, true);
            Add(1013, "1013_Buff_InjectorArmor", "硬化", 120.0f, 1, null, true, true);
            Add(1012, "1012_Buff_InjectorMaxWeight", "负重提升", 240.0f, 1, null, true, true);
            Add(1015, "1015_Buff_InjectorMeleeDamage", "力量", 30.0f, 1, null, true, true);
            Add(1016, "1016_Buff_InjectorMeleeDamageDebuff", "萎靡", 60.0f, 1, null, false, false); // 负面副作用
            Add(1017, "1017_Buff_InjectorRecoilControl", "强翅", 60.0f, 1, null, true, true);
            Add(1018, "1018_Buff_HealForWhile", "回复", 30.0f, 1, null, true, true);
            #endregion

            // ==========================================
            // 6. 抗性类 (有益)
            // ==========================================
            #region 6. Resistances
            Add(1072, "1072_Buff_ElecResistShort", "抗电", 120.0f, 1, null, true, true);
            Add(1074, "1074_Buff_FireResistShort", "抗火", 120.0f, 1, null, true, true);
            Add(1075, "1075_Buff_PoisonResistShort", "抗毒", 120.0f, 1, null, true, true);
            Add(1076, "1076_Buff_SpaceResistShort", "抗空间", 120.0f, 1, null, true, true);
            Add(1115, "1115_Buff_SpaceResistLow", "空间减伤（小）", 120.0f, 1, "Space", true, true);
            Add(1116, "1116_Buff_SpaceResistHigh", "空间减伤（大）", 120.0f, 1, "Space", true, true);
            Add(1113, "1113_Buff_StormProtection1", "弱效空间抵抗", 120.0f, 1, "StormProtection", true, true);
            Add(1114, "1114_Buff_StormProtection2", "强效空间抵抗", 120.0f, 1, "StormProtection", true, true);
            #endregion

            // ==========================================
            // 7. 痛觉系统（不确定区别）
            // ==========================================
            #region 7. Pain
            Add(1082, "1082_Buff_PainResistShort", "镇静", 45.0f, 1, "Pain", true, true);
            Add(1083, "1083_Buff_PainResistMiddle", "镇静", 180.0f, 1, "Pain", true, true);
            Add(1084, "1084_Buff_PainResistLong", "镇静", 300.0f, 1, "Pain", true, true);
            #endregion

            // ==========================================
            // 8. 情绪类
            // ==========================================
            #region 8. Emotions
            Add(1091, "1091_Buff_HotBlood", "热血", 180.0f, 1, null, true, true);
            Add(1092, "1092_Buff_Injector_HotBlood_Trigger", "易怒", 60.0f, 1, null, true, true); // 热血针剂预备buff
            Add(1093, "1093_Buff_Injector_HotBlood_SpeedDamage", "愤怒", 30.0f, 7, null, true, true); // 热血针剂触发
            Add(1101, "1101_Buff_Happy", "高兴", 90.0f, 1, "Pain", true, true);
            #endregion

            // ==========================================
            // 9. 潜行/视觉 (有益)
            // ==========================================
            #region 9. Vision
            Add(1201, "1201_Buff_NightVision", "明视", 200.0f, 1, null, true, true); // 胡萝卜
            Add(1202, "1202_Buff_PaperBox", "伪装", 200.0f, 1, null, false, true);
            Add(1204, "1204_Buff_PaperBoxMelee", "伪装", 1.0f, 1, null, false, true);
            #endregion

            // ==========================================
            // 10-13. Boss, Totem, Hidden, Equip
            // ==========================================
            #region Misc
            // Boss
            Add(1301, "1301_Buff_Boss_Heal_StormFire", "回复", -1.0f, 1, null);
            Add(1302, "1302_Buff_Boss_Heal_StormSpace", "回复", -1.0f, 1, null);
            Add(1303, "1303_Buff_Boss_Trigger_School", "易怒", -1.0f, 1, null);
            Add(1304, "1304_Buff_Boss_SpeedDamage_School", "愤怒", 30.0f, 10, null);
            Add(1305, "1305_Buff_Boss_Hurt_StormSpace", "干枯", -1.0f, 3, null);
            Add(1306, "1306_Buff_Boss_Hurt_StormFire", "干枯", -1.0f, 3, null);
            Add(1307, "1307_Buff_Boss_RedBoss", "*Buff_Red*", -1.0f, 3, null);
            
            // Totem
            Add(1481, "1481_Buff_Totem_Heal1", "回复", 30.0f, 1, null, true, true);
            Add(1900, "1900_buff_Totem_Describe_hurt", "图腾诅咒", 1.2f, 1, null);

            // Hidden
            Add(1205, "1205_Buff_DarkCarrot", "1000%", 12.0f, 1, null, false, true);
            Add(1499, "1203_Buff_RedEye", "???", 999.0f, 1, null);
            
            // Equip
            Add(1401, "1401_buff_equip_Hurt", "干枯", 25.0f, 3, null);
            Add(1402, "1402_buff_equip_FC_Buff", "高手", 10.0f, 10, null, false, true);
            Add(1403, "1403_buff_equip_FC_Remove", "高手", 10.0f, 1, null);
            Add(1051, "1051_Buff_Base", "基地", -1.0f, 1, null);
            #endregion
        }
    }
}