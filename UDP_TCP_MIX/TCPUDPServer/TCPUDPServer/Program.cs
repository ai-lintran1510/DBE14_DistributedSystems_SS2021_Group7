using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TCPUDPServer
    {
        class Program
        {
            //Available ports stored in a list
            static int[] ports = { 8033, 8034, 8051, 32213, 8939 };
            //Available IP Endpoints stored in a list
            static String[] AvailableIPEs = { "127.0.0.1:8051", "127.0.0.1:32213", "127.0.0.1:8939" };
            //Declaration of Variables for leader election and current IP Endpoint
            static String CurrentLeader;
            static String MyCurrentIPE;
            //Declaration of Variables for Process ID
            static int MyPID = Process.GetCurrentProcess().Id;
            static String MyHost = "127.0.0.1";
            static IPEndPoint ipe;
            static int MyPort;
            static System.Timers.Timer timer;
            //Dictionary for the total ordering of the sequence of requests. 
            static Dictionary<String,int> BookedCars = new Dictionary<string, int>();
            static bool debug = !String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CAR_RENT_DEBUG"));

        static void Main(string[] args)
            {   
                /*Two types of servers are here: 
                 * Tcp for the communication with the client and 
                 * udp for dynmaic discovery of hosts as well as the communication between servers*/
                TcpListener tcpServer = null;
                UdpClient udpServer = null;
                MyPort = 8051; //Default port 

                try
                {
                    if (args.Length == 0)
                        ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8051);
                    else if (args.Length == 1)
                        ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Convert.ToInt32(args[0]));
                    else if (args.Length == 2)
                        ipe = new IPEndPoint(IPAddress.Parse(args[1].Trim()), Convert.ToInt32(args[0]));
                    else
                        ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8051);

                }
                catch
                {
                    ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8051);
                }

                MyHost = ipe.Address.ToString();
                MyPort = ipe.Port;

                MyCurrentIPE = $"{MyHost}:{MyPort}";
                
                /*Heartbeat messages in form of a timer 
                 *This one checks up every 6 seconds if the current leader is still 
                 *active and functioning.
                 *If there is no signal coming back, leader election will start.*/
                timer = new System.Timers.Timer();
                timer.Interval = 6000;
                timer.Elapsed += OnTimerEvent;
                timer.Enabled = true;
                timer.Start();

                if(debug) Console.WriteLine(string.Format("Starting TCP and UDP servers on port {0}...", MyPort));

                try
                {
                    ipe = new IPEndPoint(IPAddress.Parse(MyHost), MyPort);
                    
                    /*On the server, there are two types of communcation: 
                     * udp(User Datagram Protocol) for the communication amongst servers and also for new participants 
                     * tcp (Transmission Control Protocol) for the communcation between server and client, while the application 
                     * is running. */
                    udpServer = new UdpClient(MyPort);
                    tcpServer = new TcpListener(ipe);

                    //STarting udpThread
                    var udpThread = new Thread(new ParameterizedThreadStart(UDPServerProc));
                    udpThread.IsBackground = true;
                    udpThread.Name = "UDP server thread";
                    udpThread.Start(udpServer);

                    //Starting tcpThread
                    var tcpThread = new Thread(new ParameterizedThreadStart(TCPServerProc));
                    tcpThread.IsBackground = true;
                    tcpThread.Name = "TCP server thread";
                    tcpThread.Start(tcpServer);

                Console.WriteLine("Press <ENTER> to stop the servers.");
                    Console.ReadLine(); //by pressing Enter twice the server retires. 
                }
                catch (Exception ex)
                {
                    if (debug) Console.WriteLine("Main exception: " + ex);
                }
                finally
                {
                    //Closing udp Server
                    if (udpServer != null)
                        udpServer.Close();

                    //Closing tcp Server
                    if (tcpServer != null)
                        tcpServer.Stop();
                }

                Console.WriteLine("Press <ENTER> to exit.");
                Console.ReadLine();
            }

            //========================================//
            //             UDP Server                 //
            //========================================//

            //Method for the Udp Server
            private static void UDPServerProc(object arg)
            {
                Console.WriteLine($"UDP server thread started:{MyHost}:{MyPort}:{MyPID}");

                UdpClient server = (UdpClient)arg;
                IPEndPoint remoteEP;
                
                //byte Array that receives the messages of other servers
                byte[] buffer;
                
                //Starting Leader Election, that answers the question: which server takes the leading role?
                LeaderElection();

                for (; ; )
                {

                    try
                    {
                        remoteEP = null;
                        //buffer for receiving
                        buffer = server.Receive(ref remoteEP);

                        if (buffer != null && buffer.Length > 0)
                        {
                            String ReceivedMsg = Encoding.ASCII.GetString(buffer);
                            if (debug) Console.WriteLine($"UDP: {ReceivedMsg}. CurrentLeader:{CurrentLeader}");

                            /*For Leader Election we are implementing the LeLann-Chang and Roberts 
                             * Algorithm. That means, one server can initiate leader election. 
                             * If that is the case, he then will tell his neighbor its unique ID 
                             * (in that case the port ID). 
                             * That procedure will go on until the starting point is reached again. The highest ID 
                             * will take the leading role.
                             * In the following Code, that procedure is implemented.*/

                            if (ReceivedMsg.Equals("LEADER_ID"))
                            {
                                var Reply = Encoding.ASCII.GetBytes($"LEADER_ID_BACK,{MyHost},{MyPort},{MyPID}");
                                server.Send(Reply, Reply.Length,remoteEP);
                            }
                            else if (ReceivedMsg.StartsWith("LEADER_ID_BACK"))
                            {
                                Console.WriteLine($"LEADER_ID_BACK....");

                            }
                            else
                            {
                                if ($"{MyHost}:{MyPort}".Equals(CurrentLeader)) {
                                    Console.WriteLine($"I CAN HELP!!!!!!!!!!!!!!!!!!");
                                }
                            }
                        }

                    }
                   
                    catch (Exception ex)
                    {
                        Debug.WriteLine("UDPServerProc exception: " + ex.Message);
                    }
                }

            }

            //========================================//
            //             IsBookable                 //
            //========================================//
            //Method that checks if the Car is still bookable
            private static bool IsBookable(String car)
            {
                bool IsCarBooked = true;

                foreach(String c in BookedCars.Keys)
                {
                    /*if one client has already book a car, that equals with the 
                     * one another client wants, it then is blocked from booking.*/
                    if (c.Equals(car) || (c.StartsWith(car)))
                        IsCarBooked = false;
                }

                return IsCarBooked;
            }

            //========================================//
            //             BookACar                   //
            //========================================//
            //Method for Booking a Car. 
            private static void BookACar(String car)
            {

                int res;
                if(!BookedCars.TryGetValue(car, out res))
                {
                    BookedCars.Add(car, 1);
                }

            }

            //========================================//
            //             UnBookACar                   //
            //========================================//
            //Method to reverse a booking
            private static void UnBookACar(String car)
            {

                int res;
                if (BookedCars.TryGetValue(car, out res))
                {
                    BookedCars.Remove(car);
                }

            }

        //========================================//
        //             TCP Server                 //
        //========================================//
        //Method for tcp Server
        private static void TCPServerProc(object arg)
            {
                Console.WriteLine($"TCP server thread started:");

                TcpListener server = (TcpListener)arg;

                //byte Array for exchanging messages between server and client
                byte[] buffer = new byte[200];
                int count;

            server.Stop();
                server.Start();

            for (; ; )
                {
                    try
                    {
                        //A socket is created fora targeted communication between server and client
                        Socket client = server.AcceptSocket();

                        count = client.Receive(buffer);
                        
                        String Ask = Encoding.UTF8.GetString(buffer, 0, count);

                        if (debug) Console.WriteLine($"Server: Received from client:{Ask}");
                        if (debug) Console.WriteLine($"CurrentLeader={CurrentLeader};MyIPE={MyCurrentIPE}");

                    if (MyCurrentIPE.Equals(CurrentLeader))
                    {
                        if (debug) Console.WriteLine("TCP2: " + Ask + $" {CurrentLeader}");

                        //Sending out request to the client to start the communication. 
                        if (Ask.Equals("PING") || Ask.StartsWith("PIN") || Ask.StartsWith("PI"))
                        {
                            buffer = Encoding.UTF8.GetBytes("PONG");
                          
                            client.Send(buffer);

                        }

                        //Asking for the requirements: Car, Date and Payment method
                        else if (Ask.Equals("Mercedes") || Ask.StartsWith("Mer") || Ask.StartsWith("Me"))
                        {
                            Console.WriteLine($"Client wants a Mercedes. Check if available....");
                        }
                        else if (Ask.Equals("BMW") || Ask.StartsWith("BMW") || Ask.StartsWith("BM"))
                        {
                            Console.WriteLine($"Client wants a BMW. Check if available....");
                        }
                        else if (Ask.Equals("Audi") || Ask.StartsWith("Aud") || Ask.StartsWith("Au"))
                        {
                            Console.WriteLine($"Client wants an Audi. Check if available....");
                        }
                        else if (Ask.Equals("VW") || Ask.StartsWith("VW"))
                        {
                            Console.WriteLine($"Client wants a VW. Check if available....");
                        }
                        else if (Ask.Equals("Porsche") || Ask.StartsWith("Pors") || Ask.StartsWith("Po"))
                        {
                            Console.WriteLine($"Client wants a Porsch. Check if available....");
                        }

                        if (IsBookable(Ask)) {
                            buffer = Encoding.ASCII.GetBytes("ok");
                            BookACar(Ask);
                        }
                        else
                        {
                            buffer = Encoding.ASCII.GetBytes("nok");
                        }

                        client.Send(buffer);

                        //Hold Back Queue
                        Queue q = new Queue();
                        q.Enqueue(buffer);


                    }


                }
                catch (Exception ex)
                {
                    if(ex is SocketException && debug)
                        Console.WriteLine("TCPServerProc exception: " + ex);
                }

                }

                  
        }

        //================================//
        //       SetMeToLeader           //
        //================================//
        //Method that sets the server with the highest port ID as leader. 
        static void SetMeToLeader()
        {
            Leader.LeaderHostName = MyHost;
            Leader.PortNr = MyPort;
            Leader.ImLeader = true;
            Leader.MyPID = MyPID;
        }


        //================================//
        //       OnTimerEvent            //
        //================================//
        /*TimerEvent that starts the LeaderElection as there is no signal
         *coming back in the fixed time interval of 6 seconds */
        private static void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            LeaderElection();

        }

        //================================//
        //       LeaderSelection          //
        //================================//
        //Method for LeaderElection
        private static async void LeaderElection()
        {
            SetMeToLeader();

            if (debug) Console.WriteLine($"#####################################> Before loop Leader:({Leader.LeaderHostName},{Leader.PortNr},{Leader.MyPID},{Leader.ImLeader})");


            for (int i = 0; i < AvailableIPEs.Length; i++)
            {
                if (debug) Console.WriteLine($"=======>AvailableIPEs={AvailableIPEs[i]}");
                //List of Available IP Endpoints will be used and splitted after the ":"
                String[] HostNPort = AvailableIPEs[i].Split(':');

                String _Host = HostNPort[0].Trim();
                int _Port = Convert.ToInt32(HostNPort[1].Trim());

                if(debug) Console.WriteLine($"======> LeaderElection:Checking for {_Host} and {_Port}");

                try
                {
                    
                    if (_Host.Equals(MyHost) && _Port == ipe.Port)
                    {
                        // Do nothing
                    }
                    else
                    {
                        UdpClient client = new UdpClient();
                        client.Connect(_Host, _Port);
                        var datagram = Encoding.ASCII.GetBytes("LEADER_ID");

                        //Sending Port ID 
                        if(debug) Console.WriteLine($"Sending....{_Host}:{_Port}");
                        client.Send(datagram, datagram.Length);

                        var result = await client.ReceiveAsync();

                        String ReceivedMessage = Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length);

                        if (debug) Console.WriteLine($"LeaderElection:Received:{ReceivedMessage}.");

                        if (ReceivedMessage.StartsWith("LEADER_ID_BACK"))
                        {
                            String[] HostPortPID = ReceivedMessage.Split(',');
                            
                            //Variable int Leader and initate it with null
                            int LeaderID = 0;
                            LeaderID = Convert.ToInt32(HostPortPID[3].Trim());
                            if (debug)
                            {
                                //exchanging Port IDs to finalize the leader
                                Console.WriteLine($"======> SERVER Class Leader:{Leader.LeaderHostName},{Leader.PortNr},{Leader.MyPID}");
                                Console.WriteLine($"======> SERVER: Received:    {HostPortPID[1]},{HostPortPID[2]},{HostPortPID[3]}");
                                Console.WriteLine($"======> SERVER: LeaderID:    {LeaderID}");
                            }

                            //If another leader ID is bigger than my leader ID, then...
                            if (LeaderID > Leader.MyPID)
                            {
                                if(debug) Console.WriteLine($"++++++++++> Other({HostPortPID[1]},{HostPortPID[2]},{HostPortPID[3]}) and Currren Leader ({Leader.MyPID})");
                                Leader.LeaderHostName = HostPortPID[1];
                                Leader.PortNr = Convert.ToInt32(HostPortPID[2].Trim());
                                //... he is the new leader
                                Leader.MyPID = LeaderID;
                                Leader.ImLeader = true;
                            }

                            //break;
                        }
                    }

                }
                catch
                {
                    if(debug) Console.WriteLine($"Exception:{AvailableIPEs[i]}");
                }

            }
            CurrentLeader = $"{Leader.LeaderHostName}:{Leader.PortNr}";
            if (debug)
                Console.WriteLine($"#####################################> After loop Leader:({CurrentLeader})");


        }


    }
}



