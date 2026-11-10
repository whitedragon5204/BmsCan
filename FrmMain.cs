using Newtonsoft.Json;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Bms88122
{
    public partial class FrmMain : UIForm
    {
        BmsServices bmsServices;
        BmsDataModel bmsDataModel;
        bool boxOpened;
        bool usedZlg;
        volatile bool isRunning;

        IntPtr devHandle = IntPtr.Zero;
        IntPtr[] chnHandle = new IntPtr[10];
        int chn_max = 2;

        class ThreadParams
        {
            public IntPtr chnHandle { get; set; }
            public int chnNum { get; set; }
        }

        public FrmMain()
        {
            InitializeComponent();
            bmsServices = new BmsServices();
            bmsDataModel = new BmsDataModel();
            CheckForIllegalCrossThreadCalls = false;
            radioZLGUSB2EU.CheckedChanged += RadioZlg_CheckedChanged;
            radio2.CheckedChanged += RadioChuangXin_CheckedChanged;
        }

        void RadioZlg_CheckedChanged(object sender, EventArgs e)
        {
            if (radioZLGUSB2EU.Checked)
                radio2.Checked = false;
        }

        void RadioChuangXin_CheckedChanged(object sender, EventArgs e)
        {
            if (radio2.Checked)
                radioZLGUSB2EU.Checked = false;
        }

        private void intputPack_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            string pack = CheckPackLength();
            if (pack == null)
                return;
            bmsDataModel.PackCode = pack;
        }

        private void btnComm_Click(object sender, EventArgs e)
        {
            if (boxOpened)
                StopComm();
            else
                StartComm();
        }

        void StartComm()
        {
            if (boxOpened)
                return;
            if (radioZLGUSB2EU.Checked)
                OpenZlg();
            else if (radio2.Checked)
                OpenChuangXin();
            else
                MessageBox.Show("请先选周立功或创芯");
        }

        void StopComm()
        {
            if (!boxOpened)
                return;
            isRunning = false;
            Thread.Sleep(200);
            CloseBox();
            SetRadiosEnabled(true);
            usedZlg = false;
            btnComm.Text = "打开通讯";
        }

        void AfterOpened()
        {
            boxOpened = true;
            SetRadiosEnabled(false);
            btnComm.Text = "关闭通讯";
            if (string.IsNullOrEmpty(inputTotalV.Text))
                lblTips.Text = "请检查电池是否休眠";
        }

        void SetRadiosEnabled(bool enabled)
        {
            radioZLGUSB2EU.Enabled = enabled;
            radio2.Enabled = enabled;
        }

        void OpenZlg()
        {
            if (boxOpened)
                return;
            isRunning = true;
            usedZlg = true;
            devHandle = ZLGCAN.ZCAN_OpenDevice(ZLGCAN.ZCAN_USBCAN_2E_U, 0, 0);
            if (devHandle == IntPtr.Zero)
            {
                usedZlg = false;
                MessageBox.Show("没识别到周立功盒");
                return;
            }
            AfterOpened();
            for (int i = 0; i < chn_max; i++)
            {
                chnHandle[i] = Init_chn_USBCAN_E_U(devHandle, i);
                ThreadParams parameters = new ThreadParams
                {
                    chnHandle = chnHandle[i],
                    chnNum = i
                };
                Thread workerThread = new Thread(ReceiveZlgThread);
                workerThread.IsBackground = true;
                workerThread.Start(parameters);
            }
        }

        IntPtr Init_chn_USBCAN_E_U(IntPtr dev, int chn_idx)
        {
            IntPtr temp = IntPtr.Zero;
            string path = string.Format("{0}/baud_rate", chn_idx);
            uint ret = ZLGCAN.ZCAN_SetValue(dev, path, "500000");
            if (ret != 1)
                return IntPtr.Zero;

            ZLGCAN.ZCAN_CHANNEL_INIT_CONFIG InitConfig = new ZLGCAN.ZCAN_CHANNEL_INIT_CONFIG();
            InitConfig.can_type = 0;
            InitConfig.config.can.mode = 0;
            InitConfig.config.can.acc_code = 0x0;
            InitConfig.config.can.acc_mask = 0x1FFFFFFF;

            IntPtr P2InitConfig = Marshal.AllocHGlobal(Marshal.SizeOf(InitConfig));
            Marshal.StructureToPtr(InitConfig, P2InitConfig, true);
            temp = ZLGCAN.ZCAN_InitCAN(dev, (uint)chn_idx, P2InitConfig);
            Marshal.FreeHGlobal(P2InitConfig);
            if (temp == IntPtr.Zero)
                return IntPtr.Zero;

            uint startRet = ZLGCAN.ZCAN_StartCAN(temp);
            if (startRet != 1)
                return IntPtr.Zero;
            return temp;
        }

        void ReceiveZlgThread(object obj)
        {
            ThreadParams parameters = obj as ThreadParams;
            if (parameters == null || parameters.chnHandle == IntPtr.Zero)
                return;

            const int BUFFER_SIZE = 100;
            int canStructSize = Marshal.SizeOf(typeof(ZLGCAN.ZCAN_Receive_Data));
            IntPtr pCanBuffer = Marshal.AllocHGlobal(canStructSize * BUFFER_SIZE);
            try
            {
                while (isRunning)
                {
                    uint recNum = ZLGCAN.ZCAN_GetReceiveNum(parameters.chnHandle, 0);
                    if (recNum > 0)
                    {
                        uint actualReceive = ZLGCAN.ZCAN_Receive(parameters.chnHandle, pCanBuffer, BUFFER_SIZE, 10);
                        if (actualReceive > 0)
                        {
                            for (int i = 0; i < actualReceive; i++)
                            {
                                IntPtr pCurrentCan = (IntPtr)(pCanBuffer.ToInt64() + i * canStructSize);
                                ZLGCAN.ZCAN_Receive_Data recData = (ZLGCAN.ZCAN_Receive_Data)Marshal.PtrToStructure(
                                    pCurrentCan, typeof(ZLGCAN.ZCAN_Receive_Data));
                                uint location = recData.frame.can_id & 0x1FFFFFFF;
                                ZlgBmsParse.Apply(bmsDataModel, location, recData.frame.data);
                            }
                            ShowModel();
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(inputTotalV.Text))
                            lblTips.Text = "请检查电池是否休眠";
                        else
                            lblTips.Text = "";
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pCanBuffer);
            }
        }

        void OpenChuangXin()
        {
            if (boxOpened)
                return;
            isRunning = true;
            usedZlg = false;
            bool opened = false;
            try
            {
                opened = ChuangXinCan.Open250k();
            }
            catch
            {
                opened = false;
            }
            if (!opened)
            {
                MessageBox.Show("没识别到创芯盒");
                return;
            }
            AfterOpened();
            for (int i = 0; i < 2; i++)
            {
                Thread workerThread = new Thread(ReceiveChuangXinThread);
                workerThread.IsBackground = true;
                workerThread.Start(i);
            }
        }

        void ReceiveChuangXinThread(object obj)
        {
            int channel = (int)obj;
            int canStructSize = Marshal.SizeOf(typeof(ChuangXinCan.VCI_CAN_OBJ));
            IntPtr pCanBuffer = Marshal.AllocHGlobal(canStructSize * 100);
            try
            {
                while (isRunning)
                {
                    uint actualReceive = ChuangXinCan.Receive(channel, pCanBuffer, 100, 50);
                    if (actualReceive == 0 || actualReceive == 0xFFFFFFFF)
                    {
                        if (string.IsNullOrEmpty(inputTotalV.Text))
                            lblTips.Text = "请检查电池是否休眠";
                        else
                            lblTips.Text = "";
                        Thread.Sleep(20);
                        continue;
                    }
                    for (int i = 0; i < actualReceive; i++)
                    {
                        IntPtr pCurrent = (IntPtr)(pCanBuffer.ToInt64() + i * canStructSize);
                        ChuangXinCan.VCI_CAN_OBJ recData = (ChuangXinCan.VCI_CAN_OBJ)Marshal.PtrToStructure(pCurrent, typeof(ChuangXinCan.VCI_CAN_OBJ));
                        uint location = recData.ID & 0x1FFFFFFF;
                        byte[] data = recData.Data ?? new byte[0];
                        int dataLen = Math.Min(recData.DataLen, data.Length);
                        if (dataLen < data.Length)
                        {
                            byte[] cut = new byte[dataLen];
                            Array.Copy(data, cut, dataLen);
                            data = cut;
                        }
                        FengniaoParse.Apply(bmsDataModel, location, data);
                    }
                    ShowModel();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pCanBuffer);
            }
        }

        void ShowModel()
        {
            if (bmsDataModel.TotalVoltage > 0)
            {
                inputTotalV.Text = bmsDataModel.TotalVoltage.ToString();
                lblTips.Text = "";
            }
            inputSoc.Text = bmsDataModel.SOC ?? "";
            if (usedZlg)
                inputSOH.Text = bmsDataModel.SOH ?? "";
            else
                inputSOH.Text = "";
            inputWd1.Text = bmsDataModel.Temperature1.ToString();
            inputWd2.Text = bmsDataModel.Temperature2.ToString();
            inputWd3.Text = bmsDataModel.Temperature3.ToString();
            inputWd4.Text = bmsDataModel.Temperature4.ToString();
            inputWd5.Text = bmsDataModel.Temperature5.ToString();
            inputWd6.Text = bmsDataModel.Temperature6.ToString();
            inputWd7.Text = bmsDataModel.Temperature7.ToString();
            inputWd8.Text = bmsDataModel.Temperature8.ToString();
            inputHighV.Text = bmsDataModel.MaxCellVoltage.ToString();
            inputLowerV.Text = bmsDataModel.MinCellVoltage.ToString();
            inputDifferentV.Text = bmsDataModel.MaxVoltageDiff.ToString();
            inputRjVersion.Text = bmsDataModel.SoftwareVersion ?? "";
            inputYjVersion.Text = bmsDataModel.HardwareVersion ?? "";
            if (usedZlg)
            {
                if (bmsDataModel.RemainingCapacity > 0)
                    inputCapacity.Text = (bmsDataModel.RemainingCapacity / 1000m).ToString();
                if (ZlgBmsParse.LastMaxCellIndex > 0)
                    inputHighLocation.Text = ZlgBmsParse.LastMaxCellIndex.ToString();
                if (ZlgBmsParse.LastMinCellIndex > 0)
                    inputLowerLocation.Text = ZlgBmsParse.LastMinCellIndex.ToString();
                if (bmsDataModel.Mos != 0)
                    inputMos.Text = bmsDataModel.Mos.ToString();
            }
            inputBattery1.Text = CellText(1);
            inputBattery2.Text = CellText(2);
            inputBattery3.Text = CellText(3);
            inputBattery4.Text = CellText(4);
            inputBattery5.Text = CellText(5);
            inputBattery6.Text = CellText(6);
            inputBattery7.Text = CellText(7);
            inputBattery8.Text = CellText(8);
            inputBattery9.Text = CellText(9);
            inputBattery10.Text = CellText(10);
            inputBattery11.Text = CellText(11);
            inputBattery12.Text = CellText(12);
            inputBattery13.Text = CellText(13);
            inputBattery14.Text = CellText(14);
            inputBattery15.Text = CellText(15);
            inputBattery16.Text = CellText(16);
            inputBattery17.Text = CellText(17);
            inputBattery18.Text = CellText(18);
            inputBattery19.Text = CellText(19);
            inputBattery20.Text = CellText(20);
            inputBattery21.Text = CellText(21);
            inputBattery22.Text = CellText(22);
            inputBattery23.Text = CellText(23);
            inputBattery24.Text = CellText(24);
        }

        string CellText(int n)
        {
            decimal v = FengniaoParse.GetCell(bmsDataModel, n);
            return v > 0 ? v.ToString() : "";
        }

        string CheckPackLength()
        {
            string pack = (intputPack.Text ?? "").Trim();
            intputPack.Text = pack;
            if (string.IsNullOrWhiteSpace(pack))
            {
                MessageBox.Show("PACK码不能为空");
                return null;
            }
            if (pack.Length != 24)
            {
                MessageBox.Show("PACK码长度不是24位，请重新扫码");
                return null;
            }
            return pack;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string pack = CheckPackLength();
            if (pack == null)
                return;

            bmsDataModel.PackCode = pack;
            if (bmsServices.PackCodeExists(pack))
            {
                string product = "E14";
                if (pack.IndexOf("E23", StringComparison.OrdinalIgnoreCase) >= 0)
                    product = "E23";
                else if (pack.IndexOf("E14", StringComparison.OrdinalIgnoreCase) >= 0)
                    product = "E14";
                else
                    product = usedZlg ? "E14" : "E23";
                DialogResult ask = MessageBox.Show("当前PACK码已存在" + product + "通讯测试数据，是否需要再测一次", "", MessageBoxButtons.YesNo);
                if (ask != DialogResult.Yes)
                    return;
            }

            if (string.IsNullOrEmpty(inputTotalV.Text))
            {
                MessageBox.Show("总电压为空，请先打开通讯");
                return;
            }

            if (usedZlg)
                SaveZlgLikeE14();
            else
                SaveChuangXin();
        }

        void SaveZlgLikeE14()
        {
            isRunning = false;
            Thread.Sleep(1500);
            CloseBox();
            SetRadiosEnabled(true);
            btnComm.Text = "打开通讯";

            bool brj = false;
            string resultRj = GetVersionCode("HARD_VERSION");
            RootResponse rootResponseRJ = JsonConvert.DeserializeObject<RootResponse>(resultRj);
            bool bYj = false;
            string resultYj = GetVersionCode("SOFT_VERSION");
            RootResponse rootResponseYJ = JsonConvert.DeserializeObject<RootResponse>(resultYj);

            if (rootResponseRJ != null && rootResponseRJ.Data != null && bmsDataModel.HardwareVersion != null)
            {
                foreach (var item in rootResponseRJ.Data)
                {
                    if (bmsDataModel.HardwareVersion.Contains(item.Value))
                    {
                        brj = true;
                        break;
                    }
                }
            }
            if (rootResponseYJ != null && rootResponseYJ.Data != null && bmsDataModel.SoftwareVersion != null)
            {
                foreach (var item in rootResponseYJ.Data)
                {
                    if (bmsDataModel.SoftwareVersion.Contains(item.Value))
                    {
                        bYj = true;
                        break;
                    }
                }
            }
            if (!brj)
            {
                MessageBox.Show("当前软件版本为:" + bmsDataModel.SoftwareVersion + ",与后台维护的不一致，请联系质量/研发维护。");
                Reflush();
                return;
            }
            if (!bYj)
            {
                MessageBox.Show("当前硬件版本为:" + bmsDataModel.SoftwareVersion + ",与后台维护的不一致，请联系质量/研发维护。");
                Reflush();
                return;
            }

            List<PQCSettingModel> pQCSettingModels = bmsServices.GetPQCSettingModels();
            if (pQCSettingModels.Where(o => o.Code == "bmsvdiff").First().Flag == 1)
            {
                PQCSettingModel pQCSettingModelBmsvdiff = pQCSettingModels.Where(o => o.Code == "bmsvdiff").First();
                if (!(bmsDataModel.MaxVoltageDiff > pQCSettingModelBmsvdiff.LowerLimit && bmsDataModel.MaxVoltageDiff <= pQCSettingModelBmsvdiff.UpperLimit))
                    MessageBox.Show(bmsDataModel.PackCode + "的CAN通讯压差为【" + bmsDataModel.MaxVoltageDiff + "】,不在【" + pQCSettingModelBmsvdiff.LowerLimit + "-" + pQCSettingModelBmsvdiff.UpperLimit + "】范围内");
            }
            if (pQCSettingModels.Where(o => o.Code == "bmsv").First().Flag == 1)
            {
                PQCSettingModel pQCSettingModelBmsv = pQCSettingModels.Where(o => o.Code == "bmsv").First();
                if (!(bmsDataModel.TotalVoltage > pQCSettingModelBmsv.LowerLimit && bmsDataModel.TotalVoltage <= pQCSettingModelBmsv.UpperLimit))
                    MessageBox.Show(bmsDataModel.PackCode + "的CAN通讯总电压为【" + bmsDataModel.TotalVoltage + "】,不在【" + pQCSettingModelBmsv.LowerLimit + "-" + pQCSettingModelBmsv.UpperLimit + "】范围内");
            }
            if (pQCSettingModels.Where(o => o.Code == "bmstemperature").First().Flag == 1)
            {
                PQCSettingModel pT = pQCSettingModels.Where(o => o.Code == "bmstemperature").First();
                decimal tmpdiff = (Math.Max(Math.Max(bmsDataModel.Temperature1, bmsDataModel.Temperature2), Math.Max(bmsDataModel.Temperature3, bmsDataModel.Temperature4))
                    - Math.Min(Math.Min(bmsDataModel.Temperature1, bmsDataModel.Temperature2), Math.Min(bmsDataModel.Temperature3, bmsDataModel.Temperature4)));
                if (!(tmpdiff > pT.LowerLimit && tmpdiff <= pT.UpperLimit))
                    MessageBox.Show(bmsDataModel.PackCode + "的CAN通讯温差为【" + tmpdiff + "】,不在【" + pT.LowerLimit + "-" + pT.UpperLimit + "】范围内");
            }

            bmsServices.Insert(bmsDataModel);
            MessageBox.Show("已测试完成并上传MES");
            Reflush();
        }

        void SaveChuangXin()
        {
            bool brj = true;
            bool bYj = false;
            string resultYj = GetVersionCode("SOFT_VERSION");
            RootResponse rootResponseYJ = JsonConvert.DeserializeObject<RootResponse>(resultYj);
            if (rootResponseYJ != null && rootResponseYJ.Data != null)
            {
                foreach (var item in rootResponseYJ.Data)
                {
                    if (bmsDataModel.SoftwareVersion != null && bmsDataModel.SoftwareVersion.Contains(item.Value))
                    {
                        bYj = true;
                        break;
                    }
                }
            }
            if (!brj)
            {
                MessageBox.Show("当前软件版本为:" + bmsDataModel.SoftwareVersion + ",与后台维护的不一致，请联系质量/研发维护。");
                Reflush();
                return;
            }
            if (!bYj)
            {
                MessageBox.Show("当前硬件版本为:" + bmsDataModel.SoftwareVersion + ",与后台维护的不一致，请联系质量/研发维护。");
                Reflush();
                return;
            }

            var mesThresholds = MesApiService.GetBmsTxThresholds();
            if (mesThresholds == null)
            {
                MessageBox.Show("没拿到MES参数，请联系质量/研发维护。");
                return;
            }

            if (mesThresholds.TryGetValue("bmsvdiff", out MesSettingDto pVdiff))
            {
                decimal v = bmsDataModel.MaxVoltageDiff;
                if (!(v > pVdiff.LowerLimit && v <= pVdiff.UpperLimit))
                {
                    MessageBox.Show(bmsDataModel.PackCode + "的CAN通讯压差为【" + v + "】,不在【" + pVdiff.LowerLimit + "-" + pVdiff.UpperLimit + "】范围内");
                    return;
                }
            }
            if (mesThresholds.TryGetValue("bmsv", out MesSettingDto pV))
            {
                decimal v = bmsDataModel.TotalVoltage;
                if (!(v > pV.LowerLimit && v <= pV.UpperLimit))
                {
                    MessageBox.Show(bmsDataModel.PackCode + "的CAN通讯总电压为【" + v + "】,不在【" + pV.LowerLimit + "-" + pV.UpperLimit + "】范围内");
                    return;
                }
            }
            if (mesThresholds.TryGetValue("bmstemperature", out MesSettingDto pT))
            {
                decimal tmpdiff = (Math.Max(Math.Max(bmsDataModel.Temperature1, bmsDataModel.Temperature2), Math.Max(bmsDataModel.Temperature3, bmsDataModel.Temperature4))
                    - Math.Min(Math.Min(bmsDataModel.Temperature1, bmsDataModel.Temperature2), Math.Min(bmsDataModel.Temperature3, bmsDataModel.Temperature4)));
                if (!(tmpdiff > pT.LowerLimit && tmpdiff <= pT.UpperLimit))
                {
                    MessageBox.Show(bmsDataModel.PackCode + "的CAN通讯温差为【" + tmpdiff + "】,不在【" + pT.LowerLimit + "-" + pT.UpperLimit + "】范围内");
                    return;
                }
            }
            if (mesThresholds.TryGetValue("bmsspn", out MesSettingDto pSpn))
            {
                int cellCount = CountNonZeroCells();
                if (!(cellCount >= (int)pSpn.LowerLimit && cellCount <= (int)pSpn.UpperLimit))
                {
                    MessageBox.Show(bmsDataModel.PackCode + "的CAN通讯串并数为【" + cellCount + "】,不在【" + (int)pSpn.LowerLimit + "-" + (int)pSpn.UpperLimit + "】范围内");
                    return;
                }
            }

            bmsServices.Insert(bmsDataModel);
            MessageBox.Show("已测试完成并上传MES");
            Reflush();
        }

        int CountNonZeroCells()
        {
            int n = 0;
            for (int i = 1; i <= 24; i++)
            {
                if (FengniaoParse.GetCell(bmsDataModel, i) > 0)
                    n++;
            }
            if (bmsDataModel.Cell25Voltage > 0) n++;
            if (bmsDataModel.Cell26Voltage > 0) n++;
            if (bmsDataModel.Cell27Voltage > 0) n++;
            if (bmsDataModel.Cell28Voltage > 0) n++;
            if (bmsDataModel.Cell29Voltage > 0) n++;
            if (bmsDataModel.Cell30Voltage > 0) n++;
            if (bmsDataModel.Cell31Voltage > 0) n++;
            if (bmsDataModel.Cell32Voltage > 0) n++;
            return n;
        }

        void CloseBox()
        {
            if (!boxOpened)
                return;
            try
            {
                if (usedZlg)
                {
                    for (int i = 0; i < chn_max; i++)
                    {
                        if (chnHandle[i] != IntPtr.Zero)
                            ZLGCAN.ZCAN_ResetCAN(chnHandle[i]);
                        chnHandle[i] = IntPtr.Zero;
                    }
                    if (devHandle != IntPtr.Zero)
                        ZLGCAN.ZCAN_CloseDevice(devHandle);
                    devHandle = IntPtr.Zero;
                }
                else
                {
                    ChuangXinCan.Close();
                }
            }
            catch
            {
            }
            boxOpened = false;
        }

        private void Reflush()
        {
            intputPack.Text = "";
            inputTotalV.Text = "";
            inputSoc.Text = "";
            inputSOH.Text = "";
            inputCapacity.Text = "";
            inputHighV.Text = "";
            inputHighLocation.Text = "";
            inputLowerLocation.Text = "";
            inputLowerV.Text = "";
            inputYjVersion.Text = "";
            inputRjVersion.Text = "";
            inputMos.Text = "";
            inputWd1.Text = "";
            inputWd2.Text = "";
            inputWd3.Text = "";
            inputWd4.Text = "";
            inputWd5.Text = "";
            inputWd6.Text = "";
            inputWd7.Text = "";
            inputWd8.Text = "";
            inputBattery1.Text = "";
            inputBattery2.Text = "";
            inputBattery3.Text = "";
            inputBattery4.Text = "";
            inputBattery5.Text = "";
            inputBattery6.Text = "";
            inputBattery7.Text = "";
            inputBattery8.Text = "";
            inputBattery9.Text = "";
            inputBattery10.Text = "";
            inputBattery11.Text = "";
            inputBattery12.Text = "";
            inputBattery13.Text = "";
            inputBattery14.Text = "";
            inputBattery15.Text = "";
            inputBattery16.Text = "";
            inputBattery17.Text = "";
            inputBattery18.Text = "";
            inputBattery19.Text = "";
            inputBattery20.Text = "";
            inputBattery21.Text = "";
            inputBattery22.Text = "";
            inputBattery23.Text = "";
            inputBattery24.Text = "";
            inputBattery25.Text = "";
            inputBattery26.Text = "";
            inputBattery27.Text = "";
            inputBattery28.Text = "";
            inputBattery29.Text = "";
            inputBattery30.Text = "";
            inputBattery31.Text = "";
            inputBattery32.Text = "";
            inputDifferentV.Text = "";
            bool keepZlg = usedZlg;
            bmsDataModel = new BmsDataModel();
            usedZlg = keepZlg;
            ZlgBmsParse.LastMaxCellIndex = 0;
            ZlgBmsParse.LastMinCellIndex = 0;
            if (boxOpened)
                lblTips.Text = "请检查电池是否休眠";
            intputPack.Focus();
        }

        private void panel3_Click(object sender, EventArgs e)
        {
        }

        public string GetVersionCode(string type)
        {
            string url = "http://newmes.hdc-group.cn/core/bmZeroBaseDataInfo/combo?code=" + type;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 30000;
            request.ContentType = "application/json";
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine("请求失败：" + ex.Message);
                return "";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isRunning = false;
            CloseBox();
            base.OnFormClosing(e);
        }
    }
}
