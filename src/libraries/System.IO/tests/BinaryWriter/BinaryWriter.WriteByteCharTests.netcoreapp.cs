// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.IO;
using System.Text;

namespace System.IO.Tests
{
    public partial class BinaryWriter_WriteByteCharTests
    {
        [Fact]
        public void BinaryWriter_WriteSpan()
        {
            byte[] bytes = new byte[] { 4, 2, 7, 0xFF };
            char[] chars = new char[] { 'a', '7', Char.MaxValue };
            Span<byte> byteSpan = new Span<byte>(bytes);
            Span<char> charSpan = new Span<char>(chars);

            using (Stream memoryStream = CreateStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.Unicode))
                {
                    binaryWriter.Write(byteSpan);
                    binaryWriter.Write(charSpan);

                    Stream baseStream = binaryWriter.BaseStream;
                    baseStream.Position = 2;

                    Assert.Equal(7, baseStream.ReadByte());
                    Assert.Equal(0xFF, baseStream.ReadByte());

                    char testChar;

                    testChar = BitConverter.ToChar(new byte[] { (byte)baseStream.ReadByte(), (byte)baseStream.ReadByte() }, 0);
                    Assert.Equal('a', testChar);

                    testChar = BitConverter.ToChar(new byte[] { (byte)baseStream.ReadByte(), (byte)baseStream.ReadByte() }, 0);
                    Assert.Equal('7', testChar);

                    testChar = BitConverter.ToChar(new byte[] { (byte)baseStream.ReadByte(), (byte)baseStream.ReadByte() }, 0);
                    Assert.Equal(Char.MaxValue, testChar);
                }
            }
        }
    }
}
