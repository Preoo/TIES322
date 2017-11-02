using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Timers;
using System.Diagnostics;
using System.Net.Sockets;

namespace TIES322_udp_app
{
    public delegate void HandleDatagramDelegate(byte[] datagram);
    public delegate void DeliverData(InvokeReason reason, string message);

    public enum InvokeReason
    {
        Sender,
        Receiver,
        Error,
        Debug
    }

    public partial class Form1 : Form
    {
        //public event HandleDatagramDelegate OnSend;
        System.Net.Sockets.UdpClient socket;
        public byte[] previouslySentNoChecksum;
        crc8lib crc8;
        List<byte[]> buffer = new List<byte[]>();
        //byte[][] gbnSendWindow = new byte[(int)Config.gbnWindowSize][];
        Dictionary<int, byte[]> gbnSendWindow = new Dictionary<int, byte[]>();
        VirtualSocket client;
        IRdtProtocol rdtProtocol;
        
        public event HandleDatagramDelegate OnReceive;
        public event HandleDatagramDelegate OnSend;
        public event DeliverData OnDeliver;

        public Form1()
        {
            InitializeComponent();
            crc8 = new crc8lib();
            try
            {
                /* 
                 * Frame is |seq<1byte>|data<x bytes of input.string>|chksum<1byte>, used example chat program to test versions < rdt3
                 * Tried to simulate sender and receiver in one program, printing to separte boxes,
                 * got too many problems trying to get data/messages back to UI thread. Current impl. is a temp -> permanent hack.
                 * Similary used UdpClient as udpsocket for sanity, while calling reliability layer from below.
                 * Opted to keep using udpclient for async benefits... and to follow best practice 
                 * https://docs.microsoft.com/en-us/dotnet/framework/network-programming/best-practices-for-system-net-classes
                 */
                

                //server = new VirtualSocket(int.Parse(textBox_serverport.Text), int.Parse(textBox_clientport.Text), true);

                //Thread clientthread = new Thread(() => new VirtualSocket(8081,8080, true));
                //clientthread.Start();
                //Thread serverthread = new Thread(() => new VirtualSocket(8080,8081,true));
                //serverthread.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("ASD " + e.InnerException);
            }
        }

        private void HandlerMessageFromRdt(InvokeReason reason, string message)
        {
            switch (reason)
            {
                case InvokeReason.Sender:
                    {
                        textBox_Client.AppendText(">>> " + message + Environment.NewLine);
                        break;
                    }
                case InvokeReason.Receiver:
                    {
                        textBox_Client.AppendText("<<< " + message + Environment.NewLine);
                        break;
                    }
                case InvokeReason.Error:
                    {
                        textBox_Server.AppendText("[ERROR] " + message + Environment.NewLine);
                        break;
                    }
                case InvokeReason.Debug:
                    {
                        textBox_Server.AppendText("[DEBUG] " + message + Environment.NewLine);
                        break;
                    }
            }
        }
        
