using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TIES322_udp_app
{

    internal class Rdt21 : Rdt20, IRdtProtocol
    {
        
        public Rdt21(VirtualSocket client):base()
        {
            socket = client;
            rdt = new RdtUtils();
            socket.Start();

            socket.OnReceive += RdtReceive;
        }
        
        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);
            string str = rdt.GetDatagramContentString(datagram);
            bool isAck = rdt.IsAck(datagram);
            bool isNack = rdt.IsNack(datagram);
            int seqDatagram = rdt.GetSeqNum(datagram);

            /*We are sender*/
            if (state == (int)STATE.WaitingForAck)
            {

                if (isOk && isAck && seqDatagram == senderSeq)
                {
                    if (messageBuffer.Count > 0)
                    {

                        senderSeq = rdt.Incmod(senderSeq, flipBit); //toggle seq bit by using window of 2. 0->1 || 1->0
                        RaiseOnDeliver(InvokeReason.Debug, "Sending 1st queued message");
                        ToggleState();
                        RdtSend(messageBuffer[0]);
                        messageBuffer.RemoveAt(0);
                    }
                    else
                    {
                        ToggleState();
                        senderSeq = rdt.Incmod(senderSeq, flipBit); //toggle seq bit by using window of 2. 0->1 || 1->0
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
                if (!isOk || receiverSeq != seqDatagram)
                {
                    //Unexpected seqnum
                    RdtSend(rdt.MakeAck(rdt.Incmod(receiverSeq, flipBit)), true);
                    RaiseOnDeliver(InvokeReason.Debug, "Received out-of-order or corrupted, sending prev ACK");
                }
                else
                {
                    //Received new message
                    RaiseOnDeliver(InvokeReason.Receiver, str);
                    receiverSeq = rdt.Incmod(receiverSeq, flipBit);
                    RdtSend(rdt.MakeAck(seqDatagram), true);
                }
            }
        }        
    }
}