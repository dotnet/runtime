// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    public static partial class MemoryExtensions
    {
        internal static ReadOnlySpan<byte> TrimUtf8(this ReadOnlySpan<byte> span)
        {
            // Assume that in most cases input doesn't need trimming
            //
            // Since `DecodeFromUtf8` and `DecodeLastFromUtf8` return `Rune.ReplacementChar`
            // on failure and that is not whitespace, we can safely treat it as no trimming
            // and leave failure handling up to the caller instead

            Debug.Assert(!Rune.IsWhiteSpace(Rune.ReplacementChar));

            if (span.Length == 0)
            {
                return span;
            }

            _ = Rune.DecodeFromUtf8(span, out Rune first, out int firstBytesConsumed);

            if (Rune.IsWhiteSpace(first))
            {
                span = span[firstBytesConsumed..];
                return TrimFallback(span);
            }

            _ = Rune.DecodeLastFromUtf8(span, out Rune last, out int lastBytesConsumed);

            if (Rune.IsWhiteSpace(last))
            {
                span = span[..^lastBytesConsumed];
                return TrimFallback(span);
            }

            return span;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ReadOnlySpan<byte> TrimFallback(ReadOnlySpan<byte> span)
            {
                while (span.Length != 0)
                {
                    _ = Rune.DecodeFromUtf8(span, out Rune current, out int bytesConsumed);

                    if (!Rune.IsWhiteSpace(current))
                    {
                        break;
                    }

                    span = span[bytesConsumed..];
                }

                while (span.Length != 0)
                {
                    _ = Rune.DecodeLastFromUtf8(span, out Rune current, out int bytesConsumed);

                    if (!Rune.IsWhiteSpace(current))
                    {
                        break;
                    }

                    span = span[..^bytesConsumed];
                }

                return span;
            }
        }
    }
}
