// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class SectionHeaderTests
    {
        [Theory]
        [InlineData(".debug", 0, 0, 0x5C, 0x152, 0, 0, 0, 0, SectionCharacteristics.LinkerInfo)]
        [InlineData(".drectve", 0, 0, 26, 0x12C, 0, 0, 0, 0, SectionCharacteristics.Align1Bytes)]
        [InlineData("", 1, 1, 2, 3, 5, 8, 13, 21, SectionCharacteristics.Align16Bytes)]
        [InlineData("x", 1, 1, 2, 3, 5, 8, 13, 21, SectionCharacteristics.MemSysheap)]
        [InlineData(".\u092c\u0917", int.MaxValue, int.MinValue, int.MaxValue, int.MinValue, int.MaxValue, int.MaxValue, ushort.MaxValue, ushort.MaxValue, SectionCharacteristics.GPRel)]
        [InlineData("nul\u0000nul", 1, 1, 1, 1, 1, 1, 1, 1, SectionCharacteristics.ContainsInitializedData)]
        public void Ctor(
            string name,
            int virtualSize,
            int virtualAddress,
            int sizeOfRawData,
            int ptrToRawData,
            int ptrToRelocations,
            int ptrToLineNumbers,
            ushort numRelocations,
            ushort numLineNumbers,
            SectionCharacteristics characteristics)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(PadSectionName(name));
            writer.Write(virtualSize);
            writer.Write(virtualAddress);
            writer.Write(sizeOfRawData);
            writer.Write(ptrToRawData);
            writer.Write(ptrToRelocations);
            writer.Write(ptrToLineNumbers);
            writer.Write(numRelocations);
            writer.Write(numLineNumbers);
            writer.Write((uint) characteristics);
            writer.Dispose();

            stream.Position = 0;
            var reader = new PEBinaryReader(stream, (int) stream.Length);

            var header = new SectionHeader(ref reader);

            Assert.Equal(name, header.Name);
            Assert.Equal(virtualSize, header.VirtualSize);
            Assert.Equal(virtualAddress, header.VirtualAddress);
            Assert.Equal(sizeOfRawData, header.SizeOfRawData);
            Assert.Equal(ptrToRawData, header.PointerToRawData);
            Assert.Equal(ptrToLineNumbers, header.PointerToLineNumbers);
            Assert.Equal(numRelocations, header.NumberOfRelocations);
            Assert.Equal(numLineNumbers, header.NumberOfLineNumbers);
            Assert.Equal(characteristics, header.SectionCharacteristics);
        }

        private static byte[] PadSectionName(string name)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            Assert.True(name.Length <= SectionHeader.NameSize);

            var bytes = new byte[SectionHeader.NameSize];
            Buffer.BlockCopy(nameBytes, 0, bytes, 0, nameBytes.Length);
            return bytes;
        }
    }
}
