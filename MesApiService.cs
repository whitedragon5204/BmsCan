using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Bms88122
{
    public class MesSettingDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string ProduceCode { get; set; }
        public string ProduceName { get; set; }
        public int Type { get; set; }
        public int Enable { get; set; }
        public decimal UpperLimit { get; set; }
        public decimal LowerLimit { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public string CreateUser { get; set; }
        public string CreateTime { get; set; }
        public string UpdateUser { get; set; }
        public string UpdateTime { get; set; }
    }

    public static class MesApiService
    {
        private const string ApiUrl = "http://newmes.hdc-group.cn/core/outer/mesApi/getEnableAll";
        private const string TargetProduceCode = "M0-BMS-TX1";
        private const int TimeoutMs = 3000;

        public static List<MesSettingDto> GetAllSettings()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(ApiUrl);
                request.Method = "GET";
                request.Timeout = TimeoutMs;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = reader.ReadToEnd();
                    var jo = JObject.Parse(json);

                    if ((bool?)jo["success"] != true) return null;

                    var data = jo["data"] as JArray;
                    if (data == null) return null;

                    var result = new List<MesSettingDto>();
                    foreach (var item in data)
                    {
                        var dto = new MesSettingDto
                        {
                            Id = (int?)item["id"] ?? 0,
                            Code = (string)item["code"],
                            ProduceCode = (string)item["produceCode"],
                            ProduceName = (string)item["produceName"],
                            Type = (int?)item["type"] ?? 0,
                            Enable = (int?)item["enable"] ?? 0,
                            UpperLimit = (decimal?)(item["upperLimit"]) ?? 0m,
                            LowerLimit = (decimal?)(item["lowerLimit"]) ?? 0m,
                            Name = (string)item["name"],
                            Remark = (string)item["remark"],
                            CreateUser = (string)item["createUser"],
                            CreateTime = (string)item["createTime"],
                            UpdateUser = (string)item["updateUser"],
                            UpdateTime = (string)item["updateTime"],
                        };
                        result.Add(dto);
                    }
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Dictionary<string, MesSettingDto> GetBmsTxThresholds()
        {
            var all = GetAllSettings();
            if (all == null) return null;

            var result = new Dictionary<string, MesSettingDto>();
            foreach (var dto in all)
            {
                if (dto.ProduceCode != TargetProduceCode) continue;
                if (dto.Enable != 1) continue;
                if (string.IsNullOrWhiteSpace(dto.Code)) continue;

                if (!result.ContainsKey(dto.Code))
                {
                    result[dto.Code] = dto;
                }
                else
                {
                    var existing = result[dto.Code];
                    if (string.CompareOrdinal(dto.UpdateTime, existing.UpdateTime) > 0)
                        result[dto.Code] = dto;
                }
            }
            return result;
        }
    }
}
