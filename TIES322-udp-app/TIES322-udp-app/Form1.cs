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
using System.Windows.Forms;

namespace TIES322_udp_app
{
    public partial class Form1 : Form
    {
        System.Net.Sockets.UdpClient socket;
        public byte[] previouslySentNoChecksum;
        crc8lib crc8;
        public Form1()
        {
            InitializeComponent();
            crc8 = new crc8lib();
            try
            {
                /*
                 * Tried to simulate sender and receiver in one program, printing to separte boxes,
                 * got too many problems trying to get data back to UI thread. Current impl. is a temp - permanent hack.
                 * Similary used UdpClient as udpsocket for sanity, while calling reliability layer from below.
                 */
                //client = new VirtualSocket(int.Parse(textBox_clientport.Text), int.Parse(textBox_serverport.Text));

                //UDPServerListener();
                //client = new GotPackets(true);
                //client.UDPServerListener(TaskScheduler.FromCurrentSynchronizationContext());


                //server = new VirtualSocket(int.Parse(textBox_serverport.Text), int.Parse(textBox_clientport.Text), true);
                
                //Thread clientthread = new Thread(() => new VirtualSocket(8081,8080, true));
                //clientthread.Start();
                //Thread serverthread = new Thread(() => new VirtualSocket(8080,8081,true));
                //serverthread.Start();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private void UDPSocketListener()
        {
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(async () =>
            {
                using (var udpServer = new System.Net.Sockets.UdpClient(int.Parse(textBox_clientport.Text)))
                {
                    Random rnd = new Random();
                    socket = udpServer;
                    while (true)
                    {
                        //IPEndPoint object will allow us to read datagrams sent from any source.
                        var receivedResults = await udpServer.ReceiveAsync();
                        var data = receivedResults.Buffer;
                        if (rnd.Next(100) >= Config.packetDropProp)
                        {
                            if (rnd.Next(101) < Config.bitErrorProp)
                            {
                                data = checksum.InsertBitError(data);
                                //    MessageBox.Show("BitError");
                            }
                            await Task.Delay(rnd.Next(Config.delayAmount));
                            //Thread.Sleep(rnd.Next(Config.delayAmount));

                            //new Thread(() => new GotPackets(data)).Start();
                            //Thread.Sleep(300);
                            //MessageBox.Show(Encoding.ASCII.GetString(new List<byte>(data).GetRange(0, data.Length - 1).ToArray()));

                            //textBox_Server.Text += Encoding.ASCII.GetString(new List<byte>(data).GetRange(0, data.Length - 1).ToArray()) + Environment.NewLine;
                            //var from = receivedResults.RemoteEndPoint;
                            RdtLayer(data);
                            
                        }
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.None, uiContext);
        }
        private int ToggleSeqInt(int value)
        {
            return value == 1 ? 0 : 1;
        }
        public void RdtLayer(byte[] newPacket)
        {
            var localData = newPacket;
            String datagram = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(0, localData.Length - 1).ToArray());
            //Boolean NotCorrupted = checksum.CheckChecksum(localData);
            Boolean NotCorrupted = crc8.CheckCRC8Chksum(localData);
            
            switch (Config.protocol)
            {
                #region Protocol ACK & NACK 
                case 20:
                    {
                        /*Without duplicate detection, messages ca nbe sent only one way :/ */
                        if (Config.state == 1)
                        {
                            if(datagram == "ACK" && NotCorrupted)
                            {
                                if(Config.messageBuffer.Count > 0)
                                {
                                    sendBytes(Config.messageBuffer.First());
                                    
                                    textBox_Client.AppendText("Sent buffered msg: "+ Encoding.ASCII.GetString(previouslySentNoChecksum) + Environment.NewLine);
                                    Config.messageBuffer.RemoveAt(0);
                                }
                                else
                                {
                                    Config.state = 0;
                                    textBox_Server.AppendText("Receive ACK" + Environment.NewLine);
                                }
                                
                            }
                            else if (datagram == "NACK" && NotCorrupted)
                            {
                                textBox_Client.Text += "resent: " + Encoding.ASCII.GetString(new List<byte>(previouslySentNoChecksum).GetRange(0, previouslySentNoChecksum.Length - 1).ToArray()) + Environment.NewLine;
                                sendBytes(previouslySentNoChecksum);
                                
                            }
                            else
                            {
                                textBox_Client.Text += "resent: " + Encoding.ASCII.GetString(new List<byte>(previouslySentNoChecksum).GetRange(0, previouslySentNoChecksum.Length - 1).ToArray()) + Environment.NewLine;
                                sendBytes(previouslySentNoChecksum);
                                
                            }
                            
                        }
                        else if (Config.state == 0)
                        {
                            if(NotCorrupted && datagram == "ACK")
                            { 
                            }
                            else if (NotCorrupted && datagram == "NACK")
                            {
                                textBox_Client.Text += "resent: " + Encoding.ASCII.GetString(new List<byte>(previouslySentNoChecksum).GetRange(0, previouslySentNoChecksum.Length - 1).ToArray()) + Environment.NewLine;
                                sendBytes(previouslySentNoChecksum);
                            }
                            else if (NotCorrupted)
                            {
                                textBox_Server.Text += Encoding.ASCII.GetString(new List<byte>(localData).GetRange(0, localData.Length - 1).ToArray()) + Environment.NewLine;
                                sendBytes(Encoding.ASCII.GetBytes("ACK"));
                            }
                            else
                            {
                                textBox_Server.Text += "Received with biterror, sending NACK" + Environment.NewLine;
                                sendBytes(Encoding.ASCII.GetBytes("NACK"));
                            }
                            
                        }
                        break;
                    }
                #endregion
                #region Protocol ACK SEQ
                case 21:
                    {
                        int seqFromPacket = (int)localData[0];
                        datagram = Encoding.ASCII.GetString(new List<byte>(localData).GetRange(1, localData.Length - 2).ToArray());
                        if(Config.state == 1 && NotCorrupted && datagram == "ACK" && seqFromPacket == Config.Send_Seq)
                        {
                            //Config.state = 0;
                            textBox_Server.AppendText("Receive ACK with seq: " + seqFromPacket.ToString() + " while expecting seq: " + Config.Send_Seq.ToString() + Environment.NewLine);
                            if (Config.messageBuffer.Count > 0)
                            {
                                byte[] packet = new byte[Config.messageBuffer.First().Length + 1];
                                packet[0] = (byte)Config.Send_Seq;
                                Config.messageBuffer.First().CopyTo(packet, 1);
                                Config.messageBuffer.RemoveAt(0);
                                sendBytes(packet);
                                textBox_Client.AppendText("Sent buffered msg: " + Encoding.ASCII.GetString(previouslySentNoChecksum) + Environment.NewLine);
                            }
                            else
                            {
                                Config.state = 0;
                                //textBox_Server.AppendText("Receive ACK with seq: " + seqFromPacket.ToString() + " while expecting seq: "+ Config.Send_Seq.ToString() + Environment.NewLine);
                            }
                            Config.Send_Seq = ToggleSeqInt(Config.Send_Seq);
                        }
                        else if (Config.state == 1 && (!NotCorrupted || (datagram == "ACK" && seqFromPacket != Config.Send_Seq)))
                        {
                            sendBytes(previouslySentNoChecksum);
                            //textBox_Server.AppendText("Receive ACK with seq: " + seqFromPacket.ToString() + " while expecting seq: " + Config.Send_Seq.ToString() + Environment.NewLine);
                            //textBox_Server.Text += "Received with bit error, or with out of order seq. Sent ACK for prev msg" + Environment.NewLine;
                            textBox_Client.AppendText("Corrupted or wrong ACK. Resending prev msg." + Environment.NewLine);
                        }
                        else if (Config.state == 0 && (!NotCorrupted || seqFromPacket != Config.Receive_Seq))
                        {
                            byte[] ack = Encoding.ASCII.GetBytes("ACK");
                            byte[] packet = new byte[ack.Length + 1];
                            packet[0] = (byte)ToggleSeqInt(Config.Receive_Seq);
                            ack.CopyTo(packet, 1);
                            sendBytes(packet);
                            textBox_Server.Text += "Received with bit error, or with out of order seq. Sent ACK for prev msg" + Environment.NewLine;
                        }
                        else
                        {
                            byte[] ack = Encoding.ASCII.GetBytes("ACK");
                            byte[] packet = new byte[ack.Length + 1];
                            packet[0] = (byte)Config.Receive_Seq;
                            Config.Receive_Seq = ToggleSeqInt(Config.Receive_Seq);
                            ack.CopyTo(packet, 1);
                            sendBytes(packet);
                            textBox_Server.AppendText("Sent ACK with seq: " + seqFromPacket.ToString() + Environment.NewLine);
                            textBox_Client.AppendText("Got message: " + datagram + Environment.NewLine);
                        }
                        break;
                      }
                #endregion
                case 0:
                    {
                        textBox_Server.Text += Encoding.ASCII.GetString(new List<byte>(localData).GetRange(0, localData.Length - 1).ToArray()) + Environment.NewLine;
                        textBox_Server.Text += NotCorrupted.ToString() + Environment.NewLine;
                        break;
                    }

            }

        }
        public void setStateAndSend(byte[] data)
        {
            switch (Config.protocol)
            {
                case 0:
                    {
                        sendBytes(data);
                        break;
                    }
                case 20:
                    {
                        if(Config.state == 1)
                        {
                            textBox_Client.AppendText("Waiting for ACK, buffering message" + Environment.NewLine);
                            Config.messageBuffer.Add(data);
                        }else
                        {
                            Config.state = 1;
                            sendBytes(data);
                            

                        }
                        break;
                    }
                case 21:
                    {
                        if (Config.state == 1)
                        {
                            textBox_Client.AppendText("Waiting for ACK, buffering message" + Environment.NewLine);
                            Config.messageBuffer.Add(data);
                        }
                        else
                        {
                            Config.state = 1;
                            byte[] datawithseq = new byte[data.Length + 1];
                            datawithseq[0] = (byte)Config.Send_Seq;
                            data.CopyTo(datawithseq, 1);
                            sendBytes(datawithseq);


                        }
                        break;
                    }
            }
        }

        public void sendBytes(byte[] data)
        {
            byte[] tmp = new byte[data.Length + 1];
            data.CopyTo(tmp, 0);
            //tmp[tmp.GetUpperBound(0)] = checksum.GetChecksum(data);
            //tmp[data.Length] = checksum.GetChecksum(data);
            tmp[data.Length] = crc8.GetCRC8Chksum(data);
            previouslySentNoChecksum = data;
            socket.SendAsync(tmp, tmp.Length, "localhost", int.Parse(textBox_serverport.Text));
            /*begin  Test
            crc8class test = new crc8class();
            test.GetCRC8Chksum(data);
            //MessageBox.Show(Encoding.ASCII.GetString(data));
            //socket.Send(tmp);
            /*end test*/
        }
        public void sendBytes(byte[] data, System.Net.Sockets.UdpClient socket, IPEndPoint remote)
        {
            byte[] tmp = new byte[data.Length + 1];
            data.CopyTo(tmp, 0);
            tmp[tmp.GetUpperBound(0)] = checksum.GetChecksum(data);
            using (var udpClient = new System.Net.Sockets.UdpClient(int.Parse(textBox_clientport.Text)))
            {
                udpClient.SendAsync(tmp, tmp.Length, "localhost", int.Parse(textBox_serverport.Text));

            }
            //MessageBox.Show(Encoding.ASCII.GetString(data));
            //socket.Send(tmp);
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            //sendBytes(Encoding.ASCII.GetBytes(textBox_msg.Text));
            setStateAndSend(Encoding.ASCII.GetBytes(textBox_msg.Text));
            //client.sendBytes(Encoding.ASCII.GetBytes(textBox_msg.Text));
            textBox_Client.Text += "sent: " + textBox_msg.Text + "\r\n";
        }
        private void updateUI(string txt)
        {
            textBox_Server.Text += txt;
            textBox_Server.Text += "\r\n";
        }
        public static void PostMessage(string msg = "")
        {
            
        }

        private void numericUpDown_droppacket_ValueChanged(object sender, EventArgs e)
        {
            Config.packetDropProp = (int)numericUpDown_droppacket.Value;
        }

        private void numericUpDown_delaypacket_ValueChanged(object sender, EventArgs e)
        {
            Config.delayAmount = (int)numericUpDown_delaypacket.Value;
        }

        private void numericUpDown_propbiterror_ValueChanged(object sender, EventArgs e)
        {
            Config.bitErrorProp = (int)numericUpDown_propbiterror.Value;
        }

        private void groupBoxProtocolBox_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton_posneqACK_CheckedChanged(object sender, EventArgs e)
        {
            Config.protocol = 20;
        }

        private void radioButton_posACK_CheckedChanged(object sender, EventArgs e)
        {
            Config.protocol = 21;
        }

        private void radioButton_neqACK_CheckedChanged(object sender, EventArgs e)
        {
            Config.protocol = 22;
        }

        private void radioButton_rdt_v3_CheckedChanged(object sender, EventArgs e)
        {
            Config.protocol = 30;
        }

        private void radioButton_gobackn_CheckedChanged(object sender, EventArgs e)
        {
            Config.protocol = 40;
        }

        private void radioButton_selective_CheckedChanged(object sender, EventArgs e)
        {
            Config.protocol = 50;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                UDPSocketListener();
                sendButton.Enabled = true;

            } catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
