// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEBinaryReaderTests
    {
        [Fact]
        public void ReadNullPaddedUTF8RemovesNullPadding()
        {
            var headerBytes = new byte[SectionHeader.NameSize];
            headerBytes[0] = 80;
            headerBytes[1] = 80;
            headerBytes[2] = 80;

            var stream = new MemoryStream(headerBytes);
            stream.Position = 0;

            var reader = new PEBinaryReader(stream, headerBytes.Length);
            var text = reader.ReadNullPaddedUTF8(SectionHeader.NameSize);

            Assert.Equal(3, text.Length);
            Assert.Equal("PPP", text);
        }

        [Fact]
        public void ReadNullPaddedUTF8WorksWithNoNullPadding()
        {
            byte[] headerBytes = ".abcdefg"u8.ToArray();
            var stream = new MemoryStream(headerBytes);
            stream.Position = 0;

            var reader = new PEBinaryReader(stream, headerBytes.Length);
            var text = reader.ReadNullPaddedUTF8(SectionHeader.NameSize);

            Assert.Equal(".abcdefg", text);
        }
    }
}
