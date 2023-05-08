// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;

namespace Microsoft.Extensions.Logging
{
    public ref struct BufferWriter<T>
    {
        private Span<T> _currentSpan;
        private IBufferWriter<T> _writer;
        private int _allocated;

        public BufferWriter(IBufferWriter<T> bufferWriter)
        {
            _writer = bufferWriter;
            _currentSpan = bufferWriter.GetSpan();
            _allocated = _currentSpan.Length;
        }

        public IBufferWriter<T> Writer => _writer;
        public Span<T> CurrentSpan => _currentSpan;

        public void Advance(int len)
        {
            _currentSpan = _currentSpan.Slice(len);
        }

        public void EnsureSize(int minSize)
        {
            if (_currentSpan.Length < minSize)
            {
                Grow(minSize);
            }
        }

        public void Grow(int minSize)
        {
            if (_allocated != _currentSpan.Length)
            {
                Flush();
            }
            _currentSpan = _writer.GetSpan(minSize);
            _allocated = _currentSpan.Length;
        }

        public void Flush()
        {
            _writer.Advance(_allocated - _currentSpan.Length);
            _currentSpan = default;
            _allocated = 0;
        }
    }

    internal static class BufferWriterExtensions
    {
        public static void Write(ref this BufferWriter<char> writer, ReadOnlySpan<char> value)
        {
            if (!value.TryCopyTo(writer.CurrentSpan))
            {
                writer.Grow(value.Length);
                value.CopyTo(writer.CurrentSpan);
            }
            writer.Advance(value.Length);
        }

        public static void Write(ref this BufferWriter<char> writer, ReadOnlySpan<char> value, int alignment)
        {
            int valueLen = value.Length;
            bool leftAlign = false;
            if (alignment < 0)
            {
                leftAlign = true;
                alignment = -alignment;
            }
            int lenNeeded = Math.Max(alignment, valueLen);
            int paddingNeeded = lenNeeded - valueLen;
            if (writer.CurrentSpan.Length < lenNeeded)
            {
                writer.Grow(lenNeeded);
            }
            Span<char> currentSpan = writer.CurrentSpan;
            if (leftAlign)
            {
                currentSpan.Slice(0, paddingNeeded).Fill(' ');
                value.CopyTo(currentSpan.Slice(paddingNeeded));
            }
            else
            {
                value.CopyTo(currentSpan.Slice(paddingNeeded));
                currentSpan.Slice(valueLen, paddingNeeded).Fill(' ');
            }
            writer.Advance(lenNeeded);
        }

        /*
        public void Append(int value)
        {
            if (_unusedChars.Length < 20)
            {
                Grow(20);
            }
            value.TryFormat(_unusedChars, out int written);
            _unusedChars = _unusedChars.Slice(written);
        }

        public void Append<T>(T value)
        {
            if (value is string strVal)
            {
                Append(strVal);
            }
            else if(value is int intVal)
            {
                Append(intVal);
            }
            else if(value != null)
            {
                Append(value.ToString());
            }
        }*/
    }
}
