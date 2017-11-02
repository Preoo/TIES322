﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Threading;

namespace TIES322_udp_app
{
    class GoBackN : IRdtProtocol
    {
        RdtUtils rdt;
        VirtualSocket socket;
        CancellationTokenSource cts;

        int gbnNextSeqNum = 1;
        int gbnBase = 1;
        int gbnExpectedSeqNum = 1;
        int gbnWindowSize = 4; //Max Windowsize(N) = 2^(seqnumberspceinbits) - 1
        
        int gbnTimeoutIntervalMs = 2000;
        private byte[] gbnLatestAck;
        List<byte[]> gbnSendQueue;
        byte[][] gbnSendArray;
        Dictionary<int, byte[]> gbnSendDictionary;

        public event HandleDatagramDelegate OnReceive;
        public event HandleDatagramDelegate OnSend;
        public event DeliverData OnDeliver;

        //Window could be a List where new packets are .Add'd (appended) and packets sent are popped from front () or a hash table... 
        //GBN with windowsize = 1 behaves like RDT3.0, lets make use of that.
        public GoBackN(VirtualSocket socket)
        {
            rdt = new RdtUtils();
            this.socket = socket;
            socket.Start();

            socket.OnReceive += RdtReceive;

            gbnSendDictionary = new Dictionary<int, byte[]>();
            gbnSendArray = new byte[gbnWindowSize - 1][];
            gbnSendQueue = new List<byte[]>();
            gbnLatestAck = rdt.MakeAck(0);
        //cts = new CancellationTokenSource();
        //to cancel a task based timer... 
        //init cts on timer start
        //
    }

