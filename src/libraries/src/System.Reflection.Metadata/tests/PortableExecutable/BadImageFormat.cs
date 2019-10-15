// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class BadImageFormat
    {
        [Fact]
        public void InvalidSectionCount()
        {
            var pe = new MemoryStream(new byte[] {
                0xd0, 0xcf, 0x11, 0xe0, 0xa1,
                0xb1, 0x1a, 0xe1, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00,
                0x00,
            });

            Assert.Throws<BadImageFormatException>(() => new PEHeaders(pe));
        }
    }
}
