// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class StandalonePortablePdbStreamTests
    {
        private static unsafe void ReadHeader(out DebugMetadataHeader header, out int[] externalRowCounts, byte[] buffer)
        {
            fixed (byte* bufferPtr = &buffer[0])
            {
                MetadataReader.ReadStandalonePortablePdbStream(new MemoryBlock(bufferPtr, buffer.Length), out header, out externalRowCounts);
            }
        }

        [Fact]
        public void IdAndEntryPoint()
        {
            int[] externalRowCounts;
            DebugMetadataHeader header;

            ReadHeader(out header, out externalRowCounts, new byte[]
            {
                // ID:
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13,

                // entry point:
                0x00, 0x00, 0x00, 0x00,

                // external table mask:
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            Assert.True(header.EntryPoint.IsNil);
            Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13 }, header.Id);

            // entry point is nil MethodDef:
            Assert.Throws<BadImageFormatException>(() =>
                ReadHeader(out header, out externalRowCounts, new byte[]
                {
                    // ID:
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13,

                    // entry point:
                    0x00, 0x00, 0x00, 0x06,

                    // external table mask:
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                }));

            // entry point not a MethodDef:
            Assert.Throws<BadImageFormatException>(() =>
                ReadHeader(out header, out externalRowCounts, new byte[]
                {
                    // ID:
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13,

                    // entry point:
                    0x01, 0x00, 0x00, 0x2b,

                    // external table mask:
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                }));
        }
    }
}
