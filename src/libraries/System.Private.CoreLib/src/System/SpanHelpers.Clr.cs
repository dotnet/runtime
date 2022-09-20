// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System
{
    internal static partial class SpanHelpers // helpers used by CLR
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfChar(ref char searchSpace, char value, int length)
            => IndexOfValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => IndexOfValueType<T, DontNegate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => IndexOfValueType<T, Negate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => LastIndexOfValueType<T, DontNegate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => LastIndexOfValueType<T, Negate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyChar(ref char searchSpace, char value0, char value1, int length)
            => IndexOfAnyValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, length);
    }
}
