using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIES322_udp_app
{
    internal class InternalDatagram
    {
        internal byte[] _datagram;
        internal bool _isAckd;
        internal bool _isSent;
        internal DateTime _timestamp;

        public InternalDatagram(byte[] datagram, bool isAckd = false, bool isSent = false)
        {
            _datagram = datagram;
            _isAckd = isAckd;
            _isSent = isSent;
        }
    }
    /*Quick hack, properties to be considered*/
    internal class Bucket
    {
        Dictionary<byte, InternalDatagram> buffer;
        public Bucket()
        {
            buffer = new Dictionary<byte, InternalDatagram>();
        }
        public void AddToBucket(byte[] datagram, byte seq)
        {
            buffer.Add(seq, new InternalDatagram(datagram));
        }
        public void RemoveFromBucket(byte seq)
        {
            buffer.Remove(seq);
        }
        public void MarkAsAcked(byte seq)
        {
            try
            {
                if (!buffer[seq]._isAckd && buffer[seq]._isSent)
                {
                    buffer[seq]._isAckd = true;
                }
            }
            catch
            {
                throw new ApplicationException("Tried to access buffer with invalid index");
            }
        }
        public void MarkAsSent(byte seq)
        {
            try
            {
                if (!buffer[seq]._isSent)
                {
                    buffer[seq]._isSent = true;
                }
            }
            catch
            {
                throw new ApplicationException("Tried to access buffer with invalid index");
            }
        }
        public byte[] GetDatagramBySeq(byte key)
        {
            return buffer[key]._datagram;
        }
        /// <summary>
        /// Get valid sequence from buffer as return value, clears those from buffer
        /// </summary>
        /// <param name="seq_base"></param>
        /// <returns></returns>
        public List<byte[]> GetValidSequenceFromBuffer(byte seq_base)
        {

            List<byte[]> tmp = new List<byte[]>();
            for (byte i = seq_base; buffer.ContainsKey(i); i++)
            {
                tmp.Add(buffer[i]._datagram);
                buffer.Remove(i);
            }
            return tmp;

        }
        public bool ContainsKey(byte key)
        {
            return buffer.ContainsKey(key);
        }
        public bool IsSent(byte key)
        {
            InternalDatagram value;
            if(buffer.TryGetValue(key, out value))
            {
                return value._isSent;
            }
            return false;
        }
        public bool IsAcked(byte key)
        {
            InternalDatagram value;
            if (buffer.TryGetValue(key, out value))
            {
                return value._isAckd;
            }
            return false;
        }
        /// <summary>
        /// Gets timestamp as datetime for datagram per seqnum.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DateTime GetTimestamp(byte key)
        {
            if (ContainsKey(key)) return buffer[key]._timestamp;
            throw new ApplicationException("Tried to get timedate from buffer with invalid key");
        }
        /// <summary>
        /// Sets timestamp of datagram in buffer with seqnum to current time.
        /// </summary>
        /// <param name="key"></param>
        public void SetTimestamp(byte key)
        {
            if (ContainsKey(key))
            {
                buffer[key]._timestamp = DateTime.Now;
            }
            else
            {
                throw new ApplicationException("Tried to set timedate from buffer with invalid key");
            }
        }
        public bool HasPending()
        {
            return buffer.Count > 0 ? true : false;
        }
    }
}
