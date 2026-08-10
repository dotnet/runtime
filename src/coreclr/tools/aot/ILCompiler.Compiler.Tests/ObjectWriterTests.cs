// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using ILCompiler.ObjectWriter;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class ObjectWriterTests
    {
        [Fact]
        public void ReadStreamPositionFindsCorrectBuffer()
        {
            var sectionData = new SectionData();
            var expected = new List<byte>();

            for (int i = 0; i < 1_024; i++)
            {
                byte[] buffer = new byte[i % 5];
                buffer.AsSpan().Fill((byte)i);
                sectionData.AppendData(buffer);
                expected.AddRange(buffer);
            }

            Span<byte> appendedData = sectionData.BufferWriter.GetSpan(3).Slice(0, 3);
            appendedData.Fill(0xCC);
            sectionData.BufferWriter.Advance(appendedData.Length);
            expected.AddRange([0xCC, 0xCC, 0xCC]);

            using Stream stream = sectionData.GetReadStream();

            for (int position = 0; position < expected.Count; position++)
            {
                stream.Position = position;

                Assert.Equal(position, stream.Position);
                Assert.Equal(expected[position], stream.ReadByte());
            }

            stream.Position = stream.Length;
            Assert.Equal(-1, stream.ReadByte());
        }
    }
}
