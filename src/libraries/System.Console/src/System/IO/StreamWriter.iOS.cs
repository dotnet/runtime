// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    internal class IOSStreamWriter : StreamWriter
    {
        private readonly List<byte> _acc = new List<byte>();
        private int _lineStartPos;

        public IOSStreamWriter(Stream stream, Encoding? encoding = null, int bufferSize = -1, bool leaveOpen = false)
            : base(stream, encoding, bufferSize, leaveOpen)
        {
        }

        protected override void Flush(bool flushStream, bool flushEncoder)
        {
            // flushEncoder should be true at the end of the file and if
            // the user explicitly calls Flush (though not if AutoFlush is true).
            // This is required to flush any dangling characters from our UTF-7
            // and UTF-8 encoders.
            ThrowIfDisposed();

            // Perf boost for Flush on non-dirty writers.
            if (innerCharPos == 0 && !flushStream && !flushEncoder)
            {
                return;
            }

            if (!innerHaveWrittenPreamble)
            {
                innerHaveWrittenPreamble = true;
                ReadOnlySpan<byte> preamble = innerEncoding.Preamble;
                if (preamble.Length > 0)
                {
                    innerStream.Write(preamble);
                }
            }

            // For sufficiently small char data being flushed, try to encode to the stack.
            // For anything else, fall back to allocating the byte[] buffer.
            Span<byte> byteBuffer = stackalloc byte[0];
            if (innerByteBuffer is not null)
            {
                byteBuffer = innerByteBuffer;
            }
            else
            {
                int maxBytesForCharPos = innerEncoding.GetMaxByteCount(innerCharPos);
                byteBuffer = maxBytesForCharPos <= 1024 ? // arbitrary threshold
                    stackalloc byte[1024] :
                    (innerByteBuffer = new byte[innerEncoding.GetMaxByteCount(innerCharBuffer.Length)]);
            }

            for (int i = _lineStartPos; i < innerCharPos; i++)
            {
                if (innerCharBuffer[i] == '\n')
                {
                    int count = innerEncoder.GetBytes(new ReadOnlySpan<char>(innerCharBuffer, _lineStartPos, i - _lineStartPos), byteBuffer, flushEncoder);
                    if (count > 0)
                    {
                        _acc.AddRange(byteBuffer.Slice(0, count).ToArray());
                    }

                    innerStream.Write(CollectionsMarshal.AsSpan(_acc));
                    _acc.Clear();

                    _lineStartPos = i + 1;
                }
            }

            if (_lineStartPos < innerCharPos)
            {
                int count = innerEncoder.GetBytes(new ReadOnlySpan<char>(innerCharBuffer, _lineStartPos, innerCharPos - _lineStartPos), byteBuffer, flushEncoder);
                if (count > 0)
                {
                    _acc.AddRange(byteBuffer.Slice(0, count).ToArray());
                }
            }

            innerCharPos = 0;
            _lineStartPos = 0;

            if (flushStream && flushEncoder)
            {
                var accArray = _acc.ToArray();
                if (accArray.Length > 0)
                {
                    innerStream.Write(accArray.AsSpan<byte>().Slice(0, accArray.Length));
                    _acc.Clear();
                }
            }

            if (flushStream)
            {
                innerStream.Flush();
            }
        }
    }
}
