using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TCPUDPClient
{

    //https://stackoverflow.com/questions/46882815/c-sharp-sockets-tcp-udp


    class Program
    {

        static int[] ports = { 8034, 8034, 8051, 32213, 8939 };
        //static String[] AvailableIPEs = { "127.0.0.1:8033", "127.0.0.1:8034", "127.0.0.1:8051", "127.0.0.1:32213", "192.168.178.37:8939" };
        //static String[] AvailableIPEs = { "127.0.0.1:8051", "127.0.0.1:32213", "192.168.178.24:8939" };
        static String[] AvailableIPEs = { "127.0.0.1:8051", "127.0.0.1:32213", "127.0.0.1:3389" };
        static String Host = "127.0.0.1";
        static int Port = 8051;
        static Timer timer;
        static Timer timer1;
        static IPAddress ipaddress = IPAddress.Loopback;
        static String avServer = "127.0.0.1:8051";
        static Task<String> task;
        static bool ServerFound = false;
        static String WantedCar = "";
        static String MyPayment = "";
        static DateTime StartDay;
        static DateTime EndDay;
        static bool debug = !String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CAR_RENT_DEBUG"));

        static void Main(string[] args)
        {
            task = StartClient();
            //https://stackoverflow.com/questions/1062035/how-to-configure-socket-connect-timeout#:~:text=However%2C%20TCP%20is%20not%20controlled%20by%20your%20timer.,to%20checking%20after%202%20second%20whether%20connection%20succeeded.

            timer = new Timer();
            timer.Interval = 5000;
            timer.Elapsed += OnTimerEvent;
            timer.Enabled = true;
            timer.Start();

            //timer1 = new Timer();
            //timer1.Interval = 5000;
            //timer1.Elapsed += OnTimerEvent1;
            //timer1.Enabled = true;
            //timer1.Start();

            ConsoleKeyInfo key;
            bool finish = false;
            while (!finish)
            {
                GetInfo();

                if (String.IsNullOrEmpty(WantedCar))
                {
                    Console.WriteLine($"No car is selected.");
                }
                else
                {
                    Console.WriteLine($"Searching for your car:{WantedCar} .... Please wait one second or two...");
                    StartProc();
                }

                Console.WriteLine($"Do you want to continue [y/n]?");
                key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        finish = false;
                        break;

                    case ConsoleKey.N:
                        finish = true;
                        break;

                }
            }

        }

        //=================================//
        //    GetInfo                    //
        //================================//
        public static void GetInfo()
        {
            ConsoleKeyInfo key;

            Console.WriteLine($"Welcome to our CAR RENT.");
            Console.WriteLine($"Select your car:");
            bool run = true;

            while (run)
            {
                Console.WriteLine("Press 'M' for Mercedes, 'B' for BMW, 'A' for Audi, 'V' for 'VW', 'P' for Porsche or 'X' to exit.");
                key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.M:
                        WantedCar = "Mercedes";
                        Console.WriteLine($"{WantedCar} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.B:
                        WantedCar = "BMW";
                        Console.WriteLine($"{WantedCar} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.A:
                        WantedCar = "Audi";
                        Console.WriteLine($"{WantedCar} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.V:
                        WantedCar = "VW";
                        Console.WriteLine($"{WantedCar} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.P:
                        WantedCar = "Porsche";
                        Console.WriteLine($"{WantedCar} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.X:
                        run = false;
                        break;
                }
            }

            run = true;
            while (run)
            {
                Console.WriteLine($"Enter start and End date: For Example: {DateTime.Now.ToString("dd.MM.yyyy")}-{DateTime.Now.AddDays(4).ToString("dd.MM.yyyy")}");
                String read = Console.ReadLine();
                try
                {
                    String[] BeginEnd = read.Split('-');
                    StartDay = DateTime.Parse(BeginEnd[0]);
                    EndDay = DateTime.Parse(BeginEnd[1]);

                    if (EndDay > StartDay)
                        run = false;
                    else
                        Console.WriteLine($"Wrong day format. Please try again.");

                }
                catch
                {
                    Console.WriteLine($"Wrong day format. Please try again.");
                }

            }

            run = true;
            Console.WriteLine($"Your Payment:");
            while (run)
            {
                Console.WriteLine("Press 'C' for Cash in Store, 'M' for MasterCard, 'V' for VisaCard, 'P' for PayPal, 'T' for MoneyTransfer or 'X' to exit.");
                key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.C:
                        MyPayment = "Cash";
                        Console.WriteLine($"{MyPayment} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.M:
                        MyPayment = "MasterCard";
                        Console.WriteLine($"{MyPayment} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.V:
                        MyPayment = "VisaCard";
                        Console.WriteLine($"{MyPayment} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.P:
                        MyPayment = "PayPal";
                        Console.WriteLine($"{MyPayment} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.T:
                        MyPayment = "MoneyTransfer";
                        Console.WriteLine($"{MyPayment} is selected.");
                        run = false;
                        break;

                    case ConsoleKey.X:
                        run = false;
                        break;
                }

            }


        }

        //=================================//
        //    StartProc                    //
        //================================//
        public static void StartProc()
        {
            //UdpClient udpClient = null;
            TcpClient tcpClient = null;
            NetworkStream tcpStream = null;
            ConsoleKeyInfo key;
            bool run = true;
            byte[] buffer = new byte[2048];

            Stopwatch st = new Stopwatch();
            st.Start();

            task.Wait();
            avServer = task.Result;

            st.Stop();
            TimeSpan ts = st.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);

            if (debug) Console.WriteLine($"Task.Result={avServer}:{elapsedTime}");

            //if (String.IsNullOrEmpty(avServer))
            //{
            //    task.Wait();
            //    avServer = task.Result;

            //    if (String.IsNullOrEmpty(avServer))
            //        task = StartClient();
            //}


            if (String.IsNullOrEmpty(avServer))
            {
                Console.WriteLine($"WARNING: Currently no server is available. Try to get other server.");
                //System.Environment.Exit(0);
                return;
            }
            else
            {
                String[] Host_Port = avServer.Split(':');

                if (debug) Console.WriteLine($"A vailable server:{avServer}");

                Host = Host_Port[0];
                Port = Convert.ToInt32(Host_Port[1]);

            }
            if(debug) Console.WriteLine(string.Format("Starting TCP and UDP clients on host{0} port {1}...", Host, Port));

            try
            {
                while (run && !String.IsNullOrEmpty(avServer))
                {

                    tcpClient = new TcpClient();
                    tcpClient.Connect(Host, Port);


                    //buffer = Encoding.ASCII.GetBytes(WantedCar);
                    buffer = Encoding.UTF8.GetBytes(WantedCar);

                    ASCIIEncoding asen = new ASCIIEncoding();
                    byte[] ba = asen.GetBytes(WantedCar);

                    tcpStream = tcpClient.GetStream();

                    //tcpStream.ReadTimeout = 6;

                    //tcpStream.Write(buffer, 0, buffer.Length);
                    tcpStream.Write(ba, 0, ba.Length);

                    Console.WriteLine($"Starting to request server for {WantedCar}...");

                    int count = 0;
                    //count = tcpStream.Read(buffer, 0, buffer.Length);

                    count = tcpStream.Read(buffer, 0, buffer.Length);
                    String answer = Encoding.ASCII.GetString(buffer, 0, count);

                    if (debug) Console.WriteLine($"Server Answer for {WantedCar}:{answer} ");

                    if (answer.Equals("ok"))
                    {
                        Console.WriteLine($"Car is booked.");
                    }
                    else
                    {
                        Console.WriteLine($"Sorry Your selected car {WantedCar} is not available.");
                    }

                    run = false;

                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"WARNING: Server may be not available. Please try again.");
                return;
            }

        }

        //================================//
        //       OnTimerEvent            //
        //================================//
        private static void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            avServer = "";
            task = StartClient();

            //task.Wait();
            //avServer = task.Result;

            //if (String.IsNullOrEmpty(avServer))
            //{
            //    ServerFound = false;
            //}
            //Console.WriteLine($"Task.Result={avServer}");

        }

        //================================//
        //       OnTimerEvent1            //
        //================================//
        private static void OnTimerEvent1(object sender, ElapsedEventArgs e)
        {
            task.Wait();
            avServer = task.Result;

            if (String.IsNullOrEmpty(avServer))
            {
                ServerFound = false;
            }
            //Console.WriteLine($"Task.Result={avServer}");

        }
        //===============================================//
        //              StartClient                      //
        //==============================================//
        public static async Task<String> StartClient()
        {

            String cl = await Task.Run(() => GetMyServer());
            return cl;

        }

        //===============================================//
        //                GetMyServer                    //
        //===============================================//

        public static String GetMyServer()
        {
            String ServerIPE = "";


            //Stopwatch st = new Stopwatch();
            //st.Stop();
            //TimeSpan ts = st.Elapsed;
            //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //ts.Hours, ts.Minutes, ts.Seconds,
            //ts.Milliseconds / 10);

            try
            {
                for (int i = 0; i < AvailableIPEs.Length; i++)
                {
                    if(debug) Console.WriteLine($"Searching port number:{AvailableIPEs[i]}");

                    try
                    {
                        //client = UdpClientUser.ConnectTo("127.0.0.1", AvailablePortNumbers[i]);
                        String[] HostNPort = AvailableIPEs[i].Split(':');
                        String _Host = HostNPort[0].Trim();
                        int _Port = Convert.ToInt32(HostNPort[1].Trim());
                        byte[] buf = new byte[1024];


                        /*
                        //https://www.codeproject.com/questions/638955/how-to-set-the-timeout-for-tcp-client-connect-meth

                        //================= TEST With Timeout =============================================//

                        using (TcpClient tcp = new TcpClient())
                        {
                            IAsyncResult ar = tcp.BeginConnect(_Host, _Port, null, null);  
                            System.Threading.WaitHandle wh = ar.AsyncWaitHandle;  
                            try 
                            {  
                               if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))  
                               {
                                    tcp.Close();
                                    //throw new TimeoutException();
                                    throw new Exception();
                                }

                                tcp.EndConnect(ar);  
                            }  
                            finally 
                            {  
                                wh.Close();  
                            }
                        } */

                        //================= TEST With Timeout =============================================//

                        TcpClient tcpclient = new TcpClient();
                        tcpclient.Connect(_Host, _Port);

                        if(debug) Console.WriteLine($"Connecting to...{_Host}:{_Port}");
                        buf = Encoding.ASCII.GetBytes("PING");
                        NetworkStream stm = tcpclient.GetStream();
                        stm.ReadTimeout = 6;
                        stm.Write(buf, 0, buf.Length);
                        if(debug) Console.WriteLine($"Message sent to...{_Host}:{_Port}");

                        //========= Now Read ==============//
                        int count = 0;
                        buf = new byte[1024];
                        if(debug) Console.WriteLine($"Begin reading...{_Host}:{_Port}");

                        count = stm.Read(buf, 0, buf.Length);
                        
                        if(debug) Console.WriteLine($"Message read from...{_Host}:{_Port}");

                        String ans = Encoding.ASCII.GetString(buf, 0, count);
                        //Console.WriteLine("TCP-GetMyServer: " + Encoding.ASCII.GetString(buf, 0, count));

                        if(debug) Console.WriteLine("TCP-GetMyServer: " + ans);

                        if (ans.Equals("PONG"))
                        {
                            ServerIPE = $"{_Host}:{_Port}";
                            tcpclient.Close();
                            break;
                        }

                        //using (var stream = tcpclient.GetStream())
                        //{
                        //    while ((count = stream.Read(buf, 0, buf.Length)) != 0)
                        //    {
                        //        Console.WriteLine("TCP-GetMyServer: " + Encoding.ASCII.GetString(buf, 0, count));
                        //    }

                        //    ServerIPE = $"{_Host}:{_Port}";

                        //}
                        tcpclient.Close();

                    }
                    catch (Exception e)
                    {
                        if(debug) Console.WriteLine($"Exception:{AvailableIPEs[i]}");
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            //========= TEST TEST =======//

            return ServerIPE;

        }

        //===============================================//
        //                GetMyServer                   //
        //==============================================//

    }
}
