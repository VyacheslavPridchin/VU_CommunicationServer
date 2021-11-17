using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CustomClasses;

namespace CommunicationSystemServer
{
    class Program
    {
        #region --- Переменные ---
        static Dictionary<string, DateTime> TimesIP = new Dictionary<string, DateTime>();
        static Dictionary<int, List<string>> Lessons = new Dictionary<int, List<string>>();
        static Dictionary<int, string> Teachers = new Dictionary<int, string>();
        static Dictionary<IPEndPoint, setType> Users = new Dictionary<IPEndPoint, setType>();
        static List<UdpClient> udpClients = new List<UdpClient>();
        static bool showLog = false;
        static DateTime startShowConnection;
        static int port = 27005;
        static int countPorts = 5;
        #endregion

        #region --- Отдел преобразований ---

        public static byte[] ObjToArr(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            //bf.Binder = new Classes.CustomBinder();
            bf.Serialize(ms, obj);
            return Zip.Compress(ms.ToArray());
        }

        public static Object ArrToObj(byte[] arrBytes)
        {
            byte[] finalArrBytes = Zip.Decompress(arrBytes);
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(finalArrBytes, 0, finalArrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = binForm.Deserialize(memStream);

            return obj;
        }

        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }
        #endregion

        #region --- Приемник и отправщик ---
        static async private void FuncReceiverAsync()
        {
            //for (int i = 0; i < 20; i++)
            //{
            //    Thread.Sleep(100);
            //    
            //}
            var prg = new Program();
            await Task.Run(() => prg.FuncReceiver(port, countPorts));
        }

        public void FuncReceiver(int port, int countPort)
        {
            try
            {
                Console.WriteLine(DateTime.Now.ToString() + " | Запускаем прослушивание порта " + port);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);
                int currentClient = udpClients.Count;
                bool onlyOnce = false;

                udpClients.Add(new UdpClient(port));
                if (countPort > 0)
                    Task.Run(() => FuncReceiver(port + 1, countPort - 1));

                Console.WriteLine("Прослушивание запущено. Осталось " + countPort);
                while (true)
                {

                    byte[] data = udpClients[currentClient].Receive(ref ipEndPoint);      // Receive the information from the client as byte array

                    if (showLog)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + " | Данные приняты. Размер: " + data.Length + " Байт. Адрес: " + ipEndPoint.ToString());
                        if (Math.Abs(startShowConnection.Second - DateTime.Now.Second) > 5) { showLog = false; Console.WriteLine(DateTime.Now.ToString() + " | 5 секунд детализации завершены\n"); }
                    }

                    Package pack = ArrToObj(data) as Package;

                    // ------------------------------
                    // ----- Обработка по типам -----
                    // ------------------------------
                    // ----- Обработка по типам -----
                    // ------------------------------
                    // ----- Обработка по типам -----
                    // ------------------------------

                    #region --- Обработка по типам ---

                    if (TimesIP.ContainsKey(ipEndPoint.ToString()))
                    {
                        TimesIP[ipEndPoint.ToString()] = DateTime.Now;
                    }
                    else
                    {
                        TimesIP.Add(ipEndPoint.ToString(), DateTime.Now);
                    }

                    if(DateTime.Now.Second % 5 == 0 && !onlyOnce)
                    {
                        foreach(var time in TimesIP)
                        {
                            if(DateTime.Now - time.Value > new TimeSpan(0, 0, 20))
                            {
                                TimesIP.Remove(time.Key);
                                foreach (var User in Lessons)
                                {
                                    if (User.Value.Contains(ipEndPoint.ToString()))
                                    {
                                        User.Value.Remove(ipEndPoint.ToString());
                                        Console.WriteLine("Таймаут у IP " + ipEndPoint.ToString());
                                    }
                                }
                            }
                        }
                        onlyOnce = true;
                    } else
                    {
                        onlyOnce = false;
                    }

                    if (Lessons.ContainsKey(int.Parse(pack.lessonID)))
                    {
                        if (!Lessons[int.Parse(pack.lessonID)].Contains(ipEndPoint.ToString()))
                            Lessons[int.Parse(pack.lessonID)].Add(ipEndPoint.ToString());
                    }
                    else
                    {
                        Lessons.Add(int.Parse(pack.lessonID), new List<string> { ipEndPoint.ToString() });
                    }


