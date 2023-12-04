// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json
{
    internal readonly struct JsonWriterIndentationData : IEquatable<JsonWriterIndentationData>
    {
        private readonly Memory<byte> _bytes;
        private readonly int? _length;
        private readonly byte? _byte;

        private const byte Null = (byte)'\0';

        public readonly byte Byte => _byte ?? JsonConstants.Space;
        public readonly ReadOnlySpan<byte> Bytes => _bytes.Span;
        public readonly int Length => _length ?? 2;

        public static JsonWriterIndentationData FromString(string value) => new(value);

        private JsonWriterIndentationData(string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            byte[] indentBytes = Encoding.UTF8.GetBytes(value);
            byte? indentByte = null;

            foreach (byte b in indentBytes)
            {
                if (JsonConstants.ValidIndentChars.IndexOf(b) is -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(value)} contains an invalid character. Allowed characters are space and horizontal tab.");
                }

                if (indentByte is not Null)
                {
                    byte? previous = indentByte;
                    indentByte = b;
                    if (previous is not null && indentByte != previous)
                    {
                        indentByte = Null;
                    }
                }
            }

            _byte = indentByte;
            _length = indentBytes.Length;

            if (indentByte is Null)
            {
                _bytes = indentBytes;
            }
        }

        public readonly bool Equals(JsonWriterIndentationData other) =>
            _byte == other._byte &&
            _length == other._length &&
            _bytes.Span.SequenceEqual(other._bytes.Span);

        public override bool Equals(object? obj) =>
            obj is JsonWriterIndentationData indentation && Equals(indentation);

        public override readonly int GetHashCode()
        {
            HashCode hc = default;
            hc.Add(_byte);
            hc.Add(_length);
            hc.Add(_bytes.GetHashCode());
            return hc.ToHashCode();
        }

#if !NETCOREAPP
        /// <summary>
        /// Polyfill for System.HashCode.
        /// </summary>
        private struct HashCode
        {
            private int _hashCode;
            public void Add<T>(T? value) => _hashCode = (_hashCode, value).GetHashCode();
            public int ToHashCode() => _hashCode;
        }
#endif

        public void Write(Span<byte> buffer, int indentation)
        {
            Debug.Assert(buffer.Length >= indentation);

            if (Byte is not Null)
            {
                // Based on perf tests, the break-even point where vectorized Fill is faster
                // than explicitly writing the space in a loop is 8.
                if (indentation < 8 && indentation % 2 == 0)
                {
                    int i = 0;
                    while (i < indentation)
                    {
                        buffer[i++] = Byte;
                        buffer[i++] = Byte;
                    }
                }
                else
                {
                    buffer.Slice(0, indentation).Fill(Byte);
                }
            }
            else if (Bytes is { Length: > 0 } indentBytes)
            {
                int offset = 0;
                while (offset + indentBytes.Length <= indentation)
                {
                    indentBytes.CopyTo(buffer.Slice(offset));
                    offset += indentBytes.Length;
                }
            }
        }
    }
}
