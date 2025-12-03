using System;
using System.IO;
using System.Text;
using LongerBuff.Data;
using UnityEngine;

namespace LongerBuff.Utilities
{
    public static class BuffExporter
    {
        private const string FileName = "LongerBuff_Database_Export.csv";
        private const string LogTag = "[LongerBuff.BuffExporter]";

        public static void ExportDatabase()
        {
            try
            {
                var sb = new StringBuilder();
                
                // 1. 写入 CSV 表头
                sb.AppendLine("ID,显示名称,内部名称,基础时长,最大层数,排他标签,允许延长(AllowExtension),是否增益(IsBeneficial)");

                // 2. 获取所有数据
                var allBuffs = KnownBuffDatabase.AllBuffs;

                // 3. 遍历写入
                foreach (var buff in allBuffs)
                {
                    // 处理可能包含逗号的名称，用引号包裹
                    string displayName = EscapeCsv(buff.DisplayName);
                    string internalName = EscapeCsv(buff.InternalName);
                    string tag = string.IsNullOrEmpty(buff.ExclusionTag) ? "None" : buff.ExclusionTag;
                    
                    // 时长显示优化：-1 显示为 Infinite
                    string durationStr = buff.Duration < 0 ? "Infinite" : buff.Duration.ToString("F1");

                    sb.AppendLine($"{buff.Id},{displayName},{internalName},{durationStr},{buff.MaxStack},{tag},{buff.AllowExtension},{buff.IsBeneficial}");
                }

                // 4. 保存文件
                // 保存路径：游戏根目录
                string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, FileName);
                
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                
                Debug.Log($"{LogTag} 数据库已成功导出至: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogTag} 导出数据库失败: {ex.Message}");
            }
        }

        // CSV 转义辅助函数
        private static string EscapeCsv(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Contains(","))
            {
                return $"\"{str}\"";
            }
            return str;
        }
    }
}