                    if (pack.type == Package.Types.SetTypePack)
                    {

                        setType type = ArrToObj(pack.data) as setType;

                        if (!Users.ContainsKey(ipEndPoint))
                        {
                            Users.Add(ipEndPoint, new setType(type.type, type.ownerID));
                        }
                        else
                        {
                            Users[ipEndPoint] = new setType(type.type, type.ownerID);
                        }


                        Console.WriteLine("Пользователь " + pack.ownerID + " сменил прослушивание потока на " + type.ownerID + ", с типом " + type.type.ToString());
                    }

                    if (pack.type == Package.Types.GreetingDownload || pack.type == Package.Types.GreetingUpload)
                    {
                        if (pack.type == Package.Types.GreetingUpload)
                        {
                            SQL.AddLink(pack.ownerID, ipEndPoint.ToString(), (port + currentClient).ToString(), pack.lessonID, currentIP, false);
                        }

                        SQL.ChangeIP(pack.ownerID, currentIP.ToString());

                        //if (Lessons.ContainsKey(int.Parse(pack.lessonID)))
                        //{
                        //    if (!Lessons[int.Parse(pack.lessonID)].Contains(ipEndPoint))
                        //        Lessons[int.Parse(pack.lessonID)].Add(ipEndPoint);
                        //}
                        //else
                        //{
                        //    Lessons.Add(int.Parse(pack.lessonID), new List<IPEndPoint> { ipEndPoint });
                        //}

                        Console.WriteLine(DateTime.Now.ToString() + " | Подключен пользователь " + pack.ownerID + " в комнату " + pack.lessonID);
                    }

                    if (pack.type == Package.Types.Parting)
                    {
                        SQL.ChangeIP(pack.ownerID, "null");
                        if (Lessons[int.Parse(pack.lessonID)].Contains(ipEndPoint.ToString()))
                            Lessons[int.Parse(pack.lessonID)].Remove(ipEndPoint.ToString());

                        SQL.DeleteLink(pack.ownerID);

                        Console.WriteLine(DateTime.Now.ToString() + " | Отключен пользователь " + pack.ownerID + " из комнаты " + pack.lessonID);

                        if (TimesIP.ContainsKey(ipEndPoint.ToString()))
                        {
                            TimesIP.Remove(ipEndPoint.ToString());
                        }
                    }

                    if (pack.type == Package.Types.MiniWebcam || pack.type == Package.Types.MiniScreen || pack.type == Package.Types.HoldOn)
                    {
                        Task.Run(() => FuncSenderAsync(data, int.Parse(pack.lessonID), currentClient));
                    }

                    if (pack.type == Package.Types.Audio)
                    {
                        partAudio part = ArrToObj(pack.data) as partAudio;
                        //Обрабатываем аудио, исключая отправляемый IP

                        if (part.isTeacher)
                        {
                            if (Teachers.ContainsKey(int.Parse(pack.lessonID)))
                            {
                                Teachers[int.Parse(pack.lessonID)] = ipEndPoint.ToString();
                            }
                            else
                            {
                                Teachers.Add(int.Parse(pack.lessonID), ipEndPoint.ToString());
                            }

                            foreach (string IP in Lessons[int.Parse(pack.lessonID)])
                            {
                                if (IP.ToString() != ipEndPoint.ToString())
                                {
                                    //if(udpClients.Count > currentClient)
                                    udpClients[currentClient].Send(data, data.Length, CreateIPEndPoint(IP));

                                    if (showLog)
                                    {
                                        Console.WriteLine(DateTime.Now.ToString() + " | Данные аудио отправлены студенту. Размер: " + data.Length + " Байт. Адрес: " + IP.ToString());
                                    }

                                    WorkLoad = WorkLoad + data.Length;
                                }
                            }
                        }
                        else
                        {
                            if (Teachers.ContainsKey(int.Parse(pack.lessonID)))
                            {
                                udpClients[currentClient].Send(data, data.Length, CreateIPEndPoint(Teachers[int.Parse(pack.lessonID)]));

                                if (showLog)
                                {
                                    Console.WriteLine(DateTime.Now.ToString() + " | Данные аудио отправлены преподавателю. Размер: " + data.Length + " Байт. Адрес: " + Teachers[int.Parse(pack.lessonID)]);
                                }

                                WorkLoad = WorkLoad + data.Length;
                            }
                        }
                    }

                    if (pack.type == Package.Types.WebCam || pack.type == Package.Types.Screen)
                    {
                        foreach (KeyValuePair<IPEndPoint, setType> kvp in Users)
                        {
                            if (kvp.Value.ownerID == int.Parse(pack.ownerID))
                            {
                                Task.Run(() => FuncSenderAsync(data, kvp.Key, currentClient));
                            }
                        }
                    }

