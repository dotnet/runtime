// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public class MD4Tests
    {
        // MD4 ("") = 31d6cfe0d16ae931b73c59d7e0c089c0
        [Fact]
        public void RFC1320_Test1() 
        {
            MD4 hash = new MD4();
            byte[] input = new byte[0];
            byte[] expected = { 0x31, 0xd6, 0xcf, 0xe0, 0xd1, 0x6a, 0xe9, 0x31, 0xb7, 0x3c, 0x59, 0xd7, 0xe0, 0xc0, 0x89, 0xc0 };
            Verify(hash, input, expected);
        }

        private void Verify(MD4 hash, byte[] input, byte[] expected) 
        {
            byte[] outputSpan = new byte[expected.Length];
            int bytesWritten = hash.HashData(input, outputSpan);
            Assert.Equal(hash.HashSizeBytes, bytesWritten);
            Assert.Equal(expected, outputSpan);
        }

        // // MD4 ("a") = bde52cb31de33e46245e05fbdbd6fb24
        [Fact]
        public void RFC1320_Test2 () 
        {
            MD4 hash = new MD4();
            byte[] expected = { 0xbd, 0xe5, 0x2c, 0xb3, 0x1d, 0xe3, 0x3e, 0x46, 0x24, 0x5e, 0x05, 0xfb, 0xdb, 0xd6, 0xfb, 0x24 };
            byte[] input = Encoding.Default.GetBytes ("a");
            Verify(hash, input, expected);
        }

        // MD4 ("abc") = a448017aaf21d8525fc10ae87aa6729d
        [Fact]
        public void RFC1320_Test3 () 
        {
            MD4 hash = new MD4();
            byte[] expected = { 0xa4, 0x48, 0x01, 0x7a, 0xaf, 0x21, 0xd8, 0x52, 0x5f, 0xc1, 0x0a, 0xe8, 0x7a, 0xa6, 0x72, 0x9d };
            byte[] input = Encoding.Default.GetBytes ("abc");
            Verify(hash, input, expected);
        }

        // MD4 ("message digest") = d9130a8164549fe818874806e1c7014b
        [Fact]
        public void RFC1320_Test4 () 
        {
            MD4 hash = new MD4();
            byte[] expected = { 0xd9, 0x13, 0x0a, 0x81, 0x64, 0x54, 0x9f, 0xe8, 0x18, 0x87, 0x48, 0x06, 0xe1, 0xc7, 0x01, 0x4b };
            byte[] input = Encoding.Default.GetBytes ("message digest");
            Verify(hash, input, expected);
        }

        // MD4 ("abcdefghijklmnopqrstuvwxyz") = d79e1c308aa5bbcdeea8ed63df412da9
        [Fact]
        public void RFC1320_Test5 () 
        {
            MD4 hash = new MD4();
            byte[] expected = { 0xd7, 0x9e, 0x1c, 0x30, 0x8a, 0xa5, 0xbb, 0xcd, 0xee, 0xa8, 0xed, 0x63, 0xdf, 0x41, 0x2d, 0xa9 };
            byte[] input = Encoding.Default.GetBytes ("abcdefghijklmnopqrstuvwxyz");
            Verify(hash, input, expected);
        }

        // MD4 ("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") =
        //	043f8582f241db351ce627e153e7f0e4
        [Fact]
        public void RFC1320_Test6 () 
        {
            MD4 hash = new MD4();
            byte[] expected = { 0x04, 0x3f, 0x85, 0x82, 0xf2, 0x41, 0xdb, 0x35, 0x1c, 0xe6, 0x27, 0xe1, 0x53, 0xe7, 0xf0, 0xe4 };
            byte[] input = Encoding.Default.GetBytes ("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
            Verify(hash, input, expected);
        }

        // MD4 ("123456789012345678901234567890123456789012345678901234567890123456
        //	78901234567890") = e33b4ddc9c38f2199c3e7b164fcc0536
        [Fact]
        public void RFC1320_Test7 () 
        {
            MD4 hash = new MD4();
            byte[] expected = { 0xe3, 0x3b, 0x4d, 0xdc, 0x9c, 0x38, 0xf2, 0x19, 0x9c, 0x3e, 0x7b, 0x16, 0x4f, 0xcc, 0x05, 0x36 };
            byte[] input = Encoding.Default.GetBytes ("12345678901234567890123456789012345678901234567890123456789012345678901234567890");
            Verify(hash, input, expected);
        }
    }
}
