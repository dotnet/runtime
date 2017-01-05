// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class BufferedStreamFlushTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldNotFlushUnderlyingStreamIfReadOnly(bool underlyingCanSeek)
        {
            var underlying = new DelegateStream(
                canReadFunc: () => true,
                canWriteFunc: () => false,
                canSeekFunc: () => underlyingCanSeek,
                readFunc: (_, __, ___) => 123,
                writeFunc: (_, __, ___) =>
                {
                    throw new NotSupportedException();
                },
                seekFunc: (_, __) => 123L
            );

            var wrapper = new CallTrackingStream(underlying);

            var buffered = new BufferedStream(wrapper);
            buffered.ReadByte();

            buffered.Flush();
            Assert.Equal(0, wrapper.TimesCalled(nameof(wrapper.Flush)));

            await buffered.FlushAsync();
            Assert.Equal(0, wrapper.TimesCalled(nameof(wrapper.FlushAsync)));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task ShouldAlwaysFlushUnderlyingStreamIfWritable(bool underlyingCanRead, bool underlyingCanSeek)
        {
            var underlying = new DelegateStream(
                canReadFunc: () => underlyingCanRead,
                canWriteFunc: () => true,
                canSeekFunc: () => underlyingCanSeek,
                readFunc: (_, __, ___) => 123,
                writeFunc: (_, __, ___) => { },
                seekFunc: (_, __) => 123L
            );

            var wrapper = new CallTrackingStream(underlying);

            var buffered = new BufferedStream(wrapper);
            
            buffered.Flush();
            Assert.Equal(1, wrapper.TimesCalled(nameof(wrapper.Flush)));

            await buffered.FlushAsync();
            Assert.Equal(1, wrapper.TimesCalled(nameof(wrapper.FlushAsync)));

            buffered.WriteByte(0);
            
            buffered.Flush();
            Assert.Equal(2, wrapper.TimesCalled(nameof(wrapper.Flush)));

            await buffered.FlushAsync();
            Assert.Equal(2, wrapper.TimesCalled(nameof(wrapper.FlushAsync)));
        }
    }
}