        private void RdtReceive(byte[] datagram)
        {
            bool isOk = rdt.IsOk(datagram);
            string str = rdt.GetDatagramContentString(datagram);
            bool isAck = rdt.IsAck(datagram);
            int seqNum = rdt.GetSeqNum(datagram);
            /*TEST, REMOVE THIS*/
            
            //OnDeliver?.Invoke(InvokeReason.Receiver, string.Join(",", datagram));

            if (isOk)
            {
                if (isAck)
                {
                    try
                    {
                        if (gbnBase != gbnNextSeqNum)
                        {
                            gbnSendDictionary.Remove(seqNum);
                            gbnBase = seqNum + 1;
                            gbnBase = gbnBase % gbnWindowSize;
                            //timer is running so stop/cancel
                            
                            /*All standing packets are ack'd*/
                            if (gbnBase == gbnNextSeqNum)
                            {
                                OnDeliver?.Invoke(InvokeReason.Debug, "Has sent all pending messages successfully" + PrintGbnDebug());
                                if (cts != null)
                                {
                                    cts.Cancel();
                                    
                                }
                            }
                            else
                            {
                                OnDeliver?.Invoke(InvokeReason.Debug, "2" + PrintGbnDebug());
                                //start new timer
                                /*CancellationTokenSource aCTS = new CancellationTokenSource();
                                cts = aCTS;
                                await Timertimeout(aCTS.Token);
                                if (cts == aCTS)
                                {
                                    cts = null;
                                }*/
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                }
                else
                {
                    /*not ack, not corrupt*/
                    if(seqNum == gbnExpectedSeqNum)
                    {
                        gbnLatestAck = rdt.MakeAck(gbnExpectedSeqNum);
                        RdtSend(gbnLatestAck, true);
                        gbnExpectedSeqNum++;
                        gbnExpectedSeqNum = gbnExpectedSeqNum % gbnWindowSize;
                        //If there is a subscriber attached to OnDeliver (e.g. UI), invoke event.
                        OnDeliver?.Invoke(InvokeReason.Receiver, str);
                        OnDeliver?.Invoke(InvokeReason.Debug, "3" + PrintGbnDebug());
                    }
                    else
                    {
                        /*As in not waiting for any acks, ie is receiver*/
                        if (gbnSendDictionary.Count == 0)
                        {
                            OnDeliver?.Invoke(InvokeReason.Debug, "Unexpected ACK SEQ (lost packets or duplicates?), resending ACK for datagram: " 
                                + rdt.GetSeqNum(gbnLatestAck).ToString());
                            RdtSend(gbnLatestAck, true);
                        }
                    }
                }
            }
            else
            {
                /*corrupt
                Assuming we are receiver e.g. no outstanding acks
                */
                if(gbnSendDictionary.Count == 0)
                {
                    OnDeliver?.Invoke(InvokeReason.Debug, "Corrupted datagram. Resending ACK with seq: "
                                + rdt.GetSeqNum(gbnLatestAck).ToString());
                    RdtSend(gbnLatestAck, true);
                }
            }
            
        }
        /*Returns true if datagram was sent e.g window had spare room*/
        public void RdtSend(string message)
        {
            /*TEST, REMOVE THIS
            var tmp = Encoding.UTF8.GetBytes(message);
            var tmp2 = rdt.MakeDatagram(tmp, gbnNextSeqNum);
            OnDeliver?.Invoke(InvokeReason.Sender, string.Join(",", tmp2)); */

            RdtSend(Encoding.UTF8.GetBytes(message), false);
            OnDeliver?.Invoke(InvokeReason.Sender, message); //TODO: Check sending window size before calling this to avoid confusion

        }
        private async void RdtSend(byte[] data, bool isOldDatagram = false)
        {
            if (isOldDatagram)
            {
                /*data has been sent before so it has seq nums etc*/
                socket.Send(data);
                
            }
            else
            {
                /*data is just new content as byte[] so new datagram is needed*/
                /*Lets try simpler way of getting available window for next packets*/
                if (rdt.distmod(gbnBase, gbnNextSeqNum, gbnWindowSize) < gbnWindowSize - 1)
                {
                    try
                    {
                        OnDeliver?.Invoke(InvokeReason.Debug, "4a" + PrintGbnDebug());

                        byte[] newDatagram = rdt.MakeDatagram(data, gbnNextSeqNum);
                        gbnSendDictionary[gbnNextSeqNum] = newDatagram;

                        socket.Send(gbnSendDictionary[gbnNextSeqNum]);
                        //socket.Send(newDatagram);

                        gbnNextSeqNum = gbnNextSeqNum + 1;
                        gbnNextSeqNum = gbnNextSeqNum % gbnWindowSize;

                        OnDeliver?.Invoke(InvokeReason.Debug, "4b" + PrintGbnDebug());

                        if (cts != null )
                        {
                            cts.Cancel();
                        }
                        /*All standing packets are ack'd*/
                        if (gbnBase == gbnNextSeqNum)
                        {
                            //OnDeliver?.Invoke(InvokeReason.Debug, "Sending window is empty.");
                        }
                        else
                        {
                            //start new timer
                            CancellationTokenSource aCTS = new CancellationTokenSource();
                            cts = aCTS;
                            await Timertimeout(aCTS.Token);
                            /*
                            if (cts == aCTS)
                            {
                                cts = null;
                            }
                            */
                        }
                    }
                    catch (OperationCanceledException) { }
                }
                else //send window is full
                {
                    OnDeliver?.Invoke(InvokeReason.Error, "Sending window is full, try again later.");
                }
            }
            
            
        }
        private async Task Timertimeout(CancellationToken token)
        {
            await Task.Delay(gbnTimeoutIntervalMs, token);
            if (!token.IsCancellationRequested)
            {
                //resend pending packets/unack'd
                OnDeliver?.Invoke(InvokeReason.Debug, "Timer timedout");
                int i = gbnBase;
                while (i != gbnNextSeqNum)
                {
                    RdtSend(gbnSendDictionary[i], true);
                    i = (i + 1) % gbnWindowSize;

                }
                
                await Timertimeout(token);
                
            }
            else
            {
                OnDeliver?.Invoke(InvokeReason.Debug, "Timer is canceled");
            }
        }
        /*
        private void TimerReEntry()
        {
            CancellationTokenSource aCTS = new CancellationTokenSource();
            cts = aCTS;
            await Timertimeout(aCTS.Token);
        }    
        */
        public void CancelPending()
        {
            cts.Cancel();
            if (cts.IsCancellationRequested)
            {
                OnDeliver?.Invoke(InvokeReason.Sender, "[Canceled pending transmission(s)]");
            }
        }
        private string PrintGbnDebug()
        {
            return "gbnBase: " + gbnBase.ToString() + ", gbnNextSeq: " + gbnNextSeqNum 
                + ", gbnExpected: " + gbnExpectedSeqNum + ", gbnLatestAck: " + rdt.GetSeqNum(gbnLatestAck);
        }
    }
}