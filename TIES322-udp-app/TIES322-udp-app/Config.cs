using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIES322_udp_app
{
    static class Config
    {
        /*Static class to hold variables.*/
        //public static int waitingForAck = 0;
        public static int packetDropProp = 0;
        public static int delayAmount = 0;
        public static int bitErrorProp = 0;
        //public static int protocol = 0;
        //public static List<byte[]> messageBuffer = new List<byte[]>();
        //public static uint receiverSeq = 0;
        //public static uint senderSeq = 0;
        //public static byte[] prevSentMsg;
        //public static int nackCount = 0;
        //public static System.Timers.Timer _timer;
        //public static int timerSenderSeqValue = 0;
        //public static int gbnNextSeqNum = 1;
        //public static int gbnBase = 1;
        //public static int gbnExpectedSeqNum = 1;
        //public static int gbnWindowSize = 7; //Max Windowsize(N) = 2^(seqnumberspceinbits) - 1, window size of 1 equals alternating bit protocol behavior
        //public static int gbnN = 7; //Used with sequence number arithmatics like so:  seq = newseq % gbnN where N = 2^gbnSeqLengthInBits. 8 with 3-bit seq.
    }
}
