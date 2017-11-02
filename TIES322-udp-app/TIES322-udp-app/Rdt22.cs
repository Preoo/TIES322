using System;
using System.Timers;

namespace TIES322_udp_app
{
    internal class Rdt22 : Rdt20, IRdtProtocol
    {
        protected static Timer timer;

        public Rdt22(VirtualSocket client):base()
        {
            socket = client;
            rdt = new RdtUtils();
            socket.Start();

            socket.OnReceive += RdtReceive;

            timer = new Timer();
        }

        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);
            string str = rdt.GetDatagramContentString(datagram);
            bool isAck = rdt.IsAck(datagram);
            bool isNack = rdt.IsNack(datagram);
            int seqDatagram = rdt.GetSeqNum(datagram);
            // TODO: Figure out a better way to do this! Perhaps with seq numbering?
            switch (state)
            {
                case 0:
                    {
                        if(!isOk || isNack)
                        {
                            RdtSend(rdt.MakeAck(receiverSeq, true), true);
                            RaiseOnDeliver(InvokeReason.Debug, "Received corrupted, sending NACK");
                        }
                        else
                        {
                            RaiseOnDeliver(InvokeReason.Receiver, str);
                        }
                        break;
                    }
                case 1:
                    {
                        if(!isOk || isNack)
                        {
                            RdtSend(previouslySentDatagram, true);
                            StartTimer();
                        }
                        break;
                    }
            }
        }
        protected override void RdtSend(byte[] data, bool sendAsIs = false)
        {
            if (state == 0)
            {
                base.RdtSend(data, sendAsIs);
                state = (int)STATE.WaitingForAck;
                StartTimer();
            }
            else
            {
                RaiseOnDeliver(InvokeReason.Error, "In Error state, try again later");
            }
            //StartTimer();
            /* Given delay, lossless channel:
             * We can assume almost instant delivery time
             * meaning, short timer interval is a possiblity here.
             * 
             * Only way I can imagine how to implement NACK-only with alternating bit protocol
             * is to have protocol retransmit on NACK or corrupt... This approach makes sweeping assumption
             * on traffic channel properties, such as instant roundtrip delay and biterrors only errormode.
             * Otherwise throttling on sender is required to be atleast RTT
             */
        }

        protected virtual void StartTimer()
        {
            state = (int)STATE.WaitingForAck;
            
            timer.AutoReset = false;
            timer.Interval = 500.0;
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            state = (int)STATE.WaitingCallFromAboveOrBelow;
            timer.Stop();
            //RaiseOnDeliver(InvokeReason.Debug, "Timer");
        }
    }
}