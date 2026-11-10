using System;
using System.Runtime.InteropServices;

namespace Bms88122
{
    /// <summary>
    /// 创芯 USB-CAN-2C。只开盒、250 kbps、收标准帧。
    /// </summary>
    public static class ChuangXinCan
    {
        public const uint DevType = 4;
        const uint DevIndex = 0;
        const int ChannelCount = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct VCI_INIT_CONFIG
        {
            public uint AccCode;
            public uint AccMask;
            public uint Reserved;
            public byte Filter;
            public byte Timing0;
            public byte Timing1;
            public byte Mode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VCI_CAN_OBJ
        {
            public uint ID;
            public uint TimeStamp;
            public byte TimeFlag;
            public byte SendType;
            public byte RemoteFlag;
            public byte ExternFlag;
            public byte DataLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;
        }

        [DllImport("ControlCAN.dll", CallingConvention = CallingConvention.StdCall)]
        static extern uint VCI_OpenDevice(uint DeviceType, uint DeviceInd, uint Reserved);

        [DllImport("ControlCAN.dll", CallingConvention = CallingConvention.StdCall)]
        static extern uint VCI_CloseDevice(uint DeviceType, uint DeviceInd);

        [DllImport("ControlCAN.dll", CallingConvention = CallingConvention.StdCall)]
        static extern uint VCI_InitCAN(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_INIT_CONFIG pInitConfig);

        [DllImport("ControlCAN.dll", CallingConvention = CallingConvention.StdCall)]
        static extern uint VCI_StartCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("ControlCAN.dll", CallingConvention = CallingConvention.StdCall)]
        static extern uint VCI_ResetCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("ControlCAN.dll", CallingConvention = CallingConvention.StdCall)]
        static extern uint VCI_Receive(uint DeviceType, uint DeviceInd, uint CANInd, IntPtr pReceive, uint Len, int WaitTime);

        public static bool Open250k()
        {
            if (VCI_OpenDevice(DevType, DevIndex, 0) != 1)
                return false;

            int started = 0;
            for (uint i = 0; i < ChannelCount; i++)
            {
                VCI_INIT_CONFIG config = new VCI_INIT_CONFIG();
                config.AccCode = 0;
                config.AccMask = 0xFFFFFFFF;
                config.Filter = 1;
                config.Timing0 = 0x01;
                config.Timing1 = 0x1C;
                config.Mode = 0;
                if (VCI_InitCAN(DevType, DevIndex, i, ref config) != 1)
                    continue;
                if (VCI_StartCAN(DevType, DevIndex, i) == 1)
                    started++;
            }

            if (started == 0)
            {
                VCI_CloseDevice(DevType, DevIndex);
                return false;
            }
            return true;
        }

        public static uint Receive(int channel, IntPtr buffer, uint count, int waitMs)
        {
            return VCI_Receive(DevType, DevIndex, (uint)channel, buffer, count, waitMs);
        }

        public static void Close()
        {
            for (uint i = 0; i < ChannelCount; i++)
                VCI_ResetCAN(DevType, DevIndex, i);
            VCI_CloseDevice(DevType, DevIndex);
        }
    }
}
