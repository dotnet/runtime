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
    }
}
