// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Formats.Asn1
{
    internal abstract class RestrictedAsciiRangeEncoding : SpanBasedEncoding
    {
        private readonly byte _minCharAllowed;
        private readonly byte _range;

        protected RestrictedAsciiRangeEncoding(byte minCharAllowed, byte maxCharAllowed)
        {
            Debug.Assert(minCharAllowed <= maxCharAllowed);
            Debug.Assert(maxCharAllowed <= 0x7F);

            _minCharAllowed = minCharAllowed;
            _range = (byte)(maxCharAllowed - minCharAllowed);
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        protected override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write)
        {
            int position = 0;

            if (chars.Length >= Vector<byte>.Count && Vector.IsHardwareAccelerated)
            {
                position = GetBytesVectorized(chars, bytes, write);
            }

            for (; position < chars.Length; position++)
            {
                char c = chars[position];

                if (!IsAllowed(c))
                {
                    EncoderFallback.CreateFallbackBuffer().Fallback(c, position);

                    Debug.Fail("Fallback should have thrown");
                    throw new InvalidOperationException();
                }

                if (write)
                {
                    bytes[position] = (byte)c;
                }
            }

            return chars.Length;
        }

        protected override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write)
        {
            int position = 0;

            if (bytes.Length >= Vector<byte>.Count && Vector.IsHardwareAccelerated)
            {
                position = GetCharsVectorized(bytes, chars, write);
            }

            for (; position < bytes.Length; position++)
            {
                byte b = bytes[position];

                if (!IsAllowed(b))
                {
                    DecoderFallback.CreateFallbackBuffer().Fallback(
                        new[] { b },
                        position);

                    Debug.Fail("Fallback should have thrown");
                    throw new InvalidOperationException();
                }

                if (write)
                {
                    chars[position] = (char)b;
                }
            }

            return bytes.Length;
        }

        // The vectorization is left out of the GetChars and GetBytes directly to not regress the the code size
        // and register allocation for small inputs. Instead they are extracted methods.
        private int GetBytesVectorized(ReadOnlySpan<char> chars, Span<byte> bytes, bool write)
        {
            int available = write ? Math.Min(chars.Length, bytes.Length) : chars.Length;
            int vectorizedLength = available - (available % Vector<byte>.Count);
            int position = 0;

            Debug.Assert(Vector<byte>.Count == 2 * Vector<ushort>.Count);

            // Revisit this cast when Vector<char> is supported: https://github.com/dotnet/runtime/issues/127611
            ReadOnlySpan<ushort> source = MemoryMarshal.Cast<char, ushort>(chars);
            Vector<ushort> minCharAllowed = new Vector<ushort>(_minCharAllowed);
            Vector<ushort> range = new Vector<ushort>(_range);

            for (; position < vectorizedLength; position += Vector<byte>.Count)
            {
                Vector<ushort> lower = new Vector<ushort>(source.Slice(position));
                Vector<ushort> upper = new Vector<ushort>(source.Slice(position + Vector<ushort>.Count));

                if (!IsAllowed(lower, minCharAllowed, range) || !IsAllowed(upper, minCharAllowed, range))
                {
                    // If any element in the vector is not allowed, we break out and return the position before the
                    // current vector's width so that it goes down the scalar path. The scalar path will determine the
                    // precise location of the invalid element.
                    break;
                }

                if (write)
                {
                    Vector.Narrow(lower, upper).CopyTo(bytes.Slice(position));
                }
            }

            return position;
        }

        private int GetCharsVectorized(ReadOnlySpan<byte> bytes, Span<char> chars, bool write)
        {
            int available = write ? Math.Min(bytes.Length, chars.Length) : bytes.Length;
            int vectorizedLength = available - (available % Vector<byte>.Count);
            int position = 0;

            Debug.Assert(Vector<byte>.Count == 2 * Vector<ushort>.Count);

            // Revisit this cast when Vector<char> is supported: https://github.com/dotnet/runtime/issues/127611
            Span<ushort> destination = write ? MemoryMarshal.Cast<char, ushort>(chars) : Span<ushort>.Empty;
            Vector<byte> minCharAllowed = new Vector<byte>(_minCharAllowed);
            Vector<byte> range = new Vector<byte>(_range);

            for (; position < vectorizedLength; position += Vector<byte>.Count)
            {
                Vector<byte> source = new Vector<byte>(bytes.Slice(position));

                if (!IsAllowed(source, minCharAllowed, range))
                {
                    // If any element in the vector is not allowed, we break out and return the position before the
                    // current vector's width so that it goes down the scalar path. The scalar path will determine the
                    // precise location of the invalid element.
                    break;
                }

                if (write)
                {
                    Vector.Widen(source, out Vector<ushort> lower, out Vector<ushort> upper);
                    lower.CopyTo(destination.Slice(position));
                    upper.CopyTo(destination.Slice(position + Vector<ushort>.Count));
                }
            }

            return position;
        }

        private bool IsAllowed(byte value)
        {
            return (byte)(value - _minCharAllowed) <= _range;
        }

        private bool IsAllowed(char value)
        {
            return (uint)(value - _minCharAllowed) <= _range;
        }

        private static bool IsAllowed(Vector<byte> value, Vector<byte> minCharAllowed, Vector<byte> range)
        {
            Vector<byte> offset = value - minCharAllowed;
            return Vector.LessThanOrEqualAll(offset, range);
        }

        private static bool IsAllowed(Vector<ushort> value, Vector<ushort> minCharAllowed, Vector<ushort> range)
        {
            Vector<ushort> offset = value - minCharAllowed;
            return Vector.LessThanOrEqualAll(offset, range);
        }
    }
}
