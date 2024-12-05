// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions.
    /// </summary>
    public static partial class Math
    {
        [StackTraceHidden]
        internal static int DivInt32(int dividend, int divisor) => DivInt32Internal(dividend, divisor);

        [StackTraceHidden]
        internal static uint DivUInt32(uint dividend, uint divisor) => DivUInt32Internal(dividend, divisor);

        [StackTraceHidden]
        internal static long DivInt64(long dividend, long divisor) => DivInt64Internal(dividend, divisor);

        [StackTraceHidden]
        internal static ulong DivUInt64(ulong dividend, ulong divisor) => DivUInt64Internal(dividend, divisor);

        [StackTraceHidden]
        internal static int ModInt32(int dividend, int divisor) => ModInt32Internal(dividend, divisor);

        [StackTraceHidden]
        internal static uint ModUInt32(uint dividend, uint divisor) => ModUInt32Internal(dividend, divisor);

        [StackTraceHidden]
        internal static long ModInt64(long dividend, long divisor) => ModInt64Internal(dividend, divisor);

        [StackTraceHidden]
        internal static ulong ModUInt64(ulong dividend, ulong divisor) => ModUInt64Internal(dividend, divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial int DivInt32Internal(int dividend, int divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial uint DivUInt32Internal(uint dividend, uint divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial long DivInt64Internal(long dividend, long divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial ulong DivUInt64Internal(ulong dividend, ulong divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial int ModInt32Internal(int dividend, int divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial uint ModUInt32Internal(uint dividend, uint divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial long ModInt64Internal(long dividend, long divisor);

        [LibraryImport(RuntimeHelpers.QCall), SuppressGCTransition]
        private static partial ulong ModUInt64Internal(ulong dividend, ulong divisor);
    }
}
