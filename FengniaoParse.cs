using System;

namespace Bms88122
{
    /// <summary>
    /// 锋鸟 V0.0.22 第 6 章 BMS 广播。小端。
    /// </summary>
    public static class FengniaoParse
    {
        static readonly string[] Part1 = { "", "YN245200A", "YN205200A", "P205200A", "P245200A", "P24S150A" };
        static readonly string[] Part2 = { "", "F3338", "G1211", "60131", "60181", "60632" };

        public static ushort U16(byte[] data, int offset)
        {
            if (data == null || offset + 1 >= data.Length)
                return 0;
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }

        public static void Apply(BmsDataModel model, uint canId, byte[] data)
        {
            if (model == null || data == null)
                return;

            if (canId == 0x400 && data.Length >= 6)
            {
                model.SOC = data[0].ToString() + "%";
                model.Temperature1 = (sbyte)data[1];
                model.TotalVoltage = U16(data, 2) * 0.1m;
            }
            else if (canId >= 0x403 && canId <= 0x40A)
            {
                int start = (int)((canId - 0x403) * 4 + 1);
                int count = canId == 0x40A ? 2 : 4;
                for (int i = 0; i < count; i++)
                {
                    ushort mv = U16(data, i * 2);
                    SetCell(model, start + i, mv);
                }
                UpdateCellStat(model);
            }
            else if (canId == 0x411 && data.Length >= 3)
            {
                model.SoftwareVersion = FormatVersion(data[0], data[1], data[2]);
            }
        }

        public static string FormatVersion(byte b0, byte b1, byte b2)
        {
            string p1 = (b0 >= 1 && b0 < Part1.Length) ? Part1[b0] : "0x" + b0.ToString("X2");
            string p2 = (b1 >= 1 && b1 < Part2.Length) ? Part2[b1] : "0x" + b1.ToString("X2");
            string ver = (b2 / 100m).ToString("0.0#");
            return p1 + "-" + p2 + "-" + ver;
        }

        static void SetCell(BmsDataModel m, int n, ushort mv)
        {
            switch (n)
            {
                case 1: m.Cell1Voltage = mv; break;
                case 2: m.Cell2Voltage = mv; break;
                case 3: m.Cell3Voltage = mv; break;
                case 4: m.Cell4Voltage = mv; break;
                case 5: m.Cell5Voltage = mv; break;
                case 6: m.Cell6Voltage = mv; break;
                case 7: m.Cell7Voltage = mv; break;
                case 8: m.Cell8Voltage = mv; break;
                case 9: m.Cell9Voltage = mv; break;
                case 10: m.Cell10Voltage = mv; break;
                case 11: m.Cell11Voltage = mv; break;
                case 12: m.Cell12Voltage = mv; break;
                case 13: m.Cell13Voltage = mv; break;
                case 14: m.Cell14Voltage = mv; break;
                case 15: m.Cell15Voltage = mv; break;
                case 16: m.Cell16Voltage = mv; break;
                case 17: m.Cell17Voltage = mv; break;
                case 18: m.Cell18Voltage = mv; break;
                case 19: m.Cell19Voltage = mv; break;
                case 20: m.Cell20Voltage = mv; break;
                case 21: m.Cell21Voltage = mv; break;
                case 22: m.Cell22Voltage = mv; break;
                case 23: m.Cell23Voltage = mv; break;
                case 24: m.Cell24Voltage = mv; break;
                case 25: m.Cell25Voltage = mv; break;
                case 26: m.Cell26Voltage = mv; break;
                case 27: m.Cell27Voltage = mv; break;
                case 28: m.Cell28Voltage = mv; break;
                case 29: m.Cell29Voltage = mv; break;
                case 30: m.Cell30Voltage = mv; break;
            }
        }

        public static decimal GetCell(BmsDataModel m, int n)
        {
            switch (n)
            {
                case 1: return m.Cell1Voltage;
                case 2: return m.Cell2Voltage;
                case 3: return m.Cell3Voltage;
                case 4: return m.Cell4Voltage;
                case 5: return m.Cell5Voltage;
                case 6: return m.Cell6Voltage;
                case 7: return m.Cell7Voltage;
                case 8: return m.Cell8Voltage;
                case 9: return m.Cell9Voltage;
                case 10: return m.Cell10Voltage;
                case 11: return m.Cell11Voltage;
                case 12: return m.Cell12Voltage;
                case 13: return m.Cell13Voltage;
                case 14: return m.Cell14Voltage;
                case 15: return m.Cell15Voltage;
                case 16: return m.Cell16Voltage;
                case 17: return m.Cell17Voltage;
                case 18: return m.Cell18Voltage;
                case 19: return m.Cell19Voltage;
                case 20: return m.Cell20Voltage;
                case 21: return m.Cell21Voltage;
                case 22: return m.Cell22Voltage;
                case 23: return m.Cell23Voltage;
                case 24: return m.Cell24Voltage;
                default: return 0;
            }
        }

        static void UpdateCellStat(BmsDataModel m)
        {
            int max = 0;
            int min = 0;
            for (int i = 1; i <= 24; i++)
            {
                int v = (int)GetCell(m, i);
                if (v <= 0)
                    continue;
                if (max == 0 || v > max)
                    max = v;
                if (min == 0 || v < min)
                    min = v;
            }
            if (max > 0 && min > 0)
            {
                m.MaxCellVoltage = max;
                m.MinCellVoltage = min;
                m.MaxVoltageDiff = max - min;
            }
        }
    }
}
