// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEHeadersTests
    {
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
                ".s1 offset=0x1f0 rva=0x1f0 size=10",
                ".s2 offset=0x1fa rva=0x1fa size=10",
                ".s3 offset=0x204 rva=0x204 size=10"
            }, peHeaders.SectionHeaders.Select(h => $"{h.Name} offset=0x{h.PointerToRawData:x3} rva=0x{h.VirtualAddress:x3} size={h.SizeOfRawData}"));
        }

        [Fact]
        public void GetContainingSectionIndex()
        {
            var peHeaders = new PEReader(SynthesizedPeImages.Image1).PEHeaders;

            Assert.Equal(-1, peHeaders.GetContainingSectionIndex(0));
            Assert.Equal(-1, peHeaders.GetContainingSectionIndex(0x1f0 - 1));
            Assert.Equal(0, peHeaders.GetContainingSectionIndex(0x1f0));
            Assert.Equal(1, peHeaders.GetContainingSectionIndex(0x1fa));
            Assert.Equal(2, peHeaders.GetContainingSectionIndex(0x204));
            Assert.Equal(2, peHeaders.GetContainingSectionIndex(0x204 + 9));
            Assert.Equal(-1, peHeaders.GetContainingSectionIndex(0x204 + 10));
        }

        [Fact]
        [ActiveIssue(1664)]
        public void TryGetDirectoryOffset()
        {
            var peHeaders = new PEReader(SynthesizedPeImages.Image1).PEHeaders;
            var dir = peHeaders.PEHeader.CopyrightTableDirectory;
            
            Assert.Equal(0x1fa + 5, dir.RelativeVirtualAddress);
            Assert.Equal(10, dir.Size);

            int dirOffset;
            Assert.True(peHeaders.TryGetDirectoryOffset(dir, out dirOffset));
            Assert.Equal(0x1fa + 5, dirOffset);

            Assert.False(peHeaders.TryGetDirectoryOffset(new DirectoryEntry(0, 10), out dirOffset));
            Assert.Equal(-1, dirOffset);

            Assert.True(peHeaders.TryGetDirectoryOffset(new DirectoryEntry(0x204, 100), out dirOffset));
            Assert.Equal(0x204, dirOffset);

            Assert.False(peHeaders.TryGetDirectoryOffset(new DirectoryEntry(0x500, 10), out dirOffset));
            Assert.Equal(-1, dirOffset);
        }
    }
}