        private void StartUdpSocketListener()
        {
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(async () =>
            {
                using (var udpServer = new System.Net.Sockets.UdpClient(int.Parse(textBox_clientport.Text)))
                {
                    Random rnd = new Random();
                    //var udpServer = socket;
                    socket = udpServer;
                    while (true)
                    {
                        try
                        {
                            var receivedResults = await udpServer.ReceiveAsync();
                            var data = receivedResults.Buffer;
                            if (rnd.Next(100) >= Config.packetDropProp)
                            {
                                if (rnd.Next(101) < Config.bitErrorProp)
                                {
                                    data = checksum.InsertBitError(data);
                                    textBox_Server.AppendText("[] VS inserted bit error []" + Environment.NewLine);
                                }
                                await Task.Delay(rnd.Next(Config.delayAmount * 1000));

                                RdtLayer(data);
                            }
                            else
                            {
                                textBox_Server.AppendText("[] VS dropped something []" + Environment.NewLine);
                            }
                        } catch(Exception e)
                        {
                            if (e is ObjectDisposedException || e is SocketException)
                            {
                                MessageBox.Show(e.InnerException.ToString());
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                MessageBox.Show("This never be called...");
            }, CancellationToken.None, TaskCreationOptions.None, uiContext);
        }
        private uint ToggleSeqInt(uint value)
        {
            return (uint)(value == 1 ? 0 : 1);
        }
        public void RdtLayer(byte[] newDatagram)
        {
            var localData = newDatagram;
            string datagramContent = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(0, localData.Length - 1).ToArray());
            //bool NotCorrupted = checksum.CheckChecksum(localData);
            bool NotCorrupted = crc8.CheckCRC8Chksum(localData);
            
            switch (Config.protocol)
            {
                #region Protocol ACK & NACK 
                case 20:
                    {
                        uint seqFromDatagram = (uint)localData[0];
                        datagramContent = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(1, localData.Length - 2).ToArray());
                        /*Now with receiver side duplicate detection. Receiver set seq in datagrams can be off but it doesnt matter for this version */
                        if (Config.waitingForAck == 1)
                        {
                            if(datagramContent == "ACK" && NotCorrupted && seqFromDatagram == Config.senderSeq)
                            {
                                Config.senderSeq = ToggleSeqInt(Config.senderSeq);
                                if (Config.messageBuffer.Count > 0)
                                {
                                    SendBytes(Config.messageBuffer.First());
                                    
                                    textBox_Client.AppendText("Sent buffered msg" + Environment.NewLine);
                                    Config.messageBuffer.RemoveAt(0);
                                }
                                else
                                {
                                    Config.waitingForAck = 0;
                                    this.Text = "Done";
                                    textBox_Server.AppendText("Received ACK " + seqFromDatagram.ToString() + Environment.NewLine);
                                }
                                

                            }
                            else if (datagramContent == "NACK" && NotCorrupted)
                            {
                                textBox_Server.AppendText("Received NACK("+ seqFromDatagram.ToString() + "), resent: " 
                                    + Encoding.ASCII.GetString(Config.prevSentMsg) + Environment.NewLine);
                                SendBytes(previouslySentNoChecksum);
                                
                            }
                            else if (datagramContent != "ACK" ||!NotCorrupted)
                            {
                                textBox_Server.AppendText("Not ACK or is corrupt, resending: " 
                                    + Encoding.ASCII.GetString(Config.prevSentMsg) + Environment.NewLine);
                                SendBytes(previouslySentNoChecksum);
                                
                            }
                            //else
                            //{
                            //    textBox_Client.Text += "Weird state" + Environment.NewLine;
                            //    textBox_Client.AppendText("Sender seq: " + Config.Sender_Seq.ToString());
                            //    //sendBytes(previouslySentNoChecksum);
                            //}
                            break;
                        }
                        else if (Config.waitingForAck == 0)
                        {
                            if(NotCorrupted && seqFromDatagram == Config.receiverSeq)
                            {
                                byte[] ack = Encoding.ASCII.GetBytes("ACK");
                                byte[] outDatagram = new byte[ack.Length + 1];
                                outDatagram[0] = (byte)Config.receiverSeq;
                                Config.receiverSeq = ToggleSeqInt(Config.receiverSeq);
                                ack.CopyTo(outDatagram, 1);
                                SendBytes(outDatagram);
                                textBox_Server.AppendText("Sent ACK with seq: " + seqFromDatagram.ToString() + Environment.NewLine);
                                textBox_Client.AppendText("Got message: " + datagramContent + Environment.NewLine);

                            }
                            else if (NotCorrupted && seqFromDatagram != Config.receiverSeq)
                            {
                                byte[] ack = Encoding.ASCII.GetBytes("ACK");
                                byte[] outDatagram = new byte[ack.Length + 1];
                                outDatagram[0] = (byte)seqFromDatagram;
                                //Config.Receiver_Seq = ToggleSeqInt(Config.Receiver_Seq);
                                ack.CopyTo(outDatagram, 1);
                                SendBytes(outDatagram);
                                textBox_Client.AppendText("Dup? " + seqFromDatagram.ToString() + Environment.NewLine);
                                textBox_Server.AppendText("Sent ACK with seq: " + seqFromDatagram.ToString() + Environment.NewLine);
                            }
                            /*
                            else if (NotCorrupted)
                            {
                                textBox_Client.Text += Encoding.ASCII.GetString(Config.prev_sent_msg) + Environment.NewLine;
                                sendBytes(Encoding.ASCII.GetBytes("ACK"));
                            }*/
                            else
                            {
                                textBox_Server.AppendText("Received with bit error, sending NACK" + Environment.NewLine);
                                //sendBytes(Encoding.ASCII.GetBytes("NACK"));
                                byte[] ack = Encoding.ASCII.GetBytes("NACK");
                                byte[] outDatagram = new byte[ack.Length + 1];
                                outDatagram[0] = (byte)Config.receiverSeq;
                                //Config.Receiver_Seq = ToggleSeqInt(Config.Receiver_Seq);
                                ack.CopyTo(outDatagram, 1);
                                SendBytes(outDatagram);
                            }
                            
                        }
                        break;
                    }
                #endregion
                #region Protocol ACK SEQ
                case 21:
                    {
                        uint seqFromDatagram = (uint)localData[0];
                        datagramContent = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(1, localData.Length - 2).ToArray());
                        if(Config.waitingForAck == 1 && NotCorrupted && datagramContent == "ACK" && seqFromDatagram == Config.senderSeq)
                        {
                            
                            textBox_Server.AppendText("Received ACK with seq: " + seqFromDatagram.ToString() + 
                                " while expecting seq: " + Config.senderSeq.ToString() + Environment.NewLine);
                            Config.senderSeq = ToggleSeqInt(Config.senderSeq);
                            if (Config.messageBuffer.Count > 0)
                            {
                                byte[] outDatagram = new byte[Config.messageBuffer.First().Length + 1];
                                outDatagram[0] = (byte)Config.senderSeq;
                                Config.messageBuffer.First().CopyTo(outDatagram, 1);
                                Config.messageBuffer.RemoveAt(0);
                                SendBytes(outDatagram);
                                textBox_Client.AppendText("Sent buffered msg" + Environment.NewLine);
                            }
                            else
                            {
                                Config.waitingForAck = 0;
                                this.Text = "Done";
                                //textBox_Server.AppendText("Receive ACK with seq: " + seqFromPacket.ToString() + 
                                //" while expecting seq: " + Config.Send_Seq.ToString() + Environment.NewLine);
                            }
                            
                        }
                        else if (Config.waitingForAck == 1 && (!NotCorrupted || (datagramContent == "ACK" && seqFromDatagram != Config.senderSeq)))
                        {
                            SendBytes(previouslySentNoChecksum);
                            //textBox_Server.AppendText("Receive ACK with seq: " + seqFromPacket.ToString() + 
                            //" while expecting seq: " + Config.Send_Seq.ToString() + Environment.NewLine);
                            //textBox_Server.Text += "Received with bit error, or with out of order seq. Sent ACK for prev msg" + Environment.NewLine;
                            if (!NotCorrupted)
                            {
                                textBox_Client.AppendText("Corrupted. Resending prev msg." + Environment.NewLine);
                            }else
                            {
                                textBox_Client.AppendText("Received with wrong seq: " + seqFromDatagram.ToString() + 
                                    ". Resending prev msg." + Environment.NewLine);
                            }
                        }
                        else if (Config.waitingForAck == 0 && (!NotCorrupted || seqFromDatagram != Config.receiverSeq))
                        {
                            byte[] ack = Encoding.ASCII.GetBytes("ACK");
                            byte[] outDatagram = new byte[ack.Length + 1];
                            outDatagram[0] = (byte)ToggleSeqInt(Config.receiverSeq);
                            ack.CopyTo(outDatagram, 1);
                            SendBytes(outDatagram);
                            if (!NotCorrupted)
                            {
                                textBox_Server.Text += "Received with bit error. Sent ACK for prev msg" + Environment.NewLine;
                            }
                            else
                            {
                                textBox_Server.Text += "Received with out of order seq. Sent ACK for prev msg" + Environment.NewLine;
                            }
                        }
                        else
                        {
                            byte[] ack = Encoding.ASCII.GetBytes("ACK");
                            byte[] outDatagram = new byte[ack.Length + 1];
                            outDatagram[0] = (byte)Config.receiverSeq;
                            Config.receiverSeq = ToggleSeqInt(Config.receiverSeq);
                            ack.CopyTo(outDatagram, 1);
                            SendBytes(outDatagram);
                            textBox_Server.AppendText("Sent ACK with seq: " + seqFromDatagram.ToString() + Environment.NewLine);
                            textBox_Client.AppendText("Got message: " + datagramContent + Environment.NewLine);
                        }
                        break;
                      }
                #endregion
                #region Protocol NACK-ONLY, requires only bit error channel, no delay
                    /*
                     * This was best way I knew how to implement ACK-free protocol. Possibility of 
                     * NACKs corruption did lead to NACK loop. Searching for other implementations
                     * mentioned use of timers...
                     * Assumes no channel delay and perfect/lab latency
                     */
               case 22:
                    {
                        uint seqFromDatagram = (uint)localData[0];
                        datagramContent = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(1, localData.Length - 2).ToArray());
                        switch (Config.waitingForAck)
                        {
                            case 1:
                                {
                                    if(!NotCorrupted || (datagramContent == "NACK" /*&& seqFromDatagram == seqFromDatagram*/)){
                                        SendBytes(previouslySentNoChecksum);
                                        //sendButton.Enabled = false;
                                        StartTimer();
                                    }
                                    break;
                                }
                            case 0:
                                {
                                    if (NotCorrupted && datagramContent != "NACK")
                                    {
                                        textBox_Client.AppendText("Got message: " + datagramContent + Environment.NewLine);
                                        Config.receiverSeq = ToggleSeqInt(Config.receiverSeq);
                                    }
                                    else if (!NotCorrupted || seqFromDatagram != Config.receiverSeq)
                                    {
                                        byte[] ack = Encoding.ASCII.GetBytes("NACK");
                                        byte[] outDatagram = new byte[ack.Length + 1];
                                        outDatagram[0] = (byte)Config.receiverSeq;
                                        ack.CopyTo(outDatagram, 1);
                                        SendBytes(outDatagram);
                                        if (!NotCorrupted)
                                        {
                                            textBox_Server.Text += "Received with bit error. Sent NACK " 
                                                + Config.receiverSeq.ToString()  + Environment.NewLine;
                                        }
                                        else
                                        {
                                            textBox_Server.Text += "Received with out of order seq. Sent NACK " 
                                                + Config.receiverSeq.ToString() + Environment.NewLine;
                                        }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                #endregion

                #region ACK SEQ For Biterror, delay, dropping channel
                case 30:
                    {
                        uint seqFromDatagram = (uint)localData[0];
                        datagramContent = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(1, localData.Length - 2).ToArray());
                        switch (Config.waitingForAck)
                        {
                            case 1:
                                {
                                    if(NotCorrupted && datagramContent == "ACK" && seqFromDatagram == Config.senderSeq)
                                    {
                                        Config._timer.Stop();
                                        textBox_Server.AppendText("(Case 1)Received ACK with seq: " + seqFromDatagram.ToString() +
                                            " while expecting seq: " + Config.senderSeq.ToString() + PrintDebug());
                                        Config.senderSeq = ToggleSeqInt(Config.senderSeq);
                                        Config.timerSenderSeqValue = (int)Config.senderSeq;
                                        if (Config.messageBuffer.Count > 0)
                                        {
                                            byte[] outDatagram = new byte[Config.messageBuffer.First().Length + 1];
                                            outDatagram[0] = (byte)Config.senderSeq;
                                            Config.messageBuffer.First().CopyTo(outDatagram, 1);
                                            Config.messageBuffer.RemoveAt(0);
                                            SendBytes(outDatagram);
                                            textBox_Client.AppendText("Sent buffered msg" + PrintDebug());
                                            StartTimer();
                                        }
                                        else
                                        {
                                            Config.waitingForAck = 0;
                                            this.Text = "Done";
                                            
                                        }
                                    }
                                    else if (NotCorrupted && datagramContent == "ACK" && seqFromDatagram != Config.senderSeq)
                                    {

                                        textBox_Server.AppendText("(Case1) Seq(datagram - senderSeq) mismatch. "
                                            + Config.receiverSeq.ToString() + PrintDebug());
 
                                    }
                                    /* Below seems unnecessary. It's never processed even in very lossy simulations.
                                     * Scenario where communication channel is simplex helps too (acks duplex)
                                     * 
                                     */
                                    else if (NotCorrupted && datagramContent != "ACK" && seqFromDatagram != Config.senderSeq)
                                    {
                                        textBox_Server.AppendText("(Case1) Seq(datagram - receiverSeq) mismatch. " + PrintDebug());
                                    }
                                    else
                                    {
                                        textBox_Server.AppendText("(Case1) Waiting for timer to elapse. "
                                            + seqFromDatagram.ToString() + PrintDebug());
                                    }
                                    break;
                                }
                             case 0:
                                {
                                    if(!NotCorrupted || datagramContent == "ACK" && seqFromDatagram != Config.receiverSeq)
                                    {
                                        
                                        if (!NotCorrupted || seqFromDatagram != Config.receiverSeq)
                                        {
                                            
                                            if (!NotCorrupted)
                                            {
                                                byte[] ack = Encoding.ASCII.GetBytes("ACK");
                                                byte[] outDatagram = new byte[ack.Length + 1];
                                                outDatagram[0] = (byte)ToggleSeqInt(Config.receiverSeq);
                                                ack.CopyTo(outDatagram, 1);
                                                SendBytes(outDatagram);
                                                textBox_Server.Text += "(Case0) Received with bit error. Sent ACK for prev msg"
                                                    + ToggleSeqInt(Config.receiverSeq).ToString() + PrintDebug();
                                                
                                            }else if (datagramContent == "ACK")
                                            {

                                                textBox_Server.Text += "(Case0) Got ACK while "
                                                    + Config.receiverSeq.ToString() + PrintDebug();
                                            }else
                                            {

                                                textBox_Server.Text += "(Case0) Received with seq bad. "
                                                    + ToggleSeqInt(Config.receiverSeq).ToString() + PrintDebug();
                                                
                                            }
                                        }
                                        else
                                        {
                                            textBox_Server.Text += "Received with out of order ack. Ignored." + PrintDebug();
                                        }
                                    }
                                    else if (datagramContent == "ACK" && seqFromDatagram == Config.receiverSeq)
                                    {
                                        textBox_Server.Text += "(Case0) Got dup ACK? "
                                                    + Config.receiverSeq.ToString() + PrintDebug();
                                    }
                                    else if (datagramContent != "ACK" && seqFromDatagram != Config.receiverSeq)
                                    {
                                        byte[] ack = Encoding.ASCII.GetBytes("ACK");
                                        byte[] outDatagram = new byte[ack.Length + 1];
                                        outDatagram[0] = (byte)ToggleSeqInt(Config.receiverSeq);
                                        ack.CopyTo(outDatagram, 1);
                                        SendBytes(outDatagram);
                                        textBox_Server.AppendText("Got message but seq != receiverSeq" + PrintDebug());
                                    }
                                    else
                                    {
                                        byte[] ack = Encoding.ASCII.GetBytes("ACK");
                                        byte[] outDatagram = new byte[ack.Length + 1];
                                        outDatagram[0] = (byte)Config.receiverSeq;
                                        Config.receiverSeq = ToggleSeqInt(Config.receiverSeq);
                                        ack.CopyTo(outDatagram, 1);
                                        SendBytes(outDatagram);
                                        textBox_Server.AppendText("(Case0) Sent ACK with seq: " + seqFromDatagram.ToString() + PrintDebug());
                                        textBox_Client.AppendText("Got message: " + datagramContent + Environment.NewLine);
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                #endregion
                #region GoBackN
                case 40:
                    {
                        int seqFromDatagram = (int)localData[0];
                        datagramContent = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(1, localData.Length - 2).ToArray());
                        if (NotCorrupted && seqFromDatagram == Config.gbnExpectedSeqNum && datagramContent != "ACK")
                        {
                            /*GBN Receiver FSM*/
                            byte[] ack = Encoding.ASCII.GetBytes("ACK");
                            byte[] outDatagram = new byte[ack.Length + 1];
                            outDatagram[0] = (byte)Config.gbnExpectedSeqNum;
                            Config.gbnExpectedSeqNum = ((Config.gbnExpectedSeqNum + 1) % Config.gbnN);
                            ack.CopyTo(outDatagram, 1);
                            SendBytes(outDatagram);
                            Config.prevSentMsg = outDatagram;
                            textBox_Server.AppendText("(debug1)" + PrintDebugGbN());
                            textBox_Client.AppendText("Got message: " + datagramContent + Environment.NewLine);
                        }
                        else if (NotCorrupted && datagramContent == "ACK")
                        {
                            /*GBN Sender FSM*/
                            if (seqFromDatagram == Config.gbnExpectedSeqNum)
                            {
                                textBox_Server.AppendText("(debug2a)" + PrintDebugGbN());
                            }
                            else
                            {
                                textBox_Server.AppendText("(debug2b)" + PrintDebugGbN());
                            }
                            //textBox_Server.AppendText("(debug2)" + PrintDebugGbN());
                            Config.gbnBase = ((seqFromDatagram + 1) % Config.gbnN);
                            if (Config.gbnBase == Config.gbnNextSeqNum)
                            {
                                //Config._timer.Stop();
                                // calling _timer.Stop(); should only set timer.Enabled to false..
                                // instead it seemed to cause deadlocks O.o
                                Config.waitingForAck = 0;
                                this.Text = "Done.";
                                textBox_Server.AppendText("(debug2.1)" + PrintDebugGbN());
                            }
                            else
                            {

                                //Config._timer.Start();
                                StartTimer();
                                Config.waitingForAck = 1;
                                textBox_Server.AppendText("(debug2.2)" + PrintDebugGbN());
                            }
                        }
                        else if (Config.waitingForAck == 0) {
                            if ((Config._timer.Enabled == false && (seqFromDatagram != Config.gbnExpectedSeqNum)))
                                {
                                    /*GBN Receiver FSM*/
                                    if (Config.prevSentMsg == null)
                                    {
                                        byte[] ack = Encoding.ASCII.GetBytes("ACK");
                                        byte[] outDatagram = new byte[ack.Length + 1];
                                        outDatagram[0] = (byte)0;
                                        ack.CopyTo(outDatagram, 1);

                                        SendBytes(outDatagram);
                                        textBox_Server.AppendText("(debug3a)" + PrintDebugGbN());
                                    }
                                    else
                                    {
                                        SendBytes(Config.prevSentMsg);
                                        textBox_Server.AppendText("(debug3b)" + PrintDebugGbN());
                                       
                                    }
                                    
                                }
                        }
                        else
                        {
                            textBox_Server.AppendText("Received corrupted, discard." + Environment.NewLine);
                            textBox_Server.AppendText(PrintDebugGbN());
                        }
                        break;
                    }
                #endregion
                #region Unreliable protocol, with error checking
                case 0:
                    {
                        textBox_Server.Text += Encoding.ASCII.GetString(new List<byte>(localData)
                            .GetRange(0, localData.Length - 1).ToArray()) + Environment.NewLine;
                        textBox_Server.Text += NotCorrupted.ToString() + Environment.NewLine;
                        break;
                    }
                    #endregion
            }
            
        }
        private string PrintDebug()
        {
            return " [ senderSeq: " + Config.senderSeq.ToString() + ", receiverSeq: " + Config.receiverSeq.ToString() + "]" + Environment.NewLine;
        }

        private string PrintDebugGbN()
        {
            return " [ gbnBase: " + Config.gbnBase.ToString() + ", gbnNextSeqNum: " + Config.gbnNextSeqNum.ToString() 
                + ", gbnExpectedSeqNum: " + Config.gbnExpectedSeqNum.ToString() + "]" + Environment.NewLine + Environment.NewLine;
        }

        private void StartTimer()
        {
            /*
             * Runs in context of UI thread. Look into a new solution for GBN-protocol
             */
            if (Config._timer == null)
            {
                Config._timer = new System.Timers.Timer(2000);
                Config._timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            }
            //Config._timer.Stop();
            Config._timer.AutoReset = false;
            //Config._timer.Interval = 500.0;
            
            Config._timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Config._timer.Stop();
            switch (Config.protocol)
            {
                case 22:
                    {
                        Config.waitingForAck = 0;
                        //sendButton.Enabled = true;
                        
                        break;
                    }
                case 30:
                    {
                        //_timer.Stop();
                        //Crossthread exception, _timer not in UiThread
                        
                        // Timer sending datagrams, while state and ACK&seq have changed caused endless looping, fun times.
                        // senderSeq should be unchanged, until ack for it (senderSeq, ACK) has been received.
                        if (Config.waitingForAck == 1 && Config.senderSeq == Config.timerSenderSeqValue)
                        {
                            SendBytes(Config.prevSentMsg);

                            Config._timer.Start();
                        }
                        break;
                    }
                case 40:
                    {
                        if (Config.waitingForAck == 1 && Config.gbnBase != Config.gbnNextSeqNum)
                        {
                            try
                            {

                                //MessageBox.Show("Timer! Resending packets up to bgnNextSeqNum: " + Config.gbnNextSeqNum.ToString() + "- 1.");
                                //i out of bounds, exception when i = 6 where gbnSendWindow 0..6 = 7. Var i somehow makes it pass
                                // i % windowsize - 1, 
                                this.BeginInvoke(new MethodInvoker(delegate
                                {
                                    textBox_Client.AppendText("Timer is resending following datagrams with seq: ");


                                    int i = Config.gbnBase;
                                    //Debug.Assert(i < gbnSendWindow.Keys.Max(), "Gonna be out of bounds.");

                                    while (i != Config.gbnNextSeqNum)
                                    //foreach (int k in gbnSendWindow.Keys)
                                    {


                                        textBox_Client.AppendText(i.ToString() + ", ");

                                        byte[] value;
                                        bool isOK = gbnSendWindow.TryGetValue(i, out value);
                                        if (isOK)
                                        {
                                            SendBytes(value);
                                        }

                                        i = (((i + 1) % (Config.gbnWindowSize)));

                                    }

                                    textBox_Client.AppendText("!" + Environment.NewLine);
                                }));
                                StartTimer();

                            } catch (IndexOutOfRangeException)
                            {
                                Config._timer.Stop();

                            }
                            
                        }
                        break;
                    }
            }
            
        }

        public void SetStateAndSend(byte[] data)
        {
            switch (Config.protocol)
            {
                #region Alternating bit protocols
                case 0:
                    {
                        SendBytes(data);
                        break;
                    }
                case 20:
                    {
                        if(Config.waitingForAck == 1)
                        {
                            textBox_Client.AppendText("Waiting for ACK, buffering message" + Environment.NewLine);
                            Config.messageBuffer.Add(data);
                        }else
                        {
                            /* For version without duplicate detection
                            Config.state = 1;
                            sendBytes(data);
                            */
                            Config.waitingForAck = 1;
                            this.Text = "Sending..";
                            byte[] dataWithSeq = new byte[data.Length + 1];
                            dataWithSeq[0] = (byte)Config.senderSeq;
                            data.CopyTo(dataWithSeq, 1);
                            SendBytes(dataWithSeq);
                            Config.prevSentMsg = dataWithSeq;
                            textBox_Client.AppendText("Send: " + Encoding.ASCII.GetString(data) + Environment.NewLine);
                        }
                        break;
                    }
                case 21:
                    {
                        if (Config.waitingForAck == 1)
                        {
                            textBox_Client.AppendText("Waiting for ACK, buffering message" + Environment.NewLine);
                            Config.messageBuffer.Add(data);
                        }
                        else
                        {
                            Config.waitingForAck = 1;
                            this.Text = "Sending..";
                            byte[] dataWithSeq = new byte[data.Length + 1];
                            dataWithSeq[0] = (byte)Config.senderSeq;
                            data.CopyTo(dataWithSeq, 1);
                            SendBytes(dataWithSeq);
                            Config.prevSentMsg = dataWithSeq;
                            textBox_Client.AppendText("Send: " + Encoding.ASCII.GetString(data) + Environment.NewLine);
                        }
                        break;
                    }
                case 22:
                    {
                        if (Config.waitingForAck == 1)
                        {
                            textBox_Client.AppendText("Waiting for timer, try later" + Environment.NewLine);
                            //Config.messageBuffer.Add(data);
                        }
                        else
                        {
                            Config.waitingForAck = 1;
                            this.Text = "Sending..";
                            byte[] dataWithSeq = new byte[data.Length + 1];
                            dataWithSeq[0] = (byte)Config.senderSeq;
                            data.CopyTo(dataWithSeq, 1);
                            SendBytes(dataWithSeq);
                            Config.prevSentMsg = dataWithSeq;
                            textBox_Client.AppendText("Send: " + Encoding.ASCII.GetString(data) + Environment.NewLine);
                            StartTimer();
                        }
                        break;
                    }
                case 30:
                    {
                        if (Config.waitingForAck == 1)
                        {
                            textBox_Client.AppendText("Waiting for ACK, buffering message" + Environment.NewLine);
                            Config.messageBuffer.Add(data);
                        }
                        else
                        {
                            Config.waitingForAck = 1;
                            this.Text = "Sending..";
                            byte[] dataWithSeq = new byte[data.Length + 1];
                            dataWithSeq[0] = (byte)Config.senderSeq;
                            data.CopyTo(dataWithSeq, 1);
                            SendBytes(dataWithSeq);
                            Config.prevSentMsg = dataWithSeq;
                            textBox_Client.AppendText("Send: " + Encoding.ASCII.GetString(data) + Environment.NewLine);
                            StartTimer();
                        }
                        break;
                    }
                #endregion
                case 40:
                    {
                        //if(Config.gbnNextSeqNum - Config.gbnBase < Config.gbnWindowSize)
                        /*Sender invariant*/
                        if((Config.gbnBase <= Config.gbnNextSeqNum && Config.gbnNextSeqNum <= Config.gbnWindowSize))
                        {
                            
                            this.Text = "Sending..";
                            byte[] dataWithSeq = new byte[data.Length + 1];
                            dataWithSeq[0] = (byte)Config.gbnNextSeqNum;
                            data.CopyTo(dataWithSeq, 1);
                            gbnSendWindow[Config.gbnNextSeqNum % Config.gbnWindowSize] = dataWithSeq;
                            SendBytes(dataWithSeq);
                            Config.prevSentMsg = dataWithSeq;
                            textBox_Client.AppendText("Send: " + Encoding.ASCII.GetString(data) + Environment.NewLine);
                            Config.gbnNextSeqNum = ((Config.gbnNextSeqNum + 1) % Config.gbnN);
                            //Config.gbnNextSeqNum++;
                            textBox_Client.AppendText("gbnNextSeqNum: " + Config.gbnNextSeqNum.ToString() 
                                + " gbnBase,  gbnN: " + Config.gbnBase.ToString()+ ", " + Config.gbnN.ToString() + " " + Environment.NewLine);
                            //if (Config.gbnBase == Config.gbnNextSeqNum)
                            //{
                                Config.waitingForAck = 1;
                                StartTimer();
                            //}
                        }
                        else
                        {
                            textBox_Client.AppendText("Sending window is full, cannot send" + Environment.NewLine);
                        }
                        break;
                    }
            }
        }

        public void SendBytes(byte[] data)
        {
            try
            {
                /*Trying to use sync Send method instead*/
                byte[] datagram = new byte[data.Length + 1];
                data.CopyTo(datagram, 0);
                datagram[data.Length] = crc8.GetCRC8Chksum(data);
                previouslySentNoChecksum = data;
                //Debug.Assert(socket.Client != null);
                socket.SendAsync(datagram, datagram.Length, "localhost", int.Parse(textBox_serverport.Text));
            }
            catch(Exception e)
            {
                if(e is ObjectDisposedException)
                {
                    MessageBox.Show(e.Source.ToString() + "!!!!" + e.Message.ToString());
                }else { throw; }
            }

        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            rdtProtocol.RdtSend(textBox_msg.Text);
            //SetStateAndSend(Encoding.ASCII.GetBytes(textBox_msg.Text));
            
        }
        /*
        public static void PostMessage(string msg = "")
        {
            
        }
        */
        private void numericUpDown_droppacket_ValueChanged(object sender, EventArgs e)
        {
            Config.packetDropProp = (int)numericUpDown_droppacket.Value;
        }

        private void numericUpDown_delaypacket_ValueChanged(object sender, EventArgs e)
        {
            Config.delayAmount = (int)numericUpDown_delaypacket.Value;
        }

        private void numericUpDown_propbiterror_ValueChanged(object sender, EventArgs e)
        {
            Config.bitErrorProp = (int)numericUpDown_propbiterror.Value;
        }

        private void groupBoxProtocolBox_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton_posneqACK_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 20;
            rdtProtocol = new Rdt20(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_posACK_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 21;
            rdtProtocol = new Rdt21(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_neqACK_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 22;
            rdtProtocol = new Rdt22(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_rdt_v3_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 30;
            rdtProtocol = new Rdt30(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_gobackn_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 40;
            rdtProtocol = new GoBackN(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_selective_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                client = new VirtualSocket(int.Parse(textBox_clientport.Text), int.Parse(textBox_serverport.Text));
                client.OnDeliver += HandlerMessageFromRdt;
                groupBoxProtocolBox.Enabled = true;

                //StartUdpSocketListener();

                //sendButton.Enabled = true;

            } catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string swapstring = textBox_clientport.Text;
            textBox_clientport.Text = textBox_serverport.Text;
            textBox_serverport.Text = swapstring;
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            //client.Close();
            //groupBoxProtocolBox.Enabled = true;
        }
    }
}
