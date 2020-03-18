using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.HashFunction.CRCStandards;
/*The MIT License (MIT)

Copyright (c) 2014 Data.HashFunction Developers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/
namespace TIES322_udp_app
{/*
    static class checksum
    {
        
        public static byte[] InsertBitError(byte[] input)
        {
            Random rnd = new Random();
            int byteIndex = rnd.Next(input.Count());
            byte mask = (byte)(1 << rnd.Next(8));
            input[byteIndex] ^= mask;
            
            return input;
        }
    }
    public class crc8lib
    {
        

        CRC8 crc8instance;

        //byte[] checksum;
        public crc8lib() {
            
            //crc8instance = new CRC8();
            
        }
        public byte GetCRC8Chksum(byte[] input)
        {
            //crc8instance = new CRC8();
            return crc8instance.ComputeHash(input).First();
        }
        public bool CheckCRC8Chksum(byte[] input)
        {
            //crc8instance = new CRC8();
            byte[] tmp = input.Take(input.Length - 1).ToArray();
            byte chksum = input.Last();
            byte tmp2 = GetCRC8Chksum(tmp);
            return (tmp2 == chksum);
        }
    }*/
}
