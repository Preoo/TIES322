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

namespace TIES322_udp_app
{
    public delegate void HandleDatagramDelegate(byte[] datagram);
    public delegate void DeliverData(InvokeReason reason, string message);

    public enum InvokeReason
    {
        Sender,
        Receiver,
        Error,
        Debug
    }

    public partial class Form1 : Form
    {       
        VirtualSocket client;
        IRdtProtocol rdtProtocol;
                
        public Form1()
        {
            InitializeComponent();
            
            try
            {
                /* 
                 * Frame is |seq<1byte>|data<x bytes of input.string>|chksum<1byte>, used example chat program to test versions < rdt3
                 * Tried to simulate sender and receiver in one program, printing to separte boxes,
                 * got too many problems trying to get data/messages back to UI thread. Current impl. is a temp -> permanent hack.
                 * Similary used UdpClient as udpsocket for sanity, while calling reliability layer from below.
                 * Opted to keep using udpclient for async benefits... and to follow best practice 
                 * https://docs.microsoft.com/en-us/dotnet/framework/network-programming/best-practices-for-system-net-classes
                 */

            }
            catch (Exception e)
            {
                MessageBox.Show("ASD " + e.InnerException);
            }
        }

        private void HandlerMessageFromRdt(InvokeReason reason, string message)
        {
            switch (reason)
            {
                case InvokeReason.Sender:
                    {
                        textBox_Client.AppendText(">>> " + message + Environment.NewLine);
                        break;
                    }
                case InvokeReason.Receiver:
                    {
                        textBox_Client.AppendText("<<< " + message + Environment.NewLine);
                        break;
                    }
                case InvokeReason.Error:
                    {
                        textBox_Server.AppendText("[ERROR] " + message + Environment.NewLine);
                        break;
                    }
                case InvokeReason.Debug:
                    {
                        textBox_Server.AppendText("[DEBUG] " + message + Environment.NewLine);
                        break;
                    }
            }
        }
        
        private void sendButton_Click(object sender, EventArgs e)
        {
            rdtProtocol.RdtSend(textBox_msg.Text);
            textBox_msg.Clear();                     
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
            //Config.protocol = 20;
            rdtProtocol = new Rdt20(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_posACK_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 21;
            rdtProtocol = new Rdt21(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_neqACK_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 22;
            rdtProtocol = new Rdt22(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_rdt_v3_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 30;
            rdtProtocol = new Rdt30(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_gobackn_CheckedChanged(object sender, EventArgs e)
        {
            //Config.protocol = 40;
            rdtProtocol = new GoBackN(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void radioButton_selective_CheckedChanged(object sender, EventArgs e)
        {
            rdtProtocol = new SelectiveRepeat(client);

            rdtProtocol.OnDeliver += HandlerMessageFromRdt;

            sendButton.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                client = new VirtualSocket(int.Parse(textBox_clientport.Text), int.Parse(textBox_serverport.Text));
                client.OnDeliver += HandlerMessageFromRdt;
                groupBoxProtocolBox.Enabled = true;

                //StartUdpSocketListener();

                //sendButton.Enabled = true;

            } catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string swapstring = textBox_clientport.Text;
            textBox_clientport.Text = textBox_serverport.Text;
            textBox_serverport.Text = swapstring;
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            //client.Close();
            //groupBoxProtocolBox.Enabled = true;
        }
    }
}
