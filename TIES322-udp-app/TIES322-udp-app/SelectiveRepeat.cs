using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Threading;
using System.Collections;

namespace TIES322_udp_app
{
    internal class SelectiveRepeat : IRdtProtocol
    {
        private RdtUtils rdt;
        private VirtualSocket socket;

        private byte srNextSeqNum = 1;
        private byte srSendBase = 0;
        private byte srRcvBase = 0;
        //For selective repeat N is <= seqnumspace / 2
        //SeqNumSpace.max is 256 for us (using byte as seq) 2^8
        //Max for srN is thus 128, so we'll just use 4 for windowsize
        //This is to check if windowsize check is working at all.
        private byte srWindowSize = 4;

        

        public event DeliverData OnDeliver;
        public event HandleDatagramDelegate OnReceive;
        public event HandleDatagramDelegate OnSend;

        public SelectiveRepeat(VirtualSocket client)
        {

            rdt = new RdtUtils();
            socket = client;
            socket.Start();
            socket.OnReceive += RdtReceive;
        }

        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);

            if (isOk)
            {
                string str = rdt.GetDatagramContentString(datagram);
                bool isAck = rdt.IsAck(datagram);
                int seqNum = rdt.GetSeqNum(datagram);

                if (isAck)
                {
                    
                }
                else
                {
                    if (rdt.IsSeqInRange((byte)seqNum, (byte)(srRcvBase - srWindowSize), (byte)(srRcvBase + srWindowSize - 1)))
                    {
                        RdtSend(rdt.MakeAck(seqNum), true);
                        if (rdt.IsSeqInRange((byte)seqNum, srRcvBase, (byte)(srRcvBase + srWindowSize - 1)))
                        {
                            //TODO: Add to buffer
                            if((byte)seqNum == srRcvBase)
                            {
                                //TODO: DataToUpperlayer
                                srRcvBase = (byte)(srRcvBase + SendReceivedDAtaToUpperLayer());
                            }
                        }
                    }
                }
            }
            else
            {
                //This Kurose implementation will ignore corrupted packets and relies on timeout to recover
                //Alternative is to send SREPEAT-control message to request retransmission for seqnum-1 packet.

                OnDeliver?.Invoke(InvokeReason.Error, "Received corrupted");
            }        
        }

        private byte SendReceivedDAtaToUpperLayer()
        {
            //TODO: Better way
        }

        public void RdtSend(string message)
        {
            RdtSend(Encoding.UTF8.GetBytes(message), false);
            OnDeliver?.Invoke(InvokeReason.Sender, message);
        }

        private async void RdtSend(byte[] data, bool isOldDatagram = false)
        {
            if (isOldDatagram)
            {               
                socket.Send(data);
            }
            else
            {
                byte[] datagram = rdt.MakeDatagram(data, (int)srNextSeqNum);
                if(/*srNextSeqNum is in window*/)
                {
                    //Add to buffer
                    srNextSeqNum = (byte)(srNextSeqNum + 1);
                    OnDeliver?.Invoke(InvokeReason.Debug, "Message sent");
                }
                else
                {
                    //Add to buffer
                    OnDeliver?.Invoke(InvokeReason.Debug, "Message Buffered");
                }
            }

        }
        internal class InternalDatagram
        {
            internal byte[] _data;
            internal bool _isAckd;
            internal bool _isSent;
            public InternalDatagram(byte[] data, bool isAckd = false, bool isSent = false)
            {
                _data = data;
                _isAckd = isAckd;
                _isSent = isSent;
            }
        }
        internal class Bucket
        {
            Dictionary<byte, InternalDatagram> buffer;
            public Bucket()
            {
                buffer = new Dictionary<byte, InternalDatagram>();
            }
            public void AddToBucket(byte[] data, byte seq)
            {
                buffer.Add(seq, new InternalDatagram(data));
            }
            public void RemoveFromBucket(byte seq)
            {
                buffer.Remove(seq);
            }
            public List<byte[]> GetReadyToSend(byte seq_base)
            {
                List<byte[]> tmp = new List<byte[]>();
                tmp.Add(buffer.TakeWhile());
                buffer.First().Value._isAckd = true;
            }
        }

    }
}