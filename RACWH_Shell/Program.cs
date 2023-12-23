using System;
using System.Runtime.ExceptionServices;
using System.Text;
using Ad5933DotNet;

static class Models
{
    static public class Pre_5K
    {
        static double deltaRs = 0;
        static public string pre_5k(uint Freq, double real, double image, bool calOn, double calReal, double calImage)
        {
            double newReal = real > 0 ? real : -real;
            double rs = 0.00013523*newReal*newReal*newReal - 0.44649893*newReal*newReal + 334.18242327*newReal + 58128.74002691;
            if (calOn)
            {
                deltaRs = rs - calReal;
                rs = rs - deltaRs;
            }
            return rs.ToString();
        }
    }

    static  public class Pre_std
    {
        static double Gain_co = 1.0;
        static double Phi_sys = 0.0;
        static double k_gain = 0.0;
        static double k_phi = 0.0;
        static double Freq_cal = 0.0;

        static private double get_phi(double x, double y)
        {
            return Math.Atan2(y, x) * (180 / MathF.PI);
        }

        static public string pre_std(uint Freq, double real, double image, bool calOn, double calReal, double calImage)
        {
            double Z = Math.Sqrt(real * real + image * image); 
            string rs = "";
            if (calOn)
            {
                double Z_cal = Math.Sqrt(calReal * calReal + calImage * calImage);
                if (Freq_cal == 0.0 || Freq_cal == Freq)
                {
                    Gain_co = 1 / (Z * Z_cal);
                    Phi_sys = get_phi(real, image) - get_phi(calReal, calImage);
                    Freq_cal = Freq;
                }
                else 
                {
                    double Gain_co2 = 1 / (Z * Z_cal);
                    double Phi_sys2 = get_phi(real, image) - get_phi(calReal, calImage);
                    k_gain = (Gain_co2 - Gain_co) / (Freq - Freq_cal);
                    k_phi = (Phi_sys2 - Phi_sys) / (Freq - Freq_cal);
                }
            }
            double rs_Z = 1 / ((Gain_co + k_gain * (Freq - Freq_cal)) * Z);
            double rs_Phi = get_phi(real, image) - (Phi_sys + k_phi * (Freq - Freq_cal));
            rs += rs_Z.ToString();
            rs += ",";
            rs += rs_Phi.ToString();
            return rs;
        }
    }
}

static class Program
{
    const ushort VID = 0x0483;
    const ushort PID = 0x5740;

    static bool showFreq = true;
    static bool showReal = true;
    static bool showImage = true;
    static bool showZ = true;
    static bool showGain = false;
    static bool showVol = false;
    static bool showTemp = false;
    static int avgNum = 1;
    static int repNum = 1;
    static uint setFreq = 5000;
    static byte setVol = AD5933.OUTPUT_2V;
    static byte setGain = AD5933.GAIN_1;
    static bool exit = false;
    static bool disConnect = false;
    static bool reConnect = false;
    static int waitTime = 0;
    static bool sweepOn = false;
    static uint sweepStart = 0;
    static uint sweepEnd = 0;
    static uint sweepStep = 0;
    static bool saveStart = false;
    static bool saveEnd = false;
    static bool saveOn = false;
    static string? saveFile;
    static Dictionary<string, bool> modelsOn = new Dictionary<string, bool>();
    static Dictionary<string, string> modelsValue = new Dictionary<string, string>();
    static bool calOn = false;
    static double calReal = 0.0;
    static double calImage = 0.0;

