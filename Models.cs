using SqlSugar;
using System;
using System.Collections.Generic;

namespace Bms88122
{
    public class RootResponse
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Msg { get; set; }
        public List<DataItem> Data { get; set; }
    }

    public class DataItem
    {
        public string Code { get; set; }
        public string Value { get; set; }
    }

    [SugarTable("BmsDataModel")]
    public class BmsDataModel
    {
        [SugarColumn(ColumnName = "Id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string PackCode { get; set; }
        public decimal TotalVoltage { get; set; }
        public string SOC { get; set; }
        public int RemainingCapacity { get; set; }
        public string SOH { get; set; }
        public int MaxCellVoltage { get; set; }
        public int MinCellVoltage { get; set; }
        public int MaxVoltageDiff { get; set; }
        public decimal Cell1Voltage { get; set; }
        public decimal Cell2Voltage { get; set; }
        public decimal Cell3Voltage { get; set; }
        public decimal Cell4Voltage { get; set; }
        public decimal Cell5Voltage { get; set; }
        public decimal Cell6Voltage { get; set; }
        public decimal Cell7Voltage { get; set; }
        public decimal Cell8Voltage { get; set; }
        public decimal Cell9Voltage { get; set; }
        public decimal Cell10Voltage { get; set; }
        public decimal Cell11Voltage { get; set; }
        public decimal Cell12Voltage { get; set; }
        public decimal Cell13Voltage { get; set; }
        public decimal Cell14Voltage { get; set; }
        public decimal Cell15Voltage { get; set; }
        public decimal Cell16Voltage { get; set; }
        public decimal Cell17Voltage { get; set; }
        public decimal Cell18Voltage { get; set; }
        public decimal Cell19Voltage { get; set; }
        public decimal Cell20Voltage { get; set; }
        public decimal Cell21Voltage { get; set; }
        public decimal Cell22Voltage { get; set; }
        public decimal Cell23Voltage { get; set; }
        public decimal Cell24Voltage { get; set; }
        public decimal Cell25Voltage { get; set; }
        public decimal Cell26Voltage { get; set; }
        public decimal Cell27Voltage { get; set; }
        public decimal Cell28Voltage { get; set; }
        public decimal Cell29Voltage { get; set; }
        public decimal Cell30Voltage { get; set; }
        public decimal Cell31Voltage { get; set; }
        public decimal Cell32Voltage { get; set; }
        public decimal Temperature1 { get; set; }
        public decimal Temperature2 { get; set; }
        public decimal Temperature3 { get; set; }
        public decimal Temperature4 { get; set; }
        public decimal Temperature5 { get; set; }
        public decimal Temperature6 { get; set; }
        public decimal Temperature7 { get; set; }
        public decimal Temperature8 { get; set; }
        public decimal Mos { get; set; }
        public string HardwareVersion { get; set; }
        public string SoftwareVersion { get; set; }
        public DateTime CreateTime { get; set; }
    }

    [SugarTable("setting")]
    public class PQCSettingModel
    {
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(ColumnName = "code")]
        public string Code { get; set; }
        [SugarColumn(ColumnName = "name")]
        public string Name { get; set; }
        [SugarColumn(ColumnName = "flag")]
        public int Flag { get; set; }
        [SugarColumn(ColumnName = "updatetime")]
        public DateTime Updatetime { get; set; }
        [SugarColumn(ColumnName = "updateUser")]
        public string UpdateUser { get; set; }
        [SugarColumn(ColumnName = "upperLimit")]
        public decimal UpperLimit { get; set; }
        [SugarColumn(ColumnName = "lowerLimit")]
        public decimal LowerLimit { get; set; }
    }
}
