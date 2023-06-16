// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// This is a state machine that produces a formatted string. It is used as part of a solution for high-performance
    /// and low/no allocation ILogger message formatting.
    /// It is similar to the DefaultInterpolatedStringHandler with a few design differences:
    /// 1. The outputted format matches the pre-existing behavior for ILogger.Log extension methods. This is mostly
    /// string.Format() behavior but has special cases for formatting null values and arrays.
    /// 2. There is no support for IFormatProvider or ICustomFormatter.
    /// 3. The memory backing the formatted output comes from an IBufferWriter rather than a rented array.
    /// 4. This type isn't a ref struct. It requires the caller to manage the span parameter by repeatedly passing it in.
    /// (This type needs to get passed as a generic parameter and ref structs can't be passed that way today)
    /// 5. FormattingState knows the CompositeFormat for the string that is being formatted and tracks the current
    /// parameter index internally whereas DefaultInterpolatedStringHandler expects the caller to keep track.
    /// 6. Related to (5), the caller only calls to provide the format hole arguments whereas DefaultInterpolatedStringHandler
    /// uses calls to provide all the literals as well.
    ///
    ///
    /// Usage:
    /// 1. Construct with a CompositeFormat and an IBufferWriter that receives the formatted data
    /// 2. Call Init() to get an initial Span. A ref to this span will need to passed into each subsequent call. If the
    /// span is ever too small a new Span will automatically be created from the IBufferWriter.
    /// 3. In order from left to right for each hole in the format string call AppendPropertyUtf16 or AppendSpanPropertyUtf16
    /// to provide the value that fills that hole.
    /// 4. Call Finish() to flush
    ///
    /// In the future I imagine we might want to add Encoding support for UTF8, but for now it is UTF16 only. If a particular logging
    /// sink needed high performance UTF8 formatting this code could always be replicated external to M.E.L.Abstractions.
    /// </summary>
    internal struct FormattingState
    {
        private readonly InternalCompositeFormat _format;
        private readonly IBufferWriter<byte> _writer;
        private int _index;
        private int _allocated;


        private const string NullValue = "(null)";
        private const int GuessedLengthPerHole = 11;
        private const int MinimumArrayPoolLength = 256;

        public FormattingState(InternalCompositeFormat format, IBufferWriter<byte> writer)
        {
            _format = format;
            _writer = writer;
        }

        public void Init(out Span<byte> span)
        {
            int estimatedSize = Math.Max(MinimumArrayPoolLength, _format._literalLength + (_format._formattedCount * GuessedLengthPerHole));
            span = _writer.GetSpan(estimatedSize);
            _allocated = span.Length;
        }

        public void AppendPropertyUtf16<TProp>(ref Span<byte> span, TProp propertyValue)
        {
            // These range checks wouldn't be needed if we detect argument<->hole mismatches upfront
            if (_index == _format._segments.Length)
            {
                return;
            }
            string? literal = _format._segments[_index].Literal;
            if (literal != null)
            {
                AppendStringUtf16(ref span, literal);
                _index++;
                if (_index == _format._segments.Length)
                {
                    return;
                }
            }
            (_, _, int alignment, string? format) = _format._segments[_index];
            _index++;

            if (propertyValue is string stringVal)
            {
                AppendStringPropertyUtf16(ref span, stringVal, alignment);
            }
            else
            {
                AppendNonStringPropertyUtf16(ref span, propertyValue, alignment, format);
            }
        }

        public void AppendSpanPropertyUtf16(ref Span<byte> writeBuffer, scoped ReadOnlySpan<char> propertyValue)
        {
            // These range checks wouldn't be needed if we detect argument<->hole mismatches upfront
            if (_index == _format._segments.Length)
            {
                return;
            }
            string? literal = _format._segments[_index].Literal;
            if (literal != null)
            {
                AppendStringUtf16(ref writeBuffer, literal);
                _index++;
                if (_index == _format._segments.Length)
                {
                    return;
                }
            }
            int alignment = _format._segments[_index].Alignment;
            _index++;
            if (alignment == 0)
            {
                AppendCharsUtf16(ref writeBuffer, propertyValue);
            }
            else
            {
                AppendAlignedCharSpanUtf16(ref writeBuffer, propertyValue, alignment);
            }
        }

        public void Finish(ref Span<byte> span)
        {
            if (_index < _format._segments.Length)
            {
                Debug.Assert(_index == _format._segments.Length - 1);
                string? literal = _format._segments[_index].Literal;
                Debug.Assert(literal != null);
                if (literal != null)
                {
                    AppendStringUtf16(ref span, literal);
                    _index++;
                }
            }
            Flush(ref span);
        }

        private void AppendStringPropertyUtf16(ref Span<byte> span, string propertyValue, int alignment)
        {
            if (alignment == 0)
            {
                AppendStringUtf16(ref span, propertyValue);
            }
            else
            {
                AppendAlignedPropertyUtf16(ref span, propertyValue, alignment, null);
            }
        }

        private void AppendNonStringPropertyUtf16<TProp>(ref Span<byte> span, TProp propertyValue, int alignment, string? format)
        {
            if (alignment == 0)
            {
#if NET6_0_OR_GREATER
                if (propertyValue is ISpanFormattable)
                {
                    EnsureSize(ref span, 64);
                    if (((ISpanFormattable)propertyValue).TryFormat(MemoryMarshal.Cast<byte, char>(span), out int charsWritten, format, CultureInfo.InvariantCulture))
                    {
                        Advance(ref span, charsWritten * 2);
                        return;
                    }
                }
#endif
                AppendStringUtf16(ref span, FormatPropertyValue(propertyValue, format));
            }
            else
            {
                AppendAlignedPropertyUtf16(ref span, propertyValue, alignment, format);
            }
        }

        private void AppendAlignedCharSpanUtf16(ref Span<byte> writeBuffer, scoped ReadOnlySpan<char> value, int alignment)
        {
            bool leftAlign = false;
            int paddingNeeded;
            if (alignment < 0)
            {
                leftAlign = true;
                alignment = -alignment;
            }
            paddingNeeded = alignment - value.Length;
            if (paddingNeeded <= 0)
            {
                EnsureSize(ref writeBuffer, 2 * value.Length);
                Span<char> charWriteBuffer = MemoryMarshal.Cast<byte, char>(writeBuffer);
                value.CopyTo(charWriteBuffer);
                Advance(ref writeBuffer, 2 * value.Length);
                return;
            }

            EnsureSize(ref writeBuffer, 2 * alignment);
            Span<char> charWriteBuffer2 = MemoryMarshal.Cast<byte, char>(writeBuffer);
            if (leftAlign)
            {
                value.CopyTo(charWriteBuffer2);
                charWriteBuffer2.Slice(value.Length, paddingNeeded).Fill(' ');
            }
            else
            {
                charWriteBuffer2.Slice(0, paddingNeeded).Fill(' ');
                value.CopyTo(charWriteBuffer2.Slice(paddingNeeded));
            }
            Advance(ref writeBuffer, 2 * alignment);
        }

        private void AppendAlignedPropertyUtf16<TProp>(ref Span<byte> span, TProp value, int alignment, string? format)
        {
            bool leftAlign = false;
            int paddingNeeded;
            if (alignment < 0)
            {
                leftAlign = true;
                alignment = -alignment;
            }
#if NET6_0_OR_GREATER
            if (value is ISpanFormattable)
            {
                EnsureSize(ref span, 2 * Math.Max(32, alignment));
                Span<char> charSpan = MemoryMarshal.Cast<byte, char>(span);
                if (((ISpanFormattable)value).TryFormat(charSpan, out int charsWritten, format, CultureInfo.InvariantCulture))
                {
                    paddingNeeded = alignment - charsWritten;
                    if (paddingNeeded <= 0)
                    {
                        Advance(ref span, 2 * charsWritten);
                        return;
                    }
                    if (leftAlign)
                    {
                        charSpan.Slice(charsWritten, paddingNeeded).Fill(' ');
                    }
                    else
                    {
                        charSpan.Slice(0, charsWritten).CopyTo(charSpan.Slice(paddingNeeded));
                        charSpan.Slice(0, paddingNeeded).Fill(' ');
                    }
                    Advance(ref span, 2 * alignment);
                    return;
                }
            }
#endif

            string unpadded = FormatPropertyValue(value, format);
            paddingNeeded = alignment - unpadded.Length;
            EnsureSize(ref span, 2 * Math.Max(unpadded.Length, alignment));
            if (paddingNeeded <= 0)
            {
                AppendStringUtf16(ref span, unpadded);
                return;
            }
            Span<char> charSpan2 = MemoryMarshal.Cast<byte, char>(span);
            if (leftAlign)
            {
                unpadded.AsSpan().CopyTo(charSpan2);
                charSpan2.Slice(unpadded.Length, paddingNeeded).Fill(' ');
            }
            else
            {
                charSpan2.Slice(0, paddingNeeded).Fill(' ');
                unpadded.AsSpan().CopyTo(charSpan2.Slice(paddingNeeded));
            }
            Advance(ref span, 2 * alignment);
        }

        private static string FormatPropertyValue<T>(T value, string? format)
        {
            string? s;
            if (value is IFormattable)
            {
                s = ((IFormattable)value).ToString(format, null);
            }
            else if (value is not string && value is IEnumerable enumerable)
            {
                var vsb = new ValueStringBuilder(stackalloc char[256]);
                bool first = true;
                foreach (object? e in enumerable)
                {
                    if (!first)
                    {
                        vsb.Append(", ");
                    }

                    vsb.Append(e != null ? e.ToString() : NullValue);
                    first = false;
                }
                return vsb.ToString();
            }
            else
            {
                s = value?.ToString();
            }
            if (s == null)
            {
                return NullValue;
            }
            else
            {
                return s;
            }
        }

        private void AppendStringUtf16(ref Span<byte> span, string value)
        {
            AppendBytes(ref span, MemoryMarshal.AsBytes(value.AsSpan()));
        }

        private void AppendCharsUtf16(ref Span<byte> writeBuffer, scoped ReadOnlySpan<char> value)
        {
            Span<char> charWriteBuffer = MemoryMarshal.Cast<byte, char>(writeBuffer);
            if (!value.TryCopyTo(charWriteBuffer))
            {
                Grow(ref writeBuffer, 2 * value.Length);
                charWriteBuffer = MemoryMarshal.Cast<byte, char>(writeBuffer);
                value.CopyTo(charWriteBuffer);
            }
            Advance(ref writeBuffer, 2 * value.Length);
        }

        private void AppendBytes(ref Span<byte> span, ReadOnlySpan<byte> value)
        {
            if (!value.TryCopyTo(span))
            {
                Grow(ref span, value.Length);
                value.CopyTo(span);
            }
            Advance(ref span, value.Length);
        }

        private static void Advance(ref Span<byte> span, int len)
        {
            span = span.Slice(len);
        }

        private void EnsureSize(ref Span<byte> span, int minSize)
        {
            if (span.Length < minSize)
            {
                Grow(ref span, minSize);
            }
        }

        private void Grow(ref Span<byte> span, int minSize)
        {
            if (_allocated != span.Length)
            {
                Flush(ref span);
            }
            span = _writer.GetSpan(minSize);
            _allocated = span.Length;
        }

        private void Flush(ref Span<byte> span)
        {
            _writer.Advance(_allocated - span.Length);
            span = default;
            _allocated = 0;
        }
    }
}
