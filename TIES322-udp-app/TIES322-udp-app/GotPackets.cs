using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TIES322_udp_app
{
    class GotPackets
    {
        private Boolean isServer;
        public GotPackets(Boolean bInitAsServer) {
            isServer = bInitAsServer;
        }
        public GotPackets(byte[] packetdata)
        {
            Boolean isDataReceivedValid = checksum.CheckChecksum(packetdata);
            if (isDataReceivedValid)
            {
                MessageBox.Show(Encoding.ASCII.GetString(new List<byte>(packetdata).GetRange(0, packetdata.Length - 1).ToArray()));
            }
            else if (!isDataReceivedValid)
            {
                MessageBox.Show("CRC mismatch");

            }

        }

        public void inPacket(byte[] packetdata)
        {
            Boolean isDataReceivedValid = checksum.CheckChecksum(packetdata);
            if (isDataReceivedValid) {
                MessageBox.Show(Encoding.ASCII.GetString(new List<byte>(packetdata).GetRange(0, packetdata.Length -1).ToArray()));
            }else if (!isDataReceivedValid)
            {
                MessageBox.Show("CRC mismatch");
                
            }
            
        }

        
        
    }
}
