// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Numerics;
using System.Text;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace System.Xml
{
    internal abstract class XmlStreamNodeWriter : XmlNodeWriter
    {
        private Stream _stream = null!; // initialized by SetOutput
        private readonly byte[] _buffer;
        private int _offset;
        private bool _ownsStream;
        private const int bufferLength = 512;
        private const int maxBytesPerChar = 3;
        private Encoding? _encoding;
        private static readonly UTF8Encoding s_UTF8Encoding = new UTF8Encoding(false, true);

        protected XmlStreamNodeWriter()
        {
            _buffer = new byte[bufferLength];
        }

        protected void SetOutput(Stream stream, bool ownsStream, Encoding? encoding)
        {
            _stream = stream;
            _ownsStream = ownsStream;
            _offset = 0;
            _encoding = encoding;
        }

        // StreamBuffer/BufferOffset exists only for the BinaryWriter to fix up nodes
        public byte[] StreamBuffer
        {
            get
            {
                return _buffer;
            }
        }
        public int BufferOffset
        {
            get
            {
                return _offset;
            }
        }

        public int Position
        {
            get
            {
                return (int)_stream.Position + _offset;
            }
        }

        protected byte[] GetBuffer(int count, out int offset)
        {
            DiagnosticUtility.DebugAssert(count >= 0 && count <= bufferLength, "");
            int bufferOffset = _offset;
            if (bufferOffset + count <= bufferLength)
            {
                offset = bufferOffset;
            }
            else
            {
                FlushBuffer();
                offset = 0;
            }
#if DEBUG
            DiagnosticUtility.DebugAssert(offset + count <= bufferLength, "");
            for (int i = 0; i < count; i++)
            {
                _buffer[offset + i] = (byte)'<';
            }
#endif
            return _buffer;
        }

        protected async Task<BytesWithOffset> GetBufferAsync(int count)
        {
            int offset;
            DiagnosticUtility.DebugAssert(count >= 0 && count <= bufferLength, "");
            int bufferOffset = _offset;
            if (bufferOffset + count <= bufferLength)
            {
                offset = bufferOffset;
            }
            else
            {
                await FlushBufferAsync().ConfigureAwait(false);
                offset = 0;
            }
#if DEBUG
            DiagnosticUtility.DebugAssert(offset + count <= bufferLength, "");
            for (int i = 0; i < count; i++)
            {
                _buffer[offset + i] = (byte)'<';
            }
#endif
            return new BytesWithOffset(_buffer, offset);
        }

        protected void Advance(int count)
        {
            DiagnosticUtility.DebugAssert(_offset + count <= bufferLength, "");
            _offset += count;
        }

        private void EnsureByte()
        {
            if (_offset >= bufferLength)
            {
                FlushBuffer();
            }
        }

        protected void WriteByte(byte b)
        {
            EnsureByte();
            _buffer[_offset++] = b;
        }

        protected Task WriteByteAsync(byte b)
        {
            if (_offset >= bufferLength)
            {
                return FlushBufferAndWriteByteAsync(b);
            }
            else
            {
                _buffer[_offset++] = b;
                return Task.CompletedTask;
            }
        }

        private async Task FlushBufferAndWriteByteAsync(byte b)
        {
            await FlushBufferAsync().ConfigureAwait(false);
            _buffer[_offset++] = b;
        }

        protected void WriteByte(char ch)
        {
            DiagnosticUtility.DebugAssert(ch < 0x80, "");
            WriteByte((byte)ch);
        }

        protected Task WriteByteAsync(char ch)
        {
            DiagnosticUtility.DebugAssert(ch < 0x80, "");
            return WriteByteAsync((byte)ch);
        }

        protected void WriteBytes(byte b1, byte b2)
        {
            byte[] buffer = _buffer;
            int offset = _offset;
            if (offset + 1 >= bufferLength)
            {
                FlushBuffer();
                offset = 0;
            }
            buffer[offset + 0] = b1;
            buffer[offset + 1] = b2;
            _offset += 2;
        }

        protected Task WriteBytesAsync(byte b1, byte b2)
        {
            if (_offset + 1 >= bufferLength)
            {
                return FlushAndWriteBytesAsync(b1, b2);
            }
            else
            {
                _buffer[_offset++] = b1;
                _buffer[_offset++] = b2;
                return Task.CompletedTask;
            }
        }

        private async Task FlushAndWriteBytesAsync(byte b1, byte b2)
        {
            await FlushBufferAsync().ConfigureAwait(false);
            _buffer[_offset++] = b1;
            _buffer[_offset++] = b2;
        }

        protected void WriteBytes(char ch1, char ch2)
        {
            DiagnosticUtility.DebugAssert(ch1 < 0x80 && ch2 < 0x80, "");
            WriteBytes((byte)ch1, (byte)ch2);
        }

        protected Task WriteBytesAsync(char ch1, char ch2)
        {
            DiagnosticUtility.DebugAssert(ch1 < 0x80 && ch2 < 0x80, "");
            return WriteBytesAsync((byte)ch1, (byte)ch2);
        }

        public void WriteBytes(byte[] byteBuffer, int byteOffset, int byteCount)
        {
            if (byteCount < bufferLength)
            {
                int offset;
                byte[] buffer = GetBuffer(byteCount, out offset);
                Buffer.BlockCopy(byteBuffer, byteOffset, buffer, offset, byteCount);
                Advance(byteCount);
            }
            else
            {
                FlushBuffer();
                _stream.Write(byteBuffer, byteOffset, byteCount);
            }
        }

        protected unsafe void UnsafeWriteBytes(byte* bytes, int byteCount)
        {
            FlushBuffer();
            byte[] buffer = _buffer;
            while (byteCount >= bufferLength)
            {
                for (int i = 0; i < bufferLength; i++)
                    buffer[i] = bytes[i];
                _stream.Write(buffer, 0, bufferLength);
                bytes += bufferLength;
                byteCount -= bufferLength;
            }
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = bytes[i];
                _stream.Write(buffer, 0, byteCount);
            }
        }

        protected unsafe void WriteUTF8Char(int ch)
        {
            if (ch < 0x80)
            {
                WriteByte((byte)ch);
            }
            else if (ch <= char.MaxValue)
            {
                char* chars = stackalloc char[1];
                chars[0] = (char)ch;
                UnsafeWriteUTF8Chars(chars, 1);
            }
            else
            {
                SurrogateChar surrogateChar = new SurrogateChar(ch);
                char* chars = stackalloc char[2];
                chars[0] = surrogateChar.HighChar;
                chars[1] = surrogateChar.LowChar;
                UnsafeWriteUTF8Chars(chars, 2);
            }
        }

        protected void WriteUTF8Chars(byte[] chars, int charOffset, int charCount)
        {
            if (charCount < bufferLength)
            {
                int offset;
                byte[] buffer = GetBuffer(charCount, out offset);
                Buffer.BlockCopy(chars, charOffset, buffer, offset, charCount);
                Advance(charCount);
            }
            else
            {
                FlushBuffer();
                _stream.Write(chars, charOffset, charCount);
            }
        }

        protected unsafe void WriteUTF8Chars(string value)
        {
            int count = value.Length;
            if (count > 0)
            {
                fixed (char* chars = value)
                {
                    UnsafeWriteUTF8Chars(chars, count);
                }
            }
        }

        protected unsafe void UnsafeWriteUTF8Chars(char* chars, int charCount)
        {
            const int charChunkSize = bufferLength / maxBytesPerChar;
            while (charCount > charChunkSize)
            {
                int offset;
                int chunkSize = charChunkSize;
                if ((int)(chars[chunkSize - 1] & 0xFC00) == 0xD800) // This is a high surrogate
                    chunkSize--;
                byte[] buffer = GetBuffer(chunkSize * maxBytesPerChar, out offset);
                Advance(UnsafeGetUTF8Chars(chars, chunkSize, buffer, offset));
                charCount -= chunkSize;
                chars += chunkSize;
            }
            if (charCount > 0)
            {
                int offset;
                byte[] buffer = GetBuffer(charCount * maxBytesPerChar, out offset);
                Advance(UnsafeGetUTF8Chars(chars, charCount, buffer, offset));
            }
        }

        protected unsafe void UnsafeWriteUnicodeChars(char* chars, int charCount)
        {
            const int charChunkSize = bufferLength / 2;
            while (charCount > charChunkSize)
            {
                int offset;
                int chunkSize = charChunkSize;
                if ((int)(chars[chunkSize - 1] & 0xFC00) == 0xD800) // This is a high surrogate
                    chunkSize--;
                byte[] buffer = GetBuffer(chunkSize * 2, out offset);
                Advance(UnsafeGetUnicodeChars(chars, chunkSize, buffer, offset));
                charCount -= chunkSize;
                chars += chunkSize;
            }
            if (charCount > 0)
            {
                int offset;
                byte[] buffer = GetBuffer(charCount * 2, out offset);
                Advance(UnsafeGetUnicodeChars(chars, charCount, buffer, offset));
            }
        }

        protected unsafe int UnsafeGetUnicodeChars(char* chars, int charCount, byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                new ReadOnlySpan<byte>((byte*)chars, 2 * charCount)
                    .CopyTo(buffer.AsSpan(offset));
            }
            else
            {
                char* charsMax = chars + charCount;
                while (chars < charsMax)
                {
                    char value = *chars++;
                    buffer[offset++] = (byte)value;
                    buffer[offset++] = (byte)(value >> 8);
                }
            }

            return charCount * 2;
        }

        protected unsafe int UnsafeGetUTF8Length(char* chars, int charCount)
        {
            char* charsMax = chars + charCount;

            // This method is only called from 2 places and will use length of at least (128/3 and 256/3) respectivly
            // AVX is faster for at least 2048 chars, probably more
            // for other cases the encoding path is better optimized than any fast path done here.
            if (Vector.IsHardwareAccelerated
                && Vector<short>.Count > Vector128<short>.Count
                && Vector<short>.Count < charCount && charCount <= 2048)
            {
                char* lastSimd = chars + charCount - Vector<short>.Count;
                var mask = new Vector<short>(unchecked((short)0xff80));

                while (chars < lastSimd)
                {
                    if (((*(Vector<short>*)chars) & mask) != Vector<short>.Zero)
                        goto NonAscii;

                    chars += Vector<short>.Count;
                }

                if ((*(Vector<short>*)lastSimd & mask) == Vector<short>.Zero)
                    return charCount;
            }

        NonAscii:
            int numRemaining = (int)(charsMax - chars);
            int numAscii = charCount - numRemaining;

            return numAscii + (_encoding ?? s_UTF8Encoding).GetByteCount(chars, numRemaining);
        }

        protected unsafe int UnsafeGetUTF8Chars(char* chars, int charCount, byte[] buffer, int offset)
        {
            if (charCount > 0)
            {
                fixed (byte* _bytes = &buffer[offset])
                {
                    byte* bytes = _bytes;
                    byte* bytesMax = &bytes[buffer.Length - offset];
                    char* charsMax = &chars[charCount];
                    char* simdLast = chars + charCount - Vector128<ushort>.Count;

                    if (Sse41.IsSupported && charCount >= Vector128<ushort>.Count)
                    {
                        var mask = Vector128.Create(unchecked((short)0xff80));

                        while (chars < simdLast)
                        {
                            var v = Sse2.LoadVector128((short*)chars);
                            if (!Sse41.TestZ(v, mask))
                                goto NonAscii;

                            Sse2.StoreScalar((long*)bytes, Sse2.PackUnsignedSaturate(v, v).AsInt64());
                            bytes += Vector128<ushort>.Count;
                            chars += Vector128<ushort>.Count;
                        }

                        var v2 = Sse2.LoadVector128((short*)simdLast);
                        if (!Sse41.TestZ(v2, mask))
                            goto NonAscii;

                        Sse2.StoreScalar((long*)(bytesMax - sizeof(long)), Sse2.PackUnsignedSaturate(v2, v2).AsInt64());
                        return charCount;
                    }
                    // Directly jump to system encoding for larger strings, since it is faster even for the all Ascii case
                    else if (charCount < 16)
                    {
                        while (chars < charsMax)
                        {
                            char t = *chars;
                            if (t >= 0x80)
                                goto NonAscii;

                            *bytes = (byte)t;
                            bytes++;
                            chars++;
                        }

                        return charCount;
                    }

                NonAscii:
                    return (int)(bytes - _bytes) + (_encoding ?? s_UTF8Encoding).GetBytes(chars, (int)(charsMax - chars), bytes, (int)(bytesMax - bytes));
                }
            }
            return 0;
        }

        protected virtual void FlushBuffer()
        {
            if (_offset != 0)
            {
                _stream.Write(_buffer, 0, _offset);
                _offset = 0;
            }
        }

        protected virtual Task FlushBufferAsync()
        {
            if (_offset != 0)
            {
                var task = _stream.WriteAsync(_buffer, 0, _offset);
                _offset = 0;
                return task;
            }

            return Task.CompletedTask;
        }

        public override void Flush()
        {
            FlushBuffer();
            _stream.Flush();
        }

        public override async Task FlushAsync()
        {
            await FlushBufferAsync().ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);
        }

        public override void Close()
        {
            if (_stream != null)
            {
                if (_ownsStream)
                {
                    _stream.Dispose();
                }
                _stream = null!;
            }
        }
    }
}
