using System;
using System.ComponentModel;
using System.Timers;

namespace TIES322_udp_app
{
    internal class Rdt22 : Rdt20, IRdtProtocol
    {
        private enum STATE22
        {           
            wait0s,           
            wait1s,          
            error0,           
            error1,
            wait0r,
            wait1r
        }
        private static System.Windows.Forms.Timer timer;
        protected int seq;
        //protected byte[][] buffer;
        public Rdt22(VirtualSocket client):base()
        {
            socket = client;
            rdt = new RdtUtils();
            socket.Start();
            state = (int)STATE22.wait0r;
            socket.OnReceive += RdtReceive;
            previouslySentDatagram = null;
            seq = 0;
            //buffer = new byte[2][];
            timer = new System.Windows.Forms.Timer();
            timer.Enabled = false;
        }

        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);
            string str = rdt.GetDatagramContentString(datagram);
            bool isAck = rdt.IsAck(datagram);
            bool isNack = rdt.IsNack(datagram);
            int seqDatagram = rdt.GetSeqNum(datagram);

            if (isOk) RaiseOnDeliver(InvokeReason.Debug, "Received seq# " + seqDatagram.ToString());
            if (!isOk) RaiseOnDeliver(InvokeReason.Debug, "Received corrupted");
            //First version, cleanup later (if at all)
            switch (state)
            {
                case (int)STATE22.wait0s:
                    if(isOk && !isNack && seqDatagram == 0)
                    {
                        //proper message
                        state = (int)STATE22.wait1r;
                        RaiseOnDeliver(InvokeReason.Receiver, str);
                        seq = 1;
                        RaiseOnDeliver(InvokeReason.Debug, "wait0s --> wait1r | seq: 1");
                    }
                    if(isNack || !isOk)
                    {
                        state = (int)STATE22.error0;
                        RdtSend(previouslySentDatagram, true);
                        StartErrorRecovTimer();
                        RaiseOnDeliver(InvokeReason.Debug, "wait0s --> error0 | seq: 0");
                    }
                    break;
                case (int)STATE22.wait1s:
                    if (isOk && !isNack && seqDatagram == 1)
                    {
                        //proper message
                        state = (int)STATE22.wait0r;
                        RaiseOnDeliver(InvokeReason.Receiver, str);
                        seq = 0;
                        RaiseOnDeliver(InvokeReason.Debug, "wait1s --> wait0r | seq: 0");
                    }
                    if (isNack || !isOk)
                    {
                        state = (int)STATE22.error1;
                        RdtSend(previouslySentDatagram, true);
                        StartErrorRecovTimer();
                        RaiseOnDeliver(InvokeReason.Debug, "wait1s --> error1 | seq: 1");
                    }
                    break;
                case (int)STATE22.wait0r:
                    if (isOk && !isNack && seqDatagram == 0)
                    {
                        //proper message
                        state = (int)STATE22.wait1r;
                        RaiseOnDeliver(InvokeReason.Receiver, str);
                        seq = 1;
                        RaiseOnDeliver(InvokeReason.Debug, "wait0r --> wait1r | seq: 1");
                    }
                    if ((isNack && seqDatagram == 1) || !isOk)
                    {
                        RdtSend(rdt.MakeAck(seq, true), true);
                        RaiseOnDeliver(InvokeReason.Debug, "wait0r --> wait0r | seq: 0");
                    }
                    break;
                case (int)STATE22.wait1r:
                    if (isOk && !isNack && seqDatagram == 1)
                    {
                        //proper message
                        state = (int)STATE22.wait0r;
                        RaiseOnDeliver(InvokeReason.Receiver, str);
                        seq = 0;
                        RaiseOnDeliver(InvokeReason.Debug, "wait1r --> wait0r | seq: 0");
                    }
                    if ((isNack && seqDatagram == 0) || !isOk)
                    {
                        RdtSend(rdt.MakeAck(seq, true), true);
                        RaiseOnDeliver(InvokeReason.Debug, "wait1r --> wait1r | seq: 1");
                    }
                    break;
                case (int)STATE22.error0:
                    if(!isOk || isNack)
                    {
                        RdtSend(previouslySentDatagram, true);
                        StartErrorRecovTimer();
                        RaiseOnDeliver(InvokeReason.Debug, "error0 --> error0");
                    }
                    break;
                case (int)STATE22.error1:
                    if (!isOk || isNack)
                    {
                        RdtSend(previouslySentDatagram, true);
                        StartErrorRecovTimer();
                        RaiseOnDeliver(InvokeReason.Debug, "error1 --> error1");
                    }
                    break;
                default:
                    RaiseOnDeliver(InvokeReason.Error, "Fatal state error: "
                        + Enum.GetName(typeof(STATE22), state));
                    break;
            }
            /*if (isOk)
            {
                if (isNack)
                {
                    
                }
                
                else
                {
                    seq = rdt.Incmod(seq, flipBit);                 
                    RaiseOnDeliver(InvokeReason.Receiver, str);
                }
            }
            else
            {
                //Start timer to prevent sending while in error recovery
                StartErrorRecovTimer();
                RdtSend(rdt.MakeAck(seq, true), true);
            }*/
            
            
        }

        private void StartErrorRecovTimer()
        {
            
            timer.Stop();
            timer.Interval = 200;
            
            timer.Tick -= FireTimer;
            timer.Tick += FireTimer;
            timer.Start();
        }

        private void FireTimer(object sender, EventArgs e)
        {
            switch (state)
            {
                case (int)STATE22.error0:
                    state = (int)STATE22.wait0s;
                    RaiseOnDeliver(InvokeReason.Debug, "Timer fired in error0 --> wait0s");
                    break;
                case (int)STATE22.error1:
                    state = (int)STATE22.wait1s;
                    RaiseOnDeliver(InvokeReason.Debug, "Timer fired in error1 --> wait1s");
                    break;
                default:
                    RaiseOnDeliver(InvokeReason.Error, "Unexpected state while timer fired: " 
                        + Enum.GetName(typeof(STATE22),state));
                    break;
            }
            timer.Stop();
            timer.Tick -= FireTimer;
        }

        protected override void RdtSend(byte[] data, bool sendAsIs = false)
        {
            if (sendAsIs)
            {
                base.RdtSend(data, true);
            }
            else
            {
                byte[] datagram;
                switch (state)
                {
                    case (int)STATE22.wait0s:
                    case (int)STATE22.wait0r:
                        datagram = rdt.MakeDatagram(data, seq);
                        base.RdtSend(datagram, true);
                        previouslySentDatagram = datagram;
                        seq = 1;
                        state = (int)STATE22.wait1s;
                        RaiseOnDeliver(InvokeReason.Debug, "wait0s|wait0r --> wait1s | seq: 1");
                        break;
                    case (int)STATE22.wait1s:
                    case (int)STATE22.wait1r:
                        datagram = rdt.MakeDatagram(data, seq);
                        base.RdtSend(datagram, true);
                        previouslySentDatagram = datagram;
                        seq = 0;
                        state = (int)STATE22.wait0s;
                        RaiseOnDeliver(InvokeReason.Debug, "wait1s|wait1r --> wait0s | seq: 0");
                        break;
                    case (int)STATE22.error0:
                    case (int)STATE22.error1:
                        RaiseOnDeliver(InvokeReason.Error, "Previous send operation has errored, wait while we recover.");
                        //TODO: Automatic queue and resending queued items.
                        break;
                }
            }
        }
    }
}