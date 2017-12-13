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
using System.Collections.Specialized;
using System.Data.HashFunction.CRCStandards;

namespace TIES322_udp_app
{
    class VirtualSocket : EventArgs, IDisposable
    {
        int senderSocket, receiverSocket;
        UdpClient socket;
        Random rnd = new Random();
        bool listen = false;
        
        // Specify handlers in classes listening to events from this class
        //public delegate void ReceiveHandler(VirtualSocket vc, )
        public event HandleDatagramDelegate OnSend;
        public event HandleDatagramDelegate OnReceive;
        public event DeliverData OnDeliver;

        public VirtualSocket(int senderSocket, int receiverSocket)
        {
            this.senderSocket = senderSocket;
            this.receiverSocket = receiverSocket;
            

        }
        public async void Send(byte[] datagram)
        {
           OnSend?.Invoke(datagram);
           await socket.SendAsync(datagram, datagram.Length, "localhost", receiverSocket);
           
        }
        /// <summary>
        /// Initialize and start receiveing from UdpClient
        /// </summary>
        public void Start()
        {
            listen = true;
            socket = new UdpClient(senderSocket);
            Receive();
        }
        public async void Receive()
        {
            try
            {
                UdpReceiveResult receivedResults;
                while (listen)
                {
                    receivedResults = await socket.ReceiveAsync();
                    var data = receivedResults.Buffer;
                    if (rnd.Next(100) >= Config.packetDropProp)
                    {
                        if (rnd.Next(101) < Config.bitErrorProp)
                        {
                            data = InsertBitError(data);
                            //textBox_Server.AppendText("[] VS inserted bit error []" + Environment.NewLine);
                            OnDeliver?.Invoke(InvokeReason.Debug, "Receving socket mutilated a datagram");
                        }
                        await Task.Delay(rnd.Next(Config.delayAmount * 1000));

                        OnReceive?.Invoke(data);

                    }
                    else
                    {
                        OnDeliver?.Invoke(InvokeReason.Debug, "Receving socket dropped a datagram");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                /*Short search revealed no easy way to cancel 
                  on going ReceiveAsync other than to catch this exception...*/
                MessageBox.Show("Closed socket.");
            }
        }
        public static byte[] InsertBitError(byte[] input)
        {
            Random rnd = new Random();
            int byteIndex = rnd.Next(input.Count());
            byte mask = (byte)(1 << rnd.Next(8));
            input[byteIndex] ^= mask;

            return input;
        }
        
        public void Stop()
        {
           // try
           // {
           //     listen = false;
           //     socket.Close();
           // }
           // catch (ObjectDisposedException e)
           // {
           //     //Short search revealed no easy way to cancel 
           //     //  on going ReceiveAsync other than to catch this exception...
           //     MessageBox.Show("Closing socket.");
           // }
           // finally
           // {
           //     
           // } 
        }

        public void Dispose()
        {
            ((IDisposable)socket).Dispose();
        }
    }
    public class RdtUtils
    {
        CRC8 crc8;

        public RdtUtils()
        {
            crc8 = new CRC8();
        }
        public byte[] MakeDatagram(byte[] data, int seqNum)
        {
            byte[] datagram = new byte[data.Length + 2];
            datagram[0] = (byte)seqNum;
            data.CopyTo(datagram, 1);
            datagram[datagram.Length - 1] = CalcCRC8(datagram.Take(datagram.Length -1).ToArray());
            return datagram;
        }
        public byte[] MakeAck(int seqNum, bool isNack = false)
        {
            string ackNackString = isNack ? "NACK" : "ACK";
            return MakeDatagram(Encoding.UTF8.GetBytes(ackNackString), seqNum);
        }
        public byte[] MakeResendRequest(int seqNum)
        {
            return MakeDatagram(Encoding.UTF8.GetBytes("SREPEAT"), seqNum);
        }
        public string GetDatagramContentString(byte[] datagram)
        {
            return Encoding.UTF8.GetString(new List<byte>(datagram).GetRange(1, datagram.Length - 2).ToArray());
        }
        public int GetSeqNum(byte[] datagram)
        {
            return (int)datagram[0];
        }
        public byte CalcCRC8(byte[] datagram)
        {
            return GetCRC8Chksum(datagram);
        }
        public bool IsOk(byte[] datagram)
        {
            return CheckCRC8Chksum(datagram);
        }
        //I started this with meaning to use example protocol, hence ack packets are
        //identified by string comparison... How I regret that choice. Results in 
        //annoying code and corner cases where message "ACK" will break this prog :/
        public bool IsAck(byte[] datagram)
        {
            return GetDatagramContentString(datagram) == "ACK" ? true : false;
        }
        public bool IsNack(byte[] datagram)
        {
            //return !IsAck(datagram);
            return GetDatagramContentString(datagram) == "NACK" ? true : false;
        }
        public bool IsRepeatRequest(byte[] datagram)
        {
            return GetDatagramContentString(datagram) == "SREPEAT" ? true : false;
        }
        private byte GetCRC8Chksum(byte[] input)
        {
            return crc8.ComputeHash(input)[0];
        }
        private bool CheckCRC8Chksum(byte[] input)
        {
            
            byte[] tmp = input.Take(input.Length - 1).ToArray();
            byte chksum = input.Last();
            byte tmp2 = GetCRC8Chksum(tmp);
            return (tmp2 == chksum);
        }
        public int Incmod(int from, int window)
        {
            return from + 1 == window ? 0 : from + 1;
        }
        public int Decmod(int from, int window)
        {
            return from - 1 < 0 ? window - 1 : from - 1;
        }
        public int Distmod(int from, int to, int window)
        {
            /* |-----window-----|
             *  -->to    from---
             *     from----->to
             *  Return distance traveled clockwise with respet to mod N
             *  mostly used to see if senders window is full of unack'd
             *  datagrams
             */ 
            int n = 0;
            while(from != to)
            {
                from = Incmod(from, window);
                n++;
            }
            return n;
        }
        /// <summary>
        /// Dumb brute force method to check if seq is in range. 
        /// </summary>
        /// <param name="seq">Seq number to check</param>
        /// <param name="range_begin"></param>
        /// <param name="range_end"></param>
        /// <returns></returns>
        public bool IsSeqInRange(byte seq, byte range_begin, byte range_end)
        {
            while(range_begin != range_end)
            {
                if (range_begin == seq) return true;
                range_begin = (byte)(range_begin + 1);
            }
            return false;
        }
    }
}
