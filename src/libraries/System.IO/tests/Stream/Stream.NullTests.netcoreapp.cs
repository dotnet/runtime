// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public partial class NullTests
    {
        [Theory]
        [MemberData(nameof(NullStream_ReadWriteData))]
        public void TestNullStream_ReadSpan(byte[] buffer, int offset, int count)
        {
            if (buffer == null) return;

            byte[] copy = buffer.ToArray();
            Stream source = Stream.Null;

            int read = source.Read(new Span<byte>(buffer, offset, count));
            Assert.Equal(0, read);
            Assert.Equal(copy, buffer); // Make sure Read doesn't modify the buffer
            Assert.Equal(0, source.Position);
        }

        [Theory]
        [MemberData(nameof(NullStream_ReadWriteData))]
        public void TestNullStream_WriteSpan(byte[] buffer, int offset, int count)
        {
            if (buffer == null) return;

            byte[] copy = buffer.ToArray();
            Stream source = Stream.Null;

            source.Write(new Span<byte>(buffer, offset, count));
            Assert.Equal(copy, buffer); // Make sure Write doesn't modify the buffer
            Assert.Equal(0, source.Position);
        }
    }
}
