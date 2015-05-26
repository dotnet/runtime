using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

using TestUtilities;

using Xunit;

namespace System.Reflection.Metadata.Tests.PortableExecutable
{
    public class PEBinaryReaderTests
    {
        [Fact]
        public void ReadNullPaddedUTF8RemovesNullPadding()
        {
            var headerBytes = new byte[PEFileConstants.SizeofSectionName];
            headerBytes[0] = 80;
            headerBytes[1] = 80;
            headerBytes[2] = 80;

            var stream = new MemoryStream(headerBytes);
            stream.Position = 0;

            var reader = new PEBinaryReader(stream, headerBytes.Length);
            var text = reader.ReadNullPaddedUTF8(PEFileConstants.SizeofSectionName);

            AssertEx.AreEqual(3, text.Length, "PEBinaryReader.ReadNullPaddedUTF8 did not truncate null padding");
            AssertEx.AreEqual("PPP", text);
        }

        [Fact]
        public void ReadNullPaddedUTF8WorksWithNoNullPadding()
        {
            var headerBytes = Encoding.UTF8.GetBytes(".abcdefg");
            var stream = new MemoryStream(headerBytes);
            stream.Position = 0;

            var reader = new PEBinaryReader(stream, headerBytes.Length);
            var text = reader.ReadNullPaddedUTF8(PEFileConstants.SizeofSectionName);

            AssertEx.AreEqual(".abcdefg", text, "PEBinaryReader.ReadNullPaddedUTF8 erroneously truncated a section name");
        }
    }
}
