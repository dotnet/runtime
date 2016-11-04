// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEHeadersTests
    {
        [Fact]
        public void Sizes()
        {
            Assert.Equal(20, CoffHeader.Size);
            Assert.Equal(224, PEHeader.Size(is32Bit: true));
            Assert.Equal(240, PEHeader.Size(is32Bit: false));
            Assert.Equal(8, SectionHeader.NameSize);
            Assert.Equal(40, SectionHeader.Size);

            Assert.Equal(128 + 4 + 20 + 224, new PEHeaderBuilder(Machine.I386).ComputeSizeOfPEHeaders(0));
            Assert.Equal(128 + 4 + 20 + 224 + 16, new PEHeaderBuilder(Machine.Amd64).ComputeSizeOfPEHeaders(0));
            Assert.Equal(128 + 4 + 20 + 224 + 16 + 40 * 1, new PEHeaderBuilder(Machine.Amd64).ComputeSizeOfPEHeaders(1));
            Assert.Equal(128 + 4 + 20 + 224 + 16 + 40 * 2, new PEHeaderBuilder(Machine.Amd64).ComputeSizeOfPEHeaders(2));
        }

        [Fact]
        public void Ctor_Streams()
        {
            Assert.Throws<ArgumentException>(() => new PEHeaders(new CustomAccessMemoryStream(canRead: false, canSeek: false, canWrite: false)));
            Assert.Throws<ArgumentException>(() => new PEHeaders(new CustomAccessMemoryStream(canRead: true, canSeek: false, canWrite: false)));

            var s = new CustomAccessMemoryStream(canRead: true, canSeek: true, canWrite: false, buffer: Misc.Members);

            s.Position = 0;
            new PEHeaders(s);

            s.Position = 0;
            new PEHeaders(s, 0);

            s.Position = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaders(s, -1));
            Assert.Equal(0, s.Position);

            Assert.Throws<BadImageFormatException>(() => new PEHeaders(s, 1));
            Assert.Equal(0, s.Position);
        }

        [Fact]
        public void FromEmptyStream()
        {
            Assert.Throws<BadImageFormatException>(() => new PEHeaders(new MemoryStream()));
        }

        [Fact]
        public void Sections()
        {
            var peHeaders = new PEReader(SynthesizedPeImages.Image1).PEHeaders;
            AssertEx.Equal(new[]
            {
                ".s1 offset=0x200 rva=0x200 size=512",
                ".s2 offset=0x400 rva=0x400 size=512",
                ".s3 offset=0x600 rva=0x600 size=512"
            }, peHeaders.SectionHeaders.Select(h => $"{h.Name} offset=0x{h.PointerToRawData:x3} rva=0x{h.VirtualAddress:x3} size={h.SizeOfRawData}"));
        }

        [Fact]
        public void GetContainingSectionIndex()
        {
            var peHeaders = new PEReader(SynthesizedPeImages.Image1).PEHeaders;

            Assert.Equal(-1, peHeaders.GetContainingSectionIndex(0));
            Assert.Equal(-1, peHeaders.GetContainingSectionIndex(0x200 - 1));
            Assert.Equal(0, peHeaders.GetContainingSectionIndex(0x200));
            Assert.Equal(1, peHeaders.GetContainingSectionIndex(0x400));
            Assert.Equal(2, peHeaders.GetContainingSectionIndex(0x600));
            Assert.Equal(2, peHeaders.GetContainingSectionIndex(0x600 + 9));
            Assert.Equal(-1, peHeaders.GetContainingSectionIndex(0x600 + 10));
        }

        [Fact]
        public void TryGetDirectoryOffset()
        {
            var peHeaders = new PEReader(SynthesizedPeImages.Image1).PEHeaders;
            var dir = peHeaders.PEHeader.CopyrightTableDirectory;
            
            Assert.Equal(0x400 + 5, dir.RelativeVirtualAddress);
            Assert.Equal(10, dir.Size);

            int dirOffset;
            Assert.True(peHeaders.TryGetDirectoryOffset(dir, out dirOffset));
            Assert.Equal(0x400 + 5, dirOffset);

            Assert.False(peHeaders.TryGetDirectoryOffset(new DirectoryEntry(0, 10), out dirOffset));
            Assert.Equal(-1, dirOffset);

            Assert.True(peHeaders.TryGetDirectoryOffset(new DirectoryEntry(0x600, 0x300), out dirOffset));
            Assert.Equal(0x600, dirOffset);

            Assert.False(peHeaders.TryGetDirectoryOffset(new DirectoryEntry(0x1000, 10), out dirOffset));
            Assert.Equal(-1, dirOffset);
        }
    }
}
