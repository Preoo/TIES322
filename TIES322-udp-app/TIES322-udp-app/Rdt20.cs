﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TIES322_udp_app
{
    public enum STATE
    {
        WaitingCallFromAboveOrBelow,
        WaitingForAck
    }
    internal class Rdt20 : IRdtProtocol
    {
        protected VirtualSocket socket;
        protected RdtUtils rdt;
        protected int state = 0;
        public static List<byte[]> messageBuffer = new List<byte[]>();
        protected int senderSeq = 0;
        protected int receiverSeq = 0;
        protected byte[] previouslySentDatagram;

        public event DeliverData OnDeliver;
        public event HandleDatagramDelegate OnReceive;
        public event HandleDatagramDelegate OnSend;

        public Rdt20(VirtualSocket client)
        {
            socket = client;
            rdt = new RdtUtils();
            socket.Start();

            socket.OnReceive += RdtReceive;
        }
        public Rdt20()
        {

        }
        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);
            string str = rdt.GetDatagramContentString(datagram);
            bool isAck = rdt.IsAck(datagram);
            bool isNack = rdt.IsNack(datagram); //Very naive implementation!
            int seqDatagram = rdt.GetSeqNum(datagram);

            /*We are sender*/
            if(state == (int)STATE.WaitingForAck)
            {

                if (isOk && isAck && seqDatagram == senderSeq)
                {
                    if (messageBuffer.Count > 0)
                    {

                        senderSeq = rdt.incmod(senderSeq, 2); //toggle seq bit by using window of 2. 0->1 || 1->0
                        RaiseOnDeliver(InvokeReason.Debug, "Sending 1st queued message");
                        ToggleState();
                        RdtSend(messageBuffer[0]);
                        messageBuffer.RemoveAt(0);
                    }
                    else
                    {
                        ToggleState();
                        senderSeq = rdt.incmod(senderSeq, 2); //toggle seq bit by using window of 2. 0->1 || 1->0
                        RaiseOnDeliver(InvokeReason.Debug, "Received correct ACK, switching states to #" + state.ToString());
                    }
                }
                else
                {
                    RdtSend(previouslySentDatagram, true);
                    RaiseOnDeliver(InvokeReason.Debug, "Sender got received corrupted or out of order packet. Resending");
                }
            }
            /*We are receiver*/
            else
            {
                if (!isOk)
                {
                    //Corrupt
                    RdtSend(rdt.MakeAck(receiverSeq,true),true);
                    RaiseOnDeliver(InvokeReason.Debug, "Received corrupted, sending NACK");
                }
                else if (isOk && receiverSeq != seqDatagram)
                {
                    //Unexpected seqnum
                    RdtSend(rdt.MakeAck(rdt.incmod(receiverSeq, 2)),true);
                    RaiseOnDeliver(InvokeReason.Debug, "Received out-of-order, sending ACK #" 
                        + receiverSeq.ToString());
                }
                else
                {
                    //Received new message
                    RaiseOnDeliver(InvokeReason.Receiver, str);
                    receiverSeq = rdt.incmod(receiverSeq, 2);
                    RdtSend(rdt.MakeAck(seqDatagram),true);
                }
            }
        }


        public void RdtSend(string message)
        {
            

            RdtSend(Encoding.UTF8.GetBytes(message), false);
            RaiseOnDeliver(InvokeReason.Sender, message);

        }
        protected virtual async void RdtSend(byte[] data, bool sendAsIs = false)
        {
            if (sendAsIs)
            {
                socket.Send(data);
            }
            else
            {
                switch (state)
                {
                    case (int)STATE.WaitingCallFromAboveOrBelow:
                        {
                            byte[] newDatagram = rdt.MakeDatagram(data, senderSeq);
                            //await Task.Run(() => { socket.Send(newDatagram); });
                            socket.Send(newDatagram);
                            previouslySentDatagram = newDatagram;
                            state = (int)STATE.WaitingForAck;
                            break;
                        }
                    case (int)STATE.WaitingForAck:
                        {
                            RaiseOnDeliver(InvokeReason.Error, "Waiting for ACK/NACK, add to queue.");
                            messageBuffer.Add(data);
                            break;
                        }
                }
            }
        }
        protected void ToggleState()
        {
            //state == 1 ? state = 0 : state = 1; Compiler complains?
            if(state == 1)
            {
                state = 0;
            }
            else
            {
                state = 1;
            }
        }
        protected void RaiseOnDeliver(InvokeReason reason, string text)
        {
            OnDeliver?.Invoke(reason, text);
        }
    }
}