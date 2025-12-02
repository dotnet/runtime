// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    internal sealed class ByteAtATimeContent : HttpContent
    {
        private readonly Task _waitToSend;
        private readonly TaskCompletionSource<bool> _startedSend;
        private readonly int _length;
        private readonly int _millisecondDelayBetweenBytes;

        public ByteAtATimeContent(int length) : this(length, Task.CompletedTask, new TaskCompletionSource<bool>(), millisecondDelayBetweenBytes: 0) { }

        public ByteAtATimeContent(int length, Task waitToSend, TaskCompletionSource<bool> startedSend, int millisecondDelayBetweenBytes)
        {
            _length = length;
            _waitToSend = waitToSend;
            _startedSend = startedSend;
            _millisecondDelayBetweenBytes = millisecondDelayBetweenBytes;
        }

#if NET
        protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
            SerializeToStreamAsync(stream, context).GetAwaiter().GetResult();
#endif

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await _waitToSend;
            _startedSend.SetResult(true);

            var buffer = new byte[1];
            for (int i = 0; i < _length; i++)
            {
                buffer[0] = (byte)i;
                await stream.WriteAsync(buffer);
                await stream.FlushAsync();
                await Task.Delay(_millisecondDelayBetweenBytes);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _length;
            return true;
        }
    }
}
