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
            if (InnerCharPos == 0 && !flushStream && !flushEncoder)
            {
                return;
            }

            if (!InnerHaveWrittenPreamble)
            {
                InnerHaveWrittenPreamble = true;
                ReadOnlySpan<byte> preamble = InnerEncoding.Preamble;
                if (preamble.Length > 0)
                {
                    InnerStream.Write(preamble);
                }
            }

            // For sufficiently small char data being flushed, try to encode to the stack.
            // For anything else, fall back to allocating the byte[] buffer.
            Span<byte> byteBuffer = stackalloc byte[0];
            if (InnerByteBuffer is not null)
            {
                byteBuffer = InnerByteBuffer;
            }
            else
            {
                int maxBytesForCharPos = InnerEncoding.GetMaxByteCount(InnerCharPos);
                byteBuffer = maxBytesForCharPos <= 1024 ? // arbitrary threshold
                    stackalloc byte[1024] :
                    (InnerByteBuffer = new byte[InnerEncoding.GetMaxByteCount(InnerCharBuffer.Length)]);
            }

            for (int i = _lineStartPos; i < InnerCharPos; i++)
            {
                if (InnerCharBuffer[i] == '\n')
                {
                    int count = InnerEncoder.GetBytes(new ReadOnlySpan<char>(InnerCharBuffer, _lineStartPos, i - _lineStartPos), byteBuffer, flushEncoder);
                    if (count > 0)
                    {
                        _acc.AddRange(byteBuffer.Slice(0, count).ToArray());
                    }

                    InnerStream.Write(CollectionsMarshal.AsSpan(_acc));
                    _acc.Clear();

                    _lineStartPos = i + 1;
                }
            }

            if (_lineStartPos < InnerCharPos)
            {
                int count = InnerEncoder.GetBytes(new ReadOnlySpan<char>(InnerCharBuffer, _lineStartPos, InnerCharPos - _lineStartPos), byteBuffer, flushEncoder);
                if (count > 0)
                {
                    _acc.AddRange(byteBuffer.Slice(0, count).ToArray());
                }
            }

            InnerCharPos = 0;
            _lineStartPos = 0;

            if (flushStream && flushEncoder)
            {
                var accSpan = CollectionsMarshal.AsSpan(_acc);
                if (accSpan.Length > 0)
                {
                    InnerStream.Write(accSpan.Slice(0, accSpan.Length));
                    _acc.Clear();
                }
            }

            if (flushStream)
            {
                InnerStream.Flush();
            }
        }
    }
}
