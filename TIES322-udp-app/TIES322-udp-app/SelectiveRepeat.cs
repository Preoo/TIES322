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
        private CancellationTokenSource cts;
        private Task check;
        private RdtUtils rdt;
        private VirtualSocket socket;
        private Bucket rcvBucket;
        private Bucket sendBucket;

        private byte srNextSeqNum = 0;
        private byte srSendBase = 0;
        private byte srRcvBase = 0;
        //For selective repeat N is <= seqnumspace / 2
        //SeqNumSpace.max is 256 for us (using byte as seq) 2^8
        //Max for srN is thus 128, so we'll just use 4 for windowsize
        //This is to check if windowsize check is working at all.
        //Window size here set as is, we substract 1 in methods.
        private byte srWindowSize = 4;
        private int srTimeoutInterval = 2000;        

        public event DeliverData OnDeliver;
        public event HandleDatagramDelegate OnReceive;
        public event HandleDatagramDelegate OnSend;

        public SelectiveRepeat(VirtualSocket client)
        {
            cts = new CancellationTokenSource();           
            rcvBucket = new Bucket();
            sendBucket = new Bucket();
            rdt = new RdtUtils();
            socket = client;
            socket.Start();
            socket.OnReceive += RdtReceive;
            StartDumbTimer(cts.Token);
        }

        private async void StartDumbTimer(CancellationToken token)
        {
            if (check == null) await SRTimer(token);
        }

        private void CancelTimer()
        {
            cts.Cancel();
        }

        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);

            if (isOk)
            {
                string str = rdt.GetDatagramContentString(datagram);
                bool isAck = rdt.IsAck(datagram);
                byte seqNum = (byte)rdt.GetSeqNum(datagram);

                if (isAck)
                {
                    //Mark as ack'd
                    //If seq == sendBase => move window by:
                    //Go through buffer and if packets are in window and pkt.sent = false => send it and toggle bool
                    
                    if (rdt.IsSeqInRange(seqNum, srSendBase, (byte)(srSendBase + srWindowSize - 1)))
                    {
                        sendBucket.MarkAsAcked(seqNum);
                        OnDeliver?.Invoke(InvokeReason.Debug, "Received ACK: " + seqNum.ToString() 
                            + PrintSRDebugInfo());
                        if(seqNum == srSendBase)
                        {
                            //int i = 0;
                            //While packet is sent -> remove it from sendBucket, increment counter, add counter to srSendBase
                            while(sendBucket.IsSent(srSendBase) && sendBucket.IsAcked(srSendBase))
                            {
                                sendBucket.RemoveFromBucket(srSendBase);
                                srSendBase++;                               
                            }
                            //While packet in sendBucket is in new window && hasn't been sent, send it and toggle it's state
                            //E.g for each srSendBase to srSendBase + N -1, if is in sendBucket & is not sent, send it now
                            //As dictionary is not ordered, we loop over range srSendBase -> srSendBase + N - 1. Stopping early if key isn't in bucket.
                            for(byte i = srSendBase; rdt.IsSeqInRange(i, srSendBase, (byte)(srSendBase + srWindowSize -1)); i++)
                            {
                                if (!sendBucket.ContainsKey(i)) break;
                                socket.Send(sendBucket.GetDatagramBySeq(i));
                                sendBucket.MarkAsSent(i);
                                //srNextSeqNum++;
                            }
                            OnDeliver.Invoke(InvokeReason.Debug, "Sender window moved. " + PrintSRDebugInfo());

                        }
                    }
                }
                else
                {
                    if (rdt.IsSeqInRange(seqNum, (byte)(srRcvBase - srWindowSize), (byte)(srRcvBase + srWindowSize - 1)))
                    {
                        RdtSend(rdt.MakeAck(seqNum), true);
                        OnDeliver?.Invoke(InvokeReason.Debug, "Received Pkt: " + seqNum.ToString()
                            + PrintSRDebugInfo());
                        if (rdt.IsSeqInRange(seqNum, srRcvBase, (byte)(srRcvBase + srWindowSize - 1)))
                        {
                            if (!rcvBucket.ContainsKey(seqNum))
                            {
                                rcvBucket.AddToBucket(datagram, seqNum);
                            }
                            else
                            {
                                OnDeliver.Invoke(InvokeReason.Debug, "Received duplicate: " + seqNum.ToString());
                            }
                            
                            if(seqNum == srRcvBase)
                            {
                                /*Pass a Action delegate for handling method. Method chain removes from buffer,
                                handles packets, returns count of handled. Naming scheme I use is bad, I know :(*/
                                srRcvBase = (byte)(srRcvBase + ProcessValidBufferedSequence(srRcvBase, 
                                    DeliverDataSequenceToUpperLayer));
                                OnDeliver.Invoke(InvokeReason.Debug, "Receiver window moved. " + PrintSRDebugInfo());
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

        private string PrintSRDebugInfo()
        {
            return " srSendBase = " + srSendBase.ToString() + " srRcvBase = " 
                + srRcvBase.ToString() + " srNextSeqNum = " + srNextSeqNum.ToString() + Environment.NewLine;
        }

        private void DeliverDataSequenceToUpperLayer(List<byte[]> items)
        {
            items.ForEach((i) => {
                OnDeliver?.Invoke(InvokeReason.Receiver, rdt.GetDatagramContentString(i));
            });
        }

        private byte ProcessValidBufferedSequence(byte srStartSeq, Action<List<byte[]>> f = null)
        {
            //Check for valid sequence
            var datagramToProcess = rcvBucket.GetValidSequenceFromBuffer(srStartSeq);

            if(datagramToProcess.Count > byte.MaxValue) {
                throw new InvalidOperationException("Returned list of valid packets is larger than sequence number space.");
            }
            /*Call delegate method IIF f is not null*/
            f?.Invoke(datagramToProcess);

            return (byte)datagramToProcess.Count;
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
                byte[] datagram = rdt.MakeDatagram(data, srNextSeqNum);
                sendBucket.AddToBucket(datagram, srNextSeqNum);
                if (rdt.IsSeqInRange(srNextSeqNum, srSendBase, (byte)(srSendBase+srWindowSize - 1)))
                {                   
                    //Send via socket
                    socket.Send(datagram);
                    sendBucket.MarkAsSent(srNextSeqNum);
                    //start timer for this packet or set timestamp to simulate via single timer

                    sendBucket.SetTimestamp(srNextSeqNum);
                    
                    
                    OnDeliver?.Invoke(InvokeReason.Debug, "Message sent" + PrintSRDebugInfo());
                }
                else
                {                  
                    OnDeliver?.Invoke(InvokeReason.Debug, "Message Buffered" + PrintSRDebugInfo());
                }
                srNextSeqNum++;
            }
        }

        /*Lightweight single timer to simulate multiple logical timers with timestamps and intervals*/
        private async Task SRTimer(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
                //Below is probably problematic when seqnum space overflows...
                if (sendBucket.HasPending())
                {
                    for (byte i = srSendBase; i < (byte)(srSendBase + srWindowSize); i++)
                    {
                        if (sendBucket.ContainsKey(i) && sendBucket.IsSent(i) && !sendBucket.IsAcked(i))
                        {
                            if (DateTime.Now - sendBucket.GetTimestamp(i) > TimeSpan.FromMilliseconds(srTimeoutInterval))
                            {
                                RdtSend(sendBucket.GetDatagramBySeq(i), true);
                                sendBucket.SetTimestamp(i);
                                OnDeliver?.Invoke(InvokeReason.Debug, "Resent timedout pkt# " + i.ToString());
                            }
                        }
                        else
                        {
                            break;
                        }
                    } 
                }
            }
        }
    }
}