// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    internal partial class CustomContent : HttpContent
    {
        internal class SlowTestStream : CustomStream
        {
            private int _delay = 1000;
            private int _count;
            private int _trigger;
            private byte[] _content;
            private readonly TaskCompletionSource<bool> _sendingDone;
            private int _iteration;

            internal SlowTestStream(byte[] content, TaskCompletionSource<bool> tsc, int count=10, int trigger=1) : base(content, false)
            {
                _sendingDone = tsc;
                _trigger = trigger;
                _count = count;
                _content = content;
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                var buffer = new byte[bufferSize];
                int bytesRead;
                while ((bytesRead = await ReadAsync(buffer, cancellationToken)) != 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    await destination.FlushAsync(); // some tests rely on data being written promptly to coordinate between client/server sides of test
                }
            }

            public async override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
            {
                if (_failException != null)
                {
                    throw _failException;
                }

                if (_delay > 0)
                {
                    await Task.Delay(_delay, cancellationToken);
                }

                _iteration++;
                if (_iteration == _trigger)
                {
                    _sendingDone?.TrySetResult(true);
                }

                if (_count == _iteration)
                {
                    return 0;
                }

                return _content.Length;
            }
        }
    }
}
