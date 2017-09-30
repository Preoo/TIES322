using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIES322_udp_app
{
    static class Config
    {
        public static int state = 0;
        public static int packetDropProp = 0;
        public static int delayAmount = 0;
        public static int bitErrorProp = 0;
        public static int protocol = 0;
        public static List<byte[]> messageBuffer = new List<byte[]>();
        public static int Receive_Seq = 0;
        public static int Send_Seq = 0;
        public static byte[] prev_sent_msg;
    }
}
