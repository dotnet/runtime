// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEHeaderTests
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
    }
}
