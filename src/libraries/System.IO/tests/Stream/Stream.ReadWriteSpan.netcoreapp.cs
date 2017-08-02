// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class Stream_ReadWriteSpan
    {
        [Fact]
        public void ReadSpan_DelegatesToRead_Success()
        {
            bool readInvoked = false;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(0, offset);
                    Assert.Equal(20, count);

                    for (int i = 0; i < 10; i++) array[offset + i] = (byte)i;
                    return 10;
                });

            Assert.Equal(0, s.Read(new Span<byte>(new byte[0])));
            Assert.False(readInvoked);

            Span<byte> totalSpan = new byte[30];
            Span<byte> targetSpan = totalSpan.Slice(5, 20);

            Assert.Equal(10, s.Read(targetSpan));
            Assert.True(readInvoked);
            for (int i = 0; i < 10; i++) Assert.Equal(i, targetSpan[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(0, targetSpan[i]);
            readInvoked = false;
        }

        [Fact]
        public void WriteSpan_DelegatesToWrite_Success()
        {
            bool writeInvoked = false;
            var s = new DelegateStream(
                canWriteFunc: () => true,
                writeFunc: (array, offset, count) =>
                {
                    writeInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(0, offset);
                    Assert.Equal(3, count);

                    for (int i = 0; i < count; i++) Assert.Equal(i, array[offset + i]);
                });

            s.Write(new Span<byte>(new byte[0]));
            Assert.False(writeInvoked);

            Span<byte> span = new byte[10];
            span[3] = 1;
            span[4] = 2;
            s.Write(span.Slice(2, 3));
            Assert.True(writeInvoked);
            writeInvoked = false;
        }
    }
}
