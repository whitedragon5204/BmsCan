using System;

namespace Bms88122
{
    /// <summary>
    /// 周立功 0x18F 扩展帧，和 BmsCan.Main 同一套拆法。
    /// </summary>
    public static class ZlgBmsParse
    {
        public static int LastMaxCellIndex;
        public static int LastMinCellIndex;

        public static void Apply(BmsDataModel model, uint location, byte[] data)
        {
            if (model == null || data == null || data.Length == 0)
                return;

            try
            {
                if (location == 0x18F004F4)
                {
                    UInt64 rawVoltage = ExtractBits(data, 0, 16);
                    model.TotalVoltage = (decimal)(rawVoltage * 0.1);
                }
                else if (location == 0x18F003F4)
                {
                    byte soc = (byte)ExtractBits(data, 32, 8);
                    byte soh = (byte)ExtractBits(data, 8, 8);
                    model.SOC = soc.ToString() + "%";
                    model.SOH = soh.ToString() + "%";
                }
                else if (location == 0x18F006F4)
                {
                    UInt16 rc = (UInt16)ExtractBits(data, 16, 16);
                    model.RemainingCapacity = rc;
                }
                else if (location == 0x18F007F4)
                {
                    int maxVoltCellNum = (int)ExtractBits(data, 0, 8);
                    int minVoltCellNum = (int)ExtractBits(data, 8, 8);
                    UInt16 maxCellVolt = (UInt16)ExtractBits(data, 16, 16);
                    UInt16 minCellVolt = (UInt16)ExtractBits(data, 32, 16);
                    model.MaxCellVoltage = maxCellVolt;
                    model.MinCellVoltage = minCellVolt;
                    model.MaxVoltageDiff = maxCellVolt - minCellVolt;
                    LastMaxCellIndex = maxVoltCellNum + 1;
                    LastMinCellIndex = minVoltCellNum + 1;
                }
                else if (location == 0x18F013F4)
                {
                    model.Cell1Voltage = (UInt16)ExtractBits(data, 0, 16);
                    model.Cell2Voltage = (UInt16)ExtractBits(data, 16, 16);
                    model.Cell3Voltage = (UInt16)ExtractBits(data, 32, 16);
                    model.Cell4Voltage = (UInt16)ExtractBits(data, 48, 16);
                }
                else if (location == 0x18F014F4)
                {
                    model.Cell5Voltage = (UInt16)ExtractBits(data, 0, 16);
                    model.Cell6Voltage = (UInt16)ExtractBits(data, 16, 16);
                    model.Cell7Voltage = (UInt16)ExtractBits(data, 32, 16);
                    model.Cell8Voltage = (UInt16)ExtractBits(data, 48, 16);
                }
                else if (location == 0x18F015F4)
                {
                    model.Cell9Voltage = (UInt16)ExtractBits(data, 0, 16);
                    model.Cell10Voltage = (UInt16)ExtractBits(data, 16, 16);
                    model.Cell11Voltage = (UInt16)ExtractBits(data, 32, 16);
                    model.Cell12Voltage = (UInt16)ExtractBits(data, 48, 16);
                }
                else if (location == 0x18F016F4)
                {
                    model.Cell13Voltage = (UInt16)ExtractBits(data, 0, 16);
                    model.Cell14Voltage = (UInt16)ExtractBits(data, 16, 16);
                    model.Cell15Voltage = (UInt16)ExtractBits(data, 32, 16);
                    model.Cell16Voltage = (UInt16)ExtractBits(data, 48, 16);
                }
                else if (location == 0x18F017F4)
                {
                    model.Cell17Voltage = (UInt16)ExtractBits(data, 0, 16);
                    model.Cell18Voltage = (UInt16)ExtractBits(data, 16, 16);
                    model.Cell19Voltage = (UInt16)ExtractBits(data, 32, 16);
                    model.Cell20Voltage = (UInt16)ExtractBits(data, 48, 16);
                }
                else if (location == 0x18F012F4)
                {
                    model.Temperature1 = (byte)ExtractBits(data, 0, 8) - 40m;
                    model.Temperature2 = (byte)ExtractBits(data, 8, 8) - 40m;
                    model.Temperature3 = (byte)ExtractBits(data, 16, 8) - 40m;
                    model.Temperature4 = (byte)ExtractBits(data, 24, 8) - 40m;
                    model.Temperature5 = (byte)ExtractBits(data, 32, 8) - 40m;
                    model.Temperature6 = (byte)ExtractBits(data, 40, 8) - 40m;
                    model.Temperature7 = (byte)ExtractBits(data, 48, 8) - 40m;
                    model.Temperature8 = (byte)ExtractBits(data, 56, 8) - 40m;
                }
                else if (location == 0x18F00DF4)
                {
                    UInt32 hwVer = (UInt32)ExtractBits(data, 0, 32);
                    UInt32 swVer = (UInt32)ExtractBits(data, 32, 32);
                    string hwVerStr = hwVer.ToString("X8");
                    string swVerStr = swVer.ToString("X8");
                    model.HardwareVersion = "V" + ParseFromHex(hwVerStr) + " " + DecodeDate(hwVerStr);
                    model.SoftwareVersion = "V" + ParseFromHex(swVerStr) + " " + DecodeDate(swVerStr);
                }
                else if (location == 0x18F01EF4)
                {
                    model.Mos = (byte)ExtractBits(data, 0, 8) - 40m;
                }
            }
            catch
            {
            }
        }

