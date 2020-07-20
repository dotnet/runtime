// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    internal static class ParserHelpers
    {
        public const int ByteOverflowLength = 3;
        public const int ByteOverflowLengthHex = 2;
        public const int UInt16OverflowLength = 5;
        public const int UInt16OverflowLengthHex = 4;
        public const int UInt32OverflowLength = 10;
        public const int UInt32OverflowLengthHex = 8;
        public const int UInt64OverflowLength = 20;
        public const int UInt64OverflowLengthHex = 16;

        public const int SByteOverflowLength = 3;
        public const int SByteOverflowLengthHex = 2;
        public const int Int16OverflowLength = 5;
        public const int Int16OverflowLengthHex = 4;
        public const int Int32OverflowLength = 10;
        public const int Int32OverflowLengthHex = 8;
        public const int Int64OverflowLength = 19;
        public const int Int64OverflowLengthHex = 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(int i)
        {
            return (uint)(i - '0') <= ('9' - '0');
        }

        //
        // Enable use of ThrowHelper from TryParse() routines without introducing dozens of non-code-coveraged "value= default; bytesConsumed = 0; return false" boilerplate.
        //
        public static bool TryParseThrowFormatException(out int bytesConsumed)
        {
            bytesConsumed = 0;
            ThrowHelper.ThrowFormatException_BadFormatSpecifier();
            return false;
        }

        //
        // Enable use of ThrowHelper from TryParse() routines without introducing dozens of non-code-coveraged "value= default; bytesConsumed = 0; return false" boilerplate.
        //
        public static bool TryParseThrowFormatException<T>(out T value, out int bytesConsumed) where T : struct
        {
            value = default;
            return TryParseThrowFormatException(out bytesConsumed);
        }

        //
        // Enable use of ThrowHelper from TryParse() routines without introducing dozens of non-code-coveraged "value= default; bytesConsumed = 0; return false" boilerplate.
        //
        [DoesNotReturn]
        [StackTraceHidden]
        public static bool TryParseThrowFormatException<T>(ReadOnlySpan<byte> source, out T value, out int bytesConsumed) where T : struct
        {
            // The parameters to this method are ordered the same as our callers' parameters
            // allowing the JIT to avoid unnecessary register swapping or spilling.

            Unsafe.SkipInit(out value); // bypass language initialization rules since we're about to throw
            Unsafe.SkipInit(out bytesConsumed);
            ThrowHelper.ThrowFormatException_BadFormatSpecifier();

            Debug.Fail("Control should never reach this point.");
            return false;
        }
    }
}