                    #endregion
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString() + " | Ошибка прослушивания порта " + port + ". " + e.Message + "...\n");
            }
        }

        static UdpClient udpSender = new UdpClient();
        static private void FuncSenderAsync(byte[] data, int ID, int UDPClientID)
        {
            if (Lessons.ContainsKey(ID))
            {
                foreach (string IP in Lessons[ID])
                {

                    udpClients[UDPClientID].Send(data, data.Length, CreateIPEndPoint(IP));

                    if (showLog)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + " | Данные отправлены. Размер: " + data.Length + " Байт. Адрес: " + IP.ToString());
                    }

                    WorkLoad = WorkLoad + data.Length;
                }
            }
            else
            {
                Lessons.Add(ID, new List<string> { });
            }

        }

        static private void FuncSenderAsync(byte[] data, IPEndPoint IP, int UDPClientID)
        {

            udpClients[UDPClientID].Send(data, data.Length, IP);

            if (showLog)
            {
                Console.WriteLine(DateTime.Now.ToString() + " | Данные отправлены. Размер: " + data.Length + " Байт. Адрес: " + IP.ToString());
            }

            WorkLoad = WorkLoad + data.Length;
        }





        static private void Updater()
        {
            while (true)
            {
                WorkLoad = 0;
                SQL.UpdateWorkload(currentIP, WorkLoad);
                Thread.Sleep(1000);
            }
        }
        #endregion

        static int WorkLoad = 0;

        public static string currentIP;

        static void Main(string[] args)
        {
            string command = "";

            Console.WriteLine("Запуск сервера Коммуникационной Системы Виртуального ДГТУ...\n\nИспользуйте символ '/' и наименование команды для работы с сервером.\n/help - справочник по командам.\n");

            Console.WriteLine("Подключение к БД...");

            if (SQL.ConnectDB())
            {
                Console.WriteLine("Подключение к БД выполнено\n");
                Console.WriteLine("Сервер может быть доступен на IP адресах:");
                var host = Dns.GetHostEntry(Dns.GetHostName());
                Console.WriteLine(string.Join("\r" + "\n", host.AddressList.Where(i => i.AddressFamily == AddressFamily.InterNetwork)) + "\n");
                Console.WriteLine("Выберите необходимый: ");
                currentIP = Console.ReadLine();
                Console.WriteLine("Введите кол-во открываемых портов: ");
                countPorts = int.Parse(Console.ReadLine());
                SQL.AddIPToDB(currentIP);
                Task.Run(() => Updater());
                Console.WriteLine();
                FuncReceiverAsync();
            }
            else { Console.WriteLine("Не удалось подключиться к БД\n"); command = "/exit"; }
            while (command != "/exit")
            {
                command = Console.ReadLine();
                if (command == "/clear") { Console.Clear(); Console.WriteLine(DateTime.Now.ToString() + " | Лог очищен.\n"); };
                if (command == "/help")
                {
                    Console.WriteLine(DateTime.Now.ToString() + " | Вызван справочник по командам.\n" +
                    "/exit - завершение работы сервера.\n" +
                    "/clear - очистка лога.\n" +
                    "/5 - запуск 5 секунд детализации трафика. Если подключений не было долгое время, то отобразится последнее.\n");
                }
                if (command == "/5") { showLog = !showLog; if (showLog) { Console.WriteLine(DateTime.Now.ToString() + " | Ожидание подключений..."); startShowConnection = DateTime.Now; } }
            }

            SQL.DeleteIPFromDB(currentIP);

            //SQL.CloseDB();
            Console.WriteLine("Сервер прекратил свою работу.");
            if (command == "/exit")
                Console.ReadKey();
            else Process.Start("CommunicationSystemServer.exe");
        }

        public static class Zip
        {
            public static byte[] Compress(byte[] src)
            {
                using (var input = new MemoryStream(src))
                {
                    using (var output = new MemoryStream())
                    {
                        using (var compressor = new GZipStream(output, CompressionMode.Compress))
                        {
                            input.CopyTo(compressor);
                        }
                        return output.ToArray();
                    }
                }
            }

            public static byte[] Decompress(byte[] src)
            {
                using (var input = new MemoryStream(src))
                {
                    using (var decompressor = new GZipStream(input, CompressionMode.Decompress))
                    {
                        using (var output = new MemoryStream())
                        {
                            decompressor.CopyTo(output);

                            return output.ToArray();
                        }
                    }
                }
            }
        }

    }
}

