// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    internal partial class CustomContent : StreamContent
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

            public async override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
            {
                if (_delay > 0)
                {
                    await Task.Delay(_delay);
                }

                _iteration++;
                if (_iteration == _trigger)
                {
                    _sendingDone.TrySetResult(true);
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