        public static string DecodeDate(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("输入不能为空");

            hex = hex.Trim();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);

            if (hex.Length < 4)
                throw new ArgumentException("至少需要4个十六进制字符");

            string code = hex.Substring(0, 4);
            ushort value = Convert.ToUInt16(code, 16);

            int year = value & 0x7F;
            int month = (value >> 7) & 0x0F;
            int day = (value >> 11) & 0x1F;

            if (month < 1 || month > 12) month = 1;
            if (day < 1 || day > 31) day = 1;

            return string.Format("{0:D2}-{1:D2}-{2:D2}", year, month, day);
        }

        public static double ParseFromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("输入字符串不能为空");

            hex = hex.Trim();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);

            if (hex.Length % 2 != 0)
                throw new ArgumentException("十六进制字符串长度必须为偶数");
            if (hex.Length < 4)
                throw new ArgumentException("字符串长度至少为4才能提取后4个字符");

            string intPartHex = hex.Substring(hex.Length - 2, 2);
            string decPartHex = hex.Substring(hex.Length - 4, 2);

            int intPart = Convert.ToInt32(intPartHex, 16);
            int decPart = Convert.ToInt32(decPartHex, 16);

            if (decPart >= 10)
                return intPart + decPart / 100.0;
            return intPart + decPart / 10.0;
        }

        public static UInt64 ExtractBits(byte[] data, int startBit, int length, bool isSigned = false)
        {
            if (length <= 0 || length > 64) throw new ArgumentException("Invalid bit length");

            int byteOffset = startBit / 8;
            int bitOffset = startBit % 8;

            int bytesNeeded = (bitOffset + length + 7) / 8;
            if (byteOffset + bytesNeeded > data.Length)
                throw new ArgumentException("Data too short");

            UInt64 result = 0;
            for (int i = 0; i < bytesNeeded; i++)
            {
                result |= (UInt64)data[byteOffset + i] << (8 * i);
            }

            result >>= bitOffset;

            UInt64 mask = (length == 64) ? UInt64.MaxValue : ((1UL << length) - 1);
            result &= mask;

            if (isSigned && length > 1)
            {
                long signedValue = (long)result;
                if ((signedValue & (1L << (length - 1))) != 0)
                {
                    signedValue |= unchecked((long)~((1L << length) - 1));
                }
                return (UInt64)signedValue;
            }

            return result;
        }
    }
}
