using System.Runtime.InteropServices;
using System.Text;
using USB_Handle = System.IntPtr;


namespace Ad5933DotNet
{
    public static class SimpleUsb
    {
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern int USB_Init();
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Exit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void USB_Exit();
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Get_Device_List", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern int USB_Get_Device_List();
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Scan_Device", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void USB_Scan_Device();

        /// <summary>
        /// 初始化USB模块
        /// </summary>
        /// <returns>如果成功，返回0</returns>
        static public int Init()
        {
            return USB_Init();
        }

        /// <summary>
        /// 退出USB模块
        /// </summary>
        static public void Exit()
        {
            USB_Exit();
        }

        /// <summary>
        /// 显示USB设备列表
        /// </summary>
        /// <returns>如果成功，返回0</returns>
        public static int GetDeviceList()
        {
            return USB_Get_Device_List();
        }

        /// <summary>
        /// 扫描并显示USB设备
        /// </summary>
        public static void ScanDevice()
        {
            USB_Scan_Device();
        }
    }

    public class UsbDevice
    {
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern USB_Handle USB_Open(ushort VID, ushort PID);
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Write", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern int USB_Write(USB_Handle usbHandle, byte[] buff, int len, uint ms);
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Read", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern int USB_Read(USB_Handle usbHandle, byte[] buff, int len, uint ms);
        [DllImport("SimpleUSB.dll", EntryPoint = "USB_Close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void USB_Close(USB_Handle usbHandle);

        private USB_Handle handle = (IntPtr)0;

        /// <summary>
        /// 打开指定USB设备
        /// </summary>
        /// <param name="VID">指定设备的vid</param>
        /// <param name="PID">指定设备的pid</param>
        /// <returns>如果成功，返回true, 否则返回false</returns>
        public bool Open(ushort VID, ushort PID)
        {
            handle = USB_Open(VID, PID);
            if (handle != (IntPtr)0) return true;
            else return false;
        }

        /// <summary>
        /// 向指定USB设备写入数据
        /// </summary>
        /// <param name="buff">需要写入的数据</param>
        /// <param name="len">写入数据大小(Byte)</param>
        /// <param name="ms">最长等待时间(ms)</param>
        /// <returns>如果成功，返回实际发送长度，否则返回值小于0</returns> 
        public int Write(byte[] buff, int len, uint ms)
        {
            if (handle == (IntPtr)0) return -1;
            return USB_Write(handle, buff, len, ms);
        }

        /// <summary>
        /// 向指定USB设备读取数据
        /// </summary>
        /// <param name="buff">需要读取的数据</param>
        /// <param name="len">读取数据大小(Byte)</param>
        /// <param name="ms">最长等待时间(ms)</param>
        /// <returns>如果成功，返回实际读取长度，否则返回值小于等于0</returns> 
        public int Read(byte[] buff, int len, uint ms)
        {
            if (handle == (IntPtr)0) return -1;
            return USB_Read(handle, buff, len, ms);
        }

        /// <summary>
        /// 关闭指定的USB设备
        /// </summary>
        public void Close()
        {
            USB_Close(handle);
            handle = (IntPtr)0;
        }

        ~UsbDevice()
        {
            if (handle != (IntPtr)0)
            {
                USB_Close(handle);
            }
        }
    }

    public struct Ad5933Info
    {
        //频率
        public uint Freq;
        //实部
        public short Real;
        //虚部
        public short Image;
        //温度
        public double Temp;
        //输出电压
        public byte OutVol;
        //增益系数
        public byte Gain;

        public Ad5933Info()
        {
            Freq = 0;
            Real = 0;
            Image = 0;
            Temp = 0;
            OutVol = 0;
            Gain = 0;
        }
    }

    public class AD5933
    {
        public const int OUTPUT_2V = 1;
        public const int OUTPUT_1V = 2;
        public const int OUTPUT_400mV = 3;
        public const int OUTPUT_200mV = 4;
        public const int GAIN_1 = 1;
        public const int GAIN_5 = 2;

        private UsbDevice usb = new UsbDevice();
        private byte[] txBuff = new byte[20];
        private byte[] rxBuff = new byte[20];

        /// <summary>
        /// 读取的RACWH_AD5933信息
        /// </summary>
        public Ad5933Info Info = new Ad5933Info();

        /// <summary>
        /// 打开RACWH_AD5933的USB连接
        /// </summary>
        /// <param name="vid">对应的vid</param>
        /// <param name="pid">对应的pid</param>
        /// <returns>true 成功 false 失败</returns>
        public bool Open(ushort vid, ushort pid)
        {
            return (usb.Open(vid, pid));
        }

        /// <summary>
        /// 向RACWH_AD5933写入控制块
        /// </summary>
        /// <param name="freq">频率</param>
        /// <param name="outVol">输出电压</param>
        /// <param name="gain">增益系数</param>
        /// <param name="ms">最长等待时间(ms)</param>
        /// <returns>true 成功 false 失败</returns>
        public bool Write(uint freq, byte outVol, byte gain, uint ms)
        {
            txBuff[0] = 68; //'D' in ascii
            txBuff[1] = (byte)((freq >> 24) & 0xFF);
            txBuff[2] = (byte)((freq >> 16) & 0xFF);
            txBuff[3] = (byte)((freq >> 8) & 0xFF);
            txBuff[4] = (byte)((freq >> 0) & 0xFF);
            txBuff[5] = outVol;
            txBuff[6] = gain;
            txBuff[7] = 65; //'A' in ascii
            int err = usb.Write(txBuff, 20, ms);
            if (err < 0) return false;
            else return true;
        }

        /// <summary>
        /// 向RACWH_AD5933获取读取块，并储存到 Info 里
        /// </summary>
        /// <param name="ms">最长单次等待时间(ms)</param>
        /// <param name="maxWait">最大等待次数(不小于2)</param>
        /// <returns>true 成功 false 失败</returns>
        public bool Read(uint ms, int maxWait = 5)
        {
            int err = usb.Read(rxBuff, 20, ms);
            int wait = 0;
            if (err < 0)
            {
                Console.WriteLine("Read Error: " + err);
                return false;
            }
            if (err == 0)
            {
                wait++;
                while (usb.Read(rxBuff, 20, ms) <= 0)
                {
                    wait++;
                    if (wait >= maxWait)
                    {
                        Console.WriteLine("Read Error: Time Out");
                        return false;
                    }
                }
            }
            if (rxBuff[0] != 65 || rxBuff[17] != 68)
            {
                Console.WriteLine("Read Error: Failed to Verify Data");
                return false;
            }
            Info.Freq = (uint)((rxBuff[4] & 0xFF) | ((rxBuff[3] & 0xFF) << 8) | ((rxBuff[2] & 0xFF) << 16) | ((rxBuff[1] & 0xFF) << 24));
            Info.Real = (short)((rxBuff[8] & 0xFF) | ((rxBuff[7] & 0xFF) << 8));
            Info.Image = (short)((rxBuff[12] & 0xFF) | ((rxBuff[11] & 0xFF) << 8));
            ushort temp = (ushort)((rxBuff[14] & 0xFF) | ((rxBuff[13] & 0xFF) << 8));
            temp = (ushort)(temp & 0x3FFF);
            if ((temp & 0x2000) == 1) Info.Temp = (temp - 16384) / 32.0;
            else Info.Temp = temp / 32.0;
            Info.OutVol = rxBuff[15];
            Info.Gain = rxBuff[16];
            return true;
        }

        /// <summary>
        /// 关闭RACWH_AD5933的USB连接
        /// </summary>
        public void Close()
        {
            usb.Close();
        }
    }
}
