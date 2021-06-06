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
    //This source was used to look up socket implementation
    //https://stackoverflow.com/questions/46882815/c-sharp-sockets-tcp-udp


    class Program
    {
        //Int Array that stores all ports
        static int[] ports = { 8034, 8034, 8051, 32213, 8939 };
        //String Array that hold avaiable IP Endpoints
        static String[] AvailableIPEs = { "127.0.0.1:8051", "127.0.0.1:32213", "127.0.0.1:3389" };
        static String Host = "127.0.0.1";
        static int Port = 8051; //Default port
        //Variable timer for an implemented timer 
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
            //Starting the client
            task = StartClient();

            //Socket connect timeout was being handled in that source. We were looking for help in that source. 
            //https://stackoverflow.com/questions/1062035/how-to-configure-socket-connect-timeout#:~:text=However%2C%20TCP%20is%20not%20controlled%20by%20your%20timer.,to%20checking%20after%202%20second%20whether%20connection%20succeeded.

            /*Timer implemented that checks up if the server is still functioning and existing. 
             * It checks up every 50 seconds.*/
            timer = new Timer();
            timer.Interval = 5000;
            timer.Elapsed += OnTimerEvent;
            timer.Enabled = true;
            timer.Start();

            ConsoleKeyInfo key; //for reading in what the clients types into the keyboard. 
            bool finish = false;

            //While not finish, which means if there is still input coming from the client, the following is going to happen...
            while (!finish)
            {
                GetInfo(); //method to get the typed in Information

                //If the client has not selected a car, he will get a warning or message that no choice was made. 
                if (String.IsNullOrEmpty(WantedCar))
                {
                    Console.WriteLine($"No car is selected.");
                }
                //If a car was selected, the server will be explicitely contacted and the requiry will be processed. 
                else 
                {
                    Console.WriteLine($"Searching for your car:{WantedCar} .... Please wait one second or two...");
                    StartProc();
                }

                //Then asking the client if he wants to continue which means, book another car. 
                Console.WriteLine($"Do you want to continue [y/n]?");
                key = Console.ReadKey(true);
                switch (key.Key)
                {
                    //yes = y, another car can be booked
                    case ConsoleKey.Y:
                        finish = false;
                        break;

                    //no = n, the application stops running
                    case ConsoleKey.N:
                        finish = true;
                        break;

                }
            }

        }

        //=================================//
        //    GetInfo                    //
        //================================//
        //Method getInfo to get all the requirements of the client
        public static void GetInfo()
        {
            //Reading in the info typed into the keyboard
            ConsoleKeyInfo key; 

            Console.WriteLine($"Welcome to our CAR RENT.");
            Console.WriteLine($"Select your car:");
            bool run = true;

            //While the application is running (means boolean run = true), then...
            while (run)
            {
                //the Client can select a car
                Console.WriteLine("Press 'M' for Mercedes, 'B' for BMW, 'A' for Audi, 'V' for 'VW', 'P' for Porsche or 'X' to exit.");
                key = Console.ReadKey(true);

                //here the different choices are separated from each other so that the application can handle it
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
                //The next step is to enter the desired date
                Console.WriteLine($"Enter start and End date: For Example: {DateTime.Now.ToString("dd.MM.yyyy")}-{DateTime.Now.AddDays(4).ToString("dd.MM.yyyy")}");
                //Reading in the typed in information
                String read = Console.ReadLine();
                try
                {
                    //Checking and preparation for processing the typed in information
                    String[] BeginEnd = read.Split('-');
                    StartDay = DateTime.Parse(BeginEnd[0]);
                    EndDay = DateTime.Parse(BeginEnd[1]);

                    //Check up if the typed in info is correct
                    if (EndDay > StartDay)
                        run = false;
                    else
                        //If there was an error dected, the client will be informed
                        Console.WriteLine($"Wrong day format. Please try again.");

                }
                catch
                {
                    Console.WriteLine($"Wrong day format. Please try again.");
                }

            }

            run = true;
            //Lastly, the payment method will be asked.
            Console.WriteLine($"Your Payment:");
            while (run)
            {
                //For that purpose some choice are here to choose from
                Console.WriteLine("Press 'C' for Cash in Store, 'M' for MasterCard, 'V' for VisaCard, 'P' for PayPal, 'T' for MoneyTransfer or 'X' to exit.");
                key = Console.ReadKey(true);

                //Different choices leads to different methods and so the application can handle it
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
        //Starting the protocol (the communcation)
        public static void StartProc()
        {
            //Server and client communicate via tcp due to reliable reasons
            TcpClient tcpClient = null;
            NetworkStream tcpStream = null;

            //Reading in the typed in information
            ConsoleKeyInfo key;
            bool run = true;
            byte[] buffer = new byte[2048];

            //Stopwatch that stops the time to see if there is a signal coming back from the server in that given time
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

            if (String.IsNullOrEmpty(avServer))
            {
                if (!String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CAR_RENT_TEST")))
                    avServer = "192.168.178.24:8939";
            }   

            //If there was no signal coming back, there is no server. That warning will then pop up. 
            if (String.IsNullOrEmpty(avServer))
            {
                Console.WriteLine($"WARNING: Currently no server is available. Try to get other server.");
                return;
            }
            else
            {
                String[] Host_Port = avServer.Split(':');

                if (debug) Console.WriteLine($"A vailable server:{avServer}");

                Host = Host_Port[0];
                Port = Convert.ToInt32(Host_Port[1]);

            }
            //Starting udp and tcp server
            if(debug) Console.WriteLine(string.Format("Starting TCP and UDP clients on host{0} port {1}...", Host, Port));

            try
            {
                while (run && !String.IsNullOrEmpty(avServer))
                {
                    //new Instance of a tcp Client will be handled
                    tcpClient = new TcpClient();
                    //Connect the client
                    tcpClient.Connect(Host, Port);

                    //Getting the information about WantedCar
                    buffer = Encoding.UTF8.GetBytes(WantedCar);

                    ASCIIEncoding asen = new ASCIIEncoding();

                    byte[] ba = asen.GetBytes(WantedCar);

                    tcpStream = tcpClient.GetStream();

                    tcpStream.Write(ba, 0, ba.Length);

                    //Requesting Server for the desired Car
                    Console.WriteLine($"Starting to request server for {WantedCar}...");

                    int count = 0;
                    
                    count = tcpStream.Read(buffer, 0, buffer.Length);
                    String answer = Encoding.ASCII.GetString(buffer, 0, count);

                    //Getting an answer about the request
                    if (debug) Console.WriteLine($"Server Answer for {WantedCar}:{answer} ");

                    //if the answer is "ok", then the car is booked
                    if (answer.Equals("ok"))
                    {
                        Console.WriteLine($"Car is booked.");
                    }
                    //if the answer is other than "ok", another client has booked that car already
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
                //Console.WriteLine($"{e.Message}");
                return;
            }

        }

        //================================//
        //       OnTimerEvent            //
        //================================//
        //Timer Event that checks up if the server is still existing and functioning. 
        private static void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            //Available Server, setting it here on empty string, but when there are existing and functionign servers, it will fill up
            avServer = "";
            //Starting CLient
            task = StartClient();

        }

        //================================//
        //       OnTimerEvent1            //
        //================================//
        //Timer Event for another check up
        private static void OnTimerEvent1(object sender, ElapsedEventArgs e)
        {
            task.Wait();
            avServer = task.Result;

            //If the String avServer is null or empty, then there is no server = no signal came back
            if (String.IsNullOrEmpty(avServer))
            {
                ServerFound = false;
            }

        }
        //===============================================//
        //              StartClient                      //
        //==============================================//
        //Method to start the client
        public static async Task<String> StartClient()
        {
            //When a client comes up, he asks who is the leading server
            String cl = await Task.Run(() => GetMyServer());
            //Then gets the answer for that
            return cl;

        }

        //===============================================//
        //                GetMyServer                    //
        //===============================================//

        //Method to get the current leading server
        public static String GetMyServer()
        {
            String ServerIPE = "";

            try
            {
                //Getting through the list of Available IP Endpoints
                for (int i = 0; i < AvailableIPEs.Length; i++)
                {
                    //Searching for port Number
                    if(debug) Console.WriteLine($"Searching port number:{AvailableIPEs[i]}");

                    try
                    {
                        String[] HostNPort = AvailableIPEs[i].Split(':');
                        String _Host = HostNPort[0].Trim();
                        int _Port = Convert.ToInt32(HostNPort[1].Trim());
                        byte[] buf = new byte[1024];

                        /*We were having problems by creating timeouts for tcp clients, so the following link helped us. For references reasons. 
                         *https://www.codeproject.com/questions/638955/how-to-set-the-timeout-for-tcp-client-connect-meth */

                        //================= TEST With Timeout =============================================//

                        using (TcpClient tcp = new TcpClient())
                        {
                            //Trying to connect to server
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
                        } 

                        //================= TEST With Timeout =============================================//

                        TcpClient tcpclient = new TcpClient();
                        tcpclient.Connect(_Host, _Port);
                        //Try to connect to server
                        if(debug) Console.WriteLine($"Connecting to...{_Host}:{_Port}");
                        buf = Encoding.ASCII.GetBytes("PING");
                        NetworkStream stm = tcpclient.GetStream();
                        //stm.ReadTimeout = 6;
                        stm.Write(buf, 0, buf.Length);
                        if(debug) Console.WriteLine($"Message sent to...{_Host}:{_Port}");

                        //========= Now Read ==============//
                        int count = 0;
                        //Byte Array for exchanging messages
                        buf = new byte[1024];
                        if(debug) Console.WriteLine($"Begin reading...{_Host}:{_Port}");

                        count = stm.Read(buf, 0, buf.Length);
                        
                        if(debug) Console.WriteLine($"Message read from...{_Host}:{_Port}");

                        String ans = Encoding.ASCII.GetString(buf, 0, count);

                        if(debug) Console.WriteLine("TCP-GetMyServer: " + ans);

                        if (ans.Equals("PONG"))
                        {
                            ServerIPE = $"{_Host}:{_Port}";
                            tcpclient.Close();
                            break;
                        }

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