    static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
    }

    static void AddModels()
    {
        //add your model's name here
        modelsOn.Add("pre-5k", false);
        modelsValue.Add("pre-5k", "");
        modelsOn.Add("pre-std", false);
        modelsValue.Add("pre-std", "");
    }

    static void ProcessModels(double real, double image)
    {
        foreach (KeyValuePair<string, bool> kvp in modelsOn)
        {
            if (kvp.Value)
            {
                //add your model's function here
                if (kvp.Key == "pre-5k") modelsValue[kvp.Key] = Models.Pre_5K.pre_5k(setFreq, real, image, calOn, calReal, calImage);
                if (kvp.Key == "pre-std") modelsValue[kvp.Key] = Models.Pre_std.pre_std(setFreq, real, image, calOn, calReal, calImage);
            }
            else modelsValue[kvp.Key] = "";
        }
        if (calOn)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Callibration Success!");
            calOn = false;
        }
        return;
    }

    static bool GetCommand()
    {
        string? str = Console.ReadLine();
        if (str == null) return true;
        if (str.Length == 0) return true;
        string[] args = str.Split(' ');
        for (int i=0; i<args.Length; i++)
        {
            if (args[i] == "-show")
            {
                while (true)
                {
                    i++;
                    if (i >= args.Length) break;
                    if (args[i][0] == '-') break;
                    if (args[i] == "freq") showFreq = true;
                    else if (args[i] == "real") showReal = true;
                    else if (args[i] == "image") showImage = true;
                    else if (args[i] == "z") showZ = true;
                    else if (args[i] == "gain") showGain = true;
                    else if (args[i] == "vol") showVol = true;
                    else if (args[i] == "temp") showTemp = true;
                    else if (args[i] == "all")
                    {
                        showFreq = true;
                        showReal = true;
                        showImage = true;
                        showZ = true;
                        showGain = true;
                        showVol = true;
                        showTemp = true;
                    }
                    else
                    {
                        PrintError("Unknown argument for -show: " + args[i]);
                        return false;
                    }
                }
                i--;
            }
            else if (args[i] == "-hide")
            {
                while (true)
                {
                    i++;
                    if (i >= args.Length) break;
                    if (args[i][0] == '-') break;
                    if (args[i] == "freq") showFreq = false;
                    else if (args[i] == "real") showReal = false;
                    else if (args[i] == "image") showImage = false;
                    else if (args[i] == "z") showZ = false;
                    else if (args[i] == "gain") showGain = false;
                    else if (args[i] == "vol") showVol = false;
                    else if (args[i] == "temp") showTemp = false;
                    else if (args[i] == "all")
                    {
                        showFreq = false;
                        showReal = false;
                        showImage = false;
                        showZ = false;
                        showGain = false;
                        showVol = false;
                        showTemp = false;
                    }
                    else
                    {
                        PrintError("Unknown argument for -hide: " + args[i]);
                        return false;
                    }
                }
                i--;
            }
            else if (args[i] == "-avg")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -avg");
                    return false;
                }
                int temp = Convert.ToInt32(args[i]);
                if (temp != 0) avgNum = temp;
                else
                {
                    PrintError("Invalid value for -avg: "+temp.ToString());
                    return false;
                }
            }
            else if (args[i] == "-rep")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -rep");
                    return false;
                }
                int temp = Convert.ToInt32(args[i]);
                if (temp > 0) repNum = temp;
                else
                {
                    PrintError("Invalid value for -rep: " + temp.ToString());
                    return false;
                }
            }
            else if (args[i] == "-fre")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -fre");
                    return false;
                }
                uint temp = Convert.ToUInt32(args[i]);
                if (temp > 0) setFreq = temp;
                else
                {
                    PrintError("Invalid value for -fre: " + temp.ToString());
                    return false;
                }
            }
            else if (args[i] == "-vol")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -vol");
                    return false;
                }
                if (args[i] == "2v") setVol = AD5933.OUTPUT_2V;
                else if (args[i] == "1v") setVol = AD5933.OUTPUT_1V;
                else if (args[i] == "400mv") setVol = AD5933.OUTPUT_400mV;
                else if (args[i] == "200mv") setVol = AD5933.OUTPUT_200mV;
                else
                {
                    PrintError("Unknown argument for -vol: " + args[i]);
                    return false;
                }
            }
            else if (args[i] == "-gain")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -gain");
                    return false;
                }
                if (args[i] == "x1") setGain = AD5933.GAIN_1;
                else if (args[i] == "x5") setGain = AD5933.GAIN_5;
                else
                {
                    PrintError("Unknown argument for -gain: " + args[i]);
                    return false;
                }
            }
            else if (args[i] == "-wait")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -wait");
                    return false;
                }
                int temp = Convert.ToInt32(args[i]);
                if (temp >= 0) waitTime = temp;
                else
                {
                    PrintError("Invalid value for -wait: " + temp.ToString());
                    return false;
                }
            }
            else if (args[i] == "-sweep")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -sweep");
                    return false;
                }
                if (args[i] == "-on")
                {
                    sweepOn = true;
                    i++;
                    if (i >= args.Length)
                    {
                        PrintError("missing argument for -sweep");
                        return false;
                    }
                    uint temp = Convert.ToUInt32(args[i]);
                    if (temp > 0) sweepStart = temp;
                    else
                    {
                        PrintError("Invalid value for -sweep [startFreq]: " + temp.ToString());
                        return false;
                    }
                    i++;
                    if (i >= args.Length)
                    {
                        PrintError("missing argument for -sweep");
                        return false;
                    }
                    temp = Convert.ToUInt32(args[i]);
                    if (temp > 0) sweepEnd = temp;
                    else
                    {
                        PrintError("Invalid value for -sweep [endFreq]: " + temp.ToString());
                        return false;
                    }
                    i++;
                    if (i >= args.Length)
                    {
                        PrintError("missing argument for -sweep");
                        return false;
                    }
                    temp = Convert.ToUInt32(args[i]);
                    if (temp > 0) sweepStep = temp;
                    else
                    {
                        PrintError("Invalid value for -sweep [stepFreq]: " + temp.ToString());
                        return false;
                    }
                    setFreq = sweepStart;
                }
                else if (args[i] == "-off")
                {
                    sweepOn = false;
                }
                else
                {
                    PrintError("Unknown argument for -sweep");
                    return false;
                }
            }
            else if (args[i] == "-save")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -save");
                    return false;
                }
                if (args[i] == "-off")
                {
                    saveFile = null;
                    saveEnd = true;
                }
                else
                {
                    saveFile = args[i];
                    saveStart = true;
                }
            }
            else if (args[i] == "-model")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -model");
                    return false;
                }
                if (args[i] == "-on")
                {
                    while (true)
                    {
                        i++;
                        if (i >= args.Length) break;
                        if (args[i][0] == '-') break;
                        if (args[i] == "all")
                        {
                            foreach (KeyValuePair<string, bool> kvp in modelsOn)
                            {
                                modelsOn[kvp.Key] = true;
                            }
                            break;
                        }
                        if (modelsOn.ContainsKey(args[i])) modelsOn[args[i]] = true;
                        else
                        {
                            PrintError("Unknown model named: " + args[i]);
                            break;
                        }
                    }
                    i--;
                }
                else if (args[i] == "-off")
                {
                    while (true)
                    {
                        i++;
                        if (i >= args.Length) break;
                        if (args[i][0] == '-') break;
                        if (args[i] == "all")
                        {
                            foreach (KeyValuePair<string, bool> kvp in modelsOn)
                            {
                                modelsOn[kvp.Key] = false;
                            }
                            break;
                        }
                        if (modelsOn.ContainsKey(args[i])) modelsOn[args[i]] = false;
                        else
                        {
                            PrintError("Unknown model named: " + args[i]);
                            break;
                        }
                    }
                    i--;
                }
                else if (args[i] == "-list")
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("=====[Models]=====");
                    foreach (KeyValuePair<string, bool> kvp in modelsOn)
                    {
                        Console.WriteLine(kvp.Key + "   " + "[" + (kvp.Value ? "on" : "off") + "]");
                    }
                    Console.WriteLine("==================");
                }
                else if (args[i] == "-cal")
                {
                    i++;
                    if (i >= args.Length)
                    {
                        PrintError("missing argument for -model -cal");
                        return false;
                    }
                    double temp = Convert.ToDouble(args[i]);
                    calReal = temp;
                    i++;
                    if (i >= args.Length)
                    {
                        PrintError("missing argument for -model -cal");
                        return false;
                    }
                    temp = Convert.ToDouble(args[i]);
                    calImage = temp;
                    calOn = true;
                }
                else
                {
                    PrintError("Unknown argument for -model: " + args[i]);
                    return false;
                }
            }
            else if (args[i] == "-set")
            {
                i++;
                if (i >= args.Length)
                {
                    PrintError("missing argument for -set");
                    return false;
                }
                if (args[i] == "default")
                {
                    showFreq = true;
                    showReal = true;
                    showImage = true;
                    showZ = true;
                    showGain = false;
                    showVol = false;
                    showTemp = false;
                    avgNum = 1;
                    repNum = 1;
                    setFreq = 5000;
                    setVol = AD5933.OUTPUT_2V;
                    setGain = AD5933.GAIN_1;
                    disConnect = false;
                    reConnect = false;
                    waitTime = 0;
                    sweepOn = false;
                    sweepStart = 0;
                    sweepEnd = 0;
                    sweepStep = 0;
                    saveStart = false;
                    saveEnd = false;
                    saveOn = false;
                    foreach (string key in modelsOn.Keys)
                    {
                        modelsOn[key] = false;
                    }
                }
                else
                {
                    PrintError("Unknown argument for -set: " + args[i]);
                    return false;
                }
            }
            else if (args[i] == "-exit")
            {
                exit = true;
                return true;
            }
            else if (args[i] == "-connect" && disConnect) reConnect = true;
            else if (args[i] == "-help")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("==========[Help]==========");
                Console.WriteLine("-show [arg] ... : [arg] = freq | real | image | z | vol | gain | temp | all");
                Console.WriteLine("-hide [arg] ... : [arg] = freq | real | image | z | vol | gain | temp | all");
                Console.WriteLine("-avg [int] : average of [int] times measure, if [int] is negative, show truncated mean");
                Console.WriteLine("-rep [int] : repeat [int] times measure");
                Console.WriteLine("-fre [uint] : set Frequency to [uint]");
                Console.WriteLine("-vol [vol] : set Output Voltage, [vol] = 2v | 1v | 400mv | 200mv");
                Console.WriteLine("-gain [gain] : set Gain, [gain] = x1 | x5");
                Console.WriteLine("-wait [int] : set wait time(ms) during repeated measure");
                Console.WriteLine("-set default : set all settings to default");
                Console.WriteLine("-sweep -on [stratFreq] [endFreq] [stepFreq] : turn on sweep mode from [startFreq] to [endFreq] with step of [stepFreq]");
                Console.WriteLine("-sweep -off : stop sweep mode");
                Console.WriteLine("-save [FileName] : open [FileName] and start to save data");
                Console.WriteLine("-save -off : close the file and save the data in CSV format");
                Console.WriteLine("-model -on [ModelName] ... : turn on the predictive model, [ModelName] can be all");
                Console.WriteLine("-model -off [ModelName] ... : turn off the predictive model, [ModelName] can be all");
                Console.WriteLine("-model -list : list all predictive models");
                Console.WriteLine("-model -cal : callibrate predictive models which are turned on");
                Console.WriteLine("-exit : close the device and exit");
                Console.WriteLine("==========================");
            }
            else
            {
                PrintError("Error: Unknown command!");
            }
        }
        return false;
    }

    static int Main()
    {
        SimpleUsb.Init();
        AD5933 ad5933 = new AD5933();
        AddModels();
        StreamWriter? sw = null;

        double Z = 0;
        double real = 0;
        double image = 0;

        string msg = "";

        Console.WriteLine(".___    _   _____      ___  _");
        Console.WriteLine("| _ \\  /_\\ / __\\ \\    / / || |");
        Console.WriteLine("|   / / _ \\ (__ \\ \\/\\/ /| __ |");
        Console.WriteLine("|_|_\\/_/ \\_\\___| \\_/\\_/ |_||_|");
        Console.WriteLine("====[AD5933 C# Shell v1.6]====");

        Console.ForegroundColor = ConsoleColor.Red;
        if (ad5933.Open(VID, PID) == false)
        {
            Console.WriteLine("\nPlease check whether AD5933 is connected properly, then use command: -connect");
            disConnect = true;
        }
        Console.ForegroundColor = ConsoleColor.White;

        while (true)
        {
            while (!GetCommand())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Command Received!");
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (reConnect)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (ad5933.Open(VID, PID) == false)
                {
                    Console.WriteLine("\nRe-connect Failed!");
                    disConnect = true;
                }
                else
                {
                    disConnect = false;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Connect Successfully!");
                }
                reConnect = false;
                Console.ForegroundColor = ConsoleColor.White;
            }
            if (exit) goto exIT;
            if (disConnect)
            {
                PrintError("AD5933 disconnected! Please check the connection and use conmmand: -connect");
                continue;
            }

            if (saveStart)
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
                if (saveFile != null && saveFile.Length > 0) sw = new StreamWriter(saveFile, false);
                string str = "";
                if (showFreq) str += "Frequency(Hz),";
                if (showReal) str += "Real,";
                if (showImage) str += "Image,";
                if (showZ) str += "Z,";
                if (showVol) str += "Voltage(V),";
                if (showGain) str += "Gain,";
                if (showTemp) str += "Temperature(℃),";
                foreach (KeyValuePair<string, bool> kvp in modelsOn)
                {
                    if (kvp.Value) str += kvp.Key + ",";
                }
                str.Remove(str.LastIndexOf(','), 1);
                if (sw != null)
                {
                    sw.WriteLine(str);
                    saveOn = true;
                    saveStart = false;
                }
                else 
                {
                    PrintError("Failed to Open File!");
                    saveOn = false;
                    saveStart = false;
                }
            }

            if (saveEnd)
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
                saveOn = false;
                saveEnd = false;
            }

            if (sweepOn) setFreq = sweepStart;

            do
            {
                for (int j = 0; j < repNum; j++)
                {
                    real = 0;
                    Z = 0;
                    image = 0;
                    msg = "";
                    if (avgNum > 0)
                    {
                        for (int i = 0; i < avgNum; i++)
                        {
                            if (ad5933.Write(setFreq, setVol, setGain, 5000) == false)
                            {
                                PrintError("Write Failed!");
                                disConnect = true;
                                break;
                            }
                            if (ad5933.Read(5000) == false)
                            {
                                PrintError("Read Failed!");
                                disConnect = true;
                                break;
                            }

                            real += ad5933.Info.Real;
                            image += ad5933.Info.Image;
                        }
                    }
                    else
                    {
                        int[] Reals = new int[-avgNum + 2];
                        int[] Images = new int[-avgNum + 2];
                        double Zmax = 0;
                        double Zmin = 100000;
                        double tempZ = 0;
                        int indexMax = 0;
                        int indexMin = 0;
                        for (int i = 0; i < -avgNum+2; i++)
                        {
                            if (ad5933.Write(setFreq, setVol, setGain, 5000) == false)
                            {
                                PrintError("Write Failed!");
                                disConnect = true;
                                break;
                            }
                            if (ad5933.Read(5000) == false)
                            {
                                PrintError("Read Failed!");
                                disConnect = true;
                                break;
                            }
                            Reals[i] = ad5933.Info.Real;
                            Images[i] = ad5933.Info.Image;
                            tempZ = Math.Sqrt(Reals[i] * Reals[i] + Images[i] * Images[i]);
                            if (tempZ > Zmax)
                            {
                                Zmax = tempZ;
                                indexMax = i;
                            }
                            if (tempZ < Zmin)
                            {
                                Zmin = tempZ;
                                indexMin = i;
                            }
                        }
                        if (indexMax == indexMin)
                        {
                            Reals[0] = 0;
                            Images[0] = 0;
                            Reals[1] = 0;
                            Images[1] = 0;
                        }
                        else
                        {
                            Reals[indexMax] = 0;
                            Images[indexMax] = 0;
                            Reals[indexMin] = 0;
                            Images[indexMin] = 0;
                        }
                        for (int i = 0; i < -avgNum+2; i++)
                        {
                            real += Reals[i];
                            image += Images[i];
                        }
                    }

                    if (!disConnect)
                    {
                        real /= Math.Abs(avgNum);
                        image /= Math.Abs(avgNum);
                        Z = Math.Sqrt(real * real + image * image);

                        if (showFreq) msg += "F=" + setFreq + " ";
                        if (showReal) msg += "R=" + real + " ";
                        if (showImage) msg += "I=" + image + " ";
                        if (showZ) msg += "Z=" + Z + " ";
                        if (showVol)
                        {
                            if (ad5933.Info.OutVol == AD5933.OUTPUT_2V) msg += "V=" + "2V" + " ";
                            else if (ad5933.Info.OutVol == AD5933.OUTPUT_1V) msg += "V=" + "1V" + " ";
                            else if (ad5933.Info.OutVol == AD5933.OUTPUT_400mV) msg += "V=" + "400mV" + " ";
                            else if (ad5933.Info.OutVol == AD5933.OUTPUT_200mV) msg += "V=" + "200mV" + " ";
                        }
                        if (showGain)
                        {
                            if (ad5933.Info.Gain == AD5933.GAIN_1) msg += "G=" + "x1" + " ";
                            else if (ad5933.Info.Gain == AD5933.GAIN_5) msg += "G=" + "x5" + " ";
                        }
                        if (showTemp) msg += "T=" + ad5933.Info.Temp + "℃ ";

                        ProcessModels(real, image);

                        if (saveOn)
                        {
                            string str = "";
                            if (showFreq) str += setFreq + ",";
                            if (showReal) str += real + ",";
                            if (showImage) str += image + ",";
                            if (showZ) str += Z + ",";
                            if (showVol)
                            {
                                if (ad5933.Info.OutVol == AD5933.OUTPUT_2V) str += "2,";
                                else if (ad5933.Info.OutVol == AD5933.OUTPUT_1V) str += "1,";
                                else if (ad5933.Info.OutVol == AD5933.OUTPUT_400mV) str += "0.4,";
                                else if (ad5933.Info.OutVol == AD5933.OUTPUT_200mV) str += "0.2,";
                            }
                            if (showGain) str += (ad5933.Info.Gain == AD5933.GAIN_1 ? "1," : "5,");
                            if (showTemp) str += ad5933.Info.Temp + ",";
                            foreach (KeyValuePair<string, bool> kvp in modelsOn)
                            {
                                if (kvp.Value) str += modelsValue[kvp.Key] + ",";
                            }
                            str.Remove(str.LastIndexOf(','), 1);
                            if (sw != null) sw.WriteLine(str);
                        }

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(saveOn ? "[save] "+msg : msg);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        foreach (KeyValuePair<string, bool> kvp in modelsOn)
                        {
                            if (kvp.Value)
                            {
                                Console.WriteLine(kvp.Key + "=> " + modelsValue[kvp.Key]);
                            }
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        PrintError("AD5933 disconnected! Please check the connection and use conmmand: -connect");
                        break;
                    }

                    if (waitTime > 0 && j < repNum - 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                        Console.Write("Wait");
                        int temp = waitTime;
                        while (temp >= 1000)
                        {
                            Thread.Sleep(1000);
                            temp -= 1000;
                            Console.Write(".");
                        }
                        Thread.Sleep(temp);
                        Console.Write("/");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write("\n");
                    }
                }

                if (sweepOn)
                {
                    if (setFreq >= sweepEnd) break;
                    else
                    {
                        if (setFreq + sweepStep <= sweepEnd) setFreq += sweepStep;
                        else setFreq = sweepEnd;
                    }
                }
            } while (sweepOn && disConnect == false);
            
        }

    exIT:
        SimpleUsb.Exit();
        return 0;
    }
}
