// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.IO
{
    internal abstract class CachedConsoleStream : ConsoleStream
    {
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly Encoding _encoding;
        private readonly Decoder _decoder;

        public CachedConsoleStream(Encoding encoding) : base(FileAccess.Write)
        {
            _encoding = encoding;
            _decoder = _encoding.GetDecoder();
        }

        public override int Read(Span<byte> buffer) => throw Error.GetReadNotSupported();

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            int maxCharCount = _encoding.GetMaxCharCount(buffer.Length);
            char[]? pooledBuffer = null;
            Span<char> charSpan = maxCharCount <= 512 ? stackalloc char[512] : (pooledBuffer = ArrayPool<char>.Shared.Rent(maxCharCount));
            try
            {
                int count = _decoder.GetChars(buffer, charSpan, false);
                if (count > 0)
                {
                    WriteOrCache(this, _buffer, charSpan.Slice(0, count));
                }
            }
            finally
            {
                if (pooledBuffer != null)
                {
                    ArrayPool<char>.Shared.Return(pooledBuffer);
                }
            }
        }

        protected abstract void Print(ReadOnlySpan<char> line);

        private static void WriteOrCache(CachedConsoleStream stream, StringBuilder cache, Span<char> charBuffer)
        {
            int lastNewLine = charBuffer.LastIndexOf('\n');
            if (lastNewLine != -1)
            {
                Span<char> lineSpan = charBuffer.Slice(0, lastNewLine);
                if (cache.Length > 0)
                {
                    stream.Print(cache.Append(lineSpan).ToString());
                    cache.Clear();
                }
                else
                {
                    stream.Print(lineSpan);
                }

                if (lastNewLine + 1 < charBuffer.Length)
                {
                    cache.Append(charBuffer.Slice(lastNewLine + 1));
                }

                return;
            }

            // no newlines found, add the entire buffer to the cache
            cache.Append(charBuffer);
        }
    }
}
