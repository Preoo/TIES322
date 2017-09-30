using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace TIES322_udp_app
{
    class VirtualSocket
    {
        public int _sendPort;
        public int _receivePort;
        public Boolean IsListening = false;
        IPEndPoint _serverEndPoint;
        IPEndPoint _remoteEndPoint;
        //UdpClient udp = new UdpClient("localhost", 0);
        Socket socket;
        EndPoint Remote;
        //public byte[] lastincoming = new byte[1024];
        int recv;
        Random rnd = new Random();
        public VirtualSocket(int port, int port2, Boolean isServer = false)
        {
            try
            {
                _serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                _receivePort = _serverEndPoint.Port;
                _sendPort = port2;
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(_serverEndPoint);

                _remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port2);
                Remote = (EndPoint)(_remoteEndPoint);
                if (isServer)
                {
                    //ReceiveLoop();
                    new Thread(() => ReceiveLoop()).Start();
                }
            }catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        public void StartListening()
        {
            Thread listingthread = new Thread(() => ReceiveLoop());
            listingthread.Start();
            IsListening = true;
        }

        public void sendBytes(byte[] data)
        {
            byte[] tmp = new byte[data.Length + 1];
            data.CopyTo(tmp, 0);
            tmp[tmp.GetUpperBound(0)] = checksum.GetChecksum(data);
            socket.SendTo(tmp, Remote);
            //MessageBox.Show(Encoding.ASCII.GetString(data));
            //socket.Send(tmp);
        }

        void ReceiveLoop()
        {
            byte[] data = new byte[1024];
            while (true) {
                recv = socket.ReceiveFrom(data, ref Remote);

                Array.Resize(ref data, recv);
                if (rnd.Next(100) >= Config.packetDropProp)
                {
                    if(rnd.Next(101) < Config.bitErrorProp)
                    {
                        data = checksum.InsertBitError(data);
                  //    MessageBox.Show("BitError");
                    }
                    Thread.Sleep(rnd.Next(Config.delayAmount));
                    new Thread(() => new GotPackets(data)).Start();
                    Thread.Sleep(300);
                }
            }

        }

    }
}
