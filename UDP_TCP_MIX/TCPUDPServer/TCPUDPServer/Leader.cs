using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPUDPServer
{
    static class Leader
    {
        public static String LeaderHostName { get; set; }
        public static int PortNr { get; set; }
        public static int MyPID { get; set; }
        public static bool ImLeader = false;
    }
}
