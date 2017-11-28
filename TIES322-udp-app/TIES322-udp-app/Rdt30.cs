using System;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace TIES322_udp_app
{
    internal class Rdt30 : Rdt20, IRdtProtocol
    {
        private static System.Windows.Forms.Timer timer;
        TimerDatagram scheduledDatagram;
        
        public Rdt30(VirtualSocket client):base()
        {
            socket = client;
            rdt = new RdtUtils();
            socket.Start();

            timer = new System.Windows.Forms.Timer();
            timer.Enabled = false;

            socket.OnReceive += RdtReceive;
        }

        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);
            string str = rdt.GetDatagramContentString(datagram);
            bool isAck = rdt.IsAck(datagram);
            bool isNack = rdt.IsNack(datagram);
            int seqDatagram = rdt.GetSeqNum(datagram);
            
            if (state == (int)STATE.WaitingForAck)
            {
                if (isOk && isAck)
                {
                    if(seqDatagram == senderSeq)
                    {
                        senderSeq = rdt.Incmod(senderSeq, flipBit);
                        StopTimer();
                        if (messageBuffer.Count > 0)
                        {
                            RaiseOnDeliver(InvokeReason.Debug, "Sending queued message");
                            ToggleState();
                            RdtSend(messageBuffer[0]);
                            messageBuffer.RemoveAt(0);
                            
                        }
                        else
                        {
                            ToggleState();
                            
                            RaiseOnDeliver(InvokeReason.Debug, "Received correct ACK, switching states to #" + state.ToString());
                        }
                    }
                    else
                    {
                        RaiseOnDeliver(InvokeReason.Debug, "Received out of order ACK # " 
                            + seqDatagram.ToString() + " Expected # " + senderSeq.ToString());
                    }
                    //Let timer expire, no need to do anything.  
                }
                else
                {
                    RaiseOnDeliver(InvokeReason.Debug, "Received corrupted");
                }
            }
            //State == 0  TEST::set receiverSeq to senderSeq to see if 1 seq is enough
            
            else
            {
                if (!isAck && isOk)
                {
                    if (seqDatagram == receiverSeq) //test if single seq is enough
                    {
                        RaiseOnDeliver(InvokeReason.Receiver, str);
                        RaiseOnDeliver(InvokeReason.Debug, "Sent ACK #" + receiverSeq.ToString());
                        RdtSend(rdt.MakeAck(receiverSeq), true);
                        receiverSeq = rdt.Incmod(receiverSeq, flipBit);
                        
                    }
                    else
                    {
                        RaiseOnDeliver(InvokeReason.Debug, "Duplicate pkt. Sending ACK #"
                            + rdt.Incmod(receiverSeq, flipBit).ToString());
                        RdtSend(rdt.MakeAck(rdt.Incmod(receiverSeq, flipBit)), true);
                    }
                    
                }
                else
                {
                    if (!isOk)
                    {
                        RaiseOnDeliver(InvokeReason.Debug, "Received corrupted.");
                    }
                    else
                    {
                        RaiseOnDeliver(InvokeReason.Debug, "Received unexpected ACK, ignoring.");
                    }
                }
            }
        }

        
        protected override void RdtSend(byte[] data, bool sendAsIs = false)
        {
            if (!sendAsIs)
            {
                StartTimer(data);
            }
            base.RdtSend(data, sendAsIs);
        }

        private void StartTimer(byte[] data = null)
        {
            if (!timer.Enabled)
            {
                timer.Interval = 1000;

                RaiseOnDeliver(InvokeReason.Debug, "Starting timer code");

                if (data != null)
                {
                    scheduledDatagram.timerSeq = senderSeq;
                    scheduledDatagram.data = data;
                }

                timer.Tick -= FireTimer;
                timer.Tick += FireTimer;
                timer.Start();
            }
        }

        private void FireTimer(object sender, EventArgs e)
        {
            //RaiseOnDeliver(InvokeReason.Debug, "Timer elapsed, resending");
            if(state == (int)STATE.WaitingForAck && scheduledDatagram.timerSeq == senderSeq)
            {
                RdtSend(previouslySentDatagram, true);
                RaiseOnDeliver(InvokeReason.Debug, "Timer timeout event");

            }
            else
            {
                timer.Enabled = false;
                StopTimer();
            }
        }

        private void StopTimer()
        {
            timer.Stop();
            RaiseOnDeliver(InvokeReason.Debug, "Stopping timer code");
            //timer.Tick -= FireTimer;      
            scheduledDatagram.timerSeq = -1;
            scheduledDatagram.data = null;
            //timer.Dispose();
        }

        public struct TimerDatagram
        {
            public int timerSeq;
            public byte[] data;

            public TimerDatagram(int s = -1, byte[] d = null)
            {
                timerSeq = s;
                data = d;
            }
        }
    }
}