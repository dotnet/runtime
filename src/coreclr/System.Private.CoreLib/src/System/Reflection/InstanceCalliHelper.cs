// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    /// <summary>
    /// Provides a set of helper methods for calling instance methods using calli.
    /// This is necessary since C# function pointers currently do not support instance methods.
    /// </summary>
    internal static unsafe class InstanceCalliHelper
    {
        // Zero parameter methods such as property getters:

        [Intrinsic]
        internal static bool Call(delegate*<object, bool> fn, object o) => fn(o);

        [Intrinsic]
        internal static byte Call(delegate*<object, byte> fn, object o) => fn(o);

        [Intrinsic]
        internal static char Call(delegate*<object, char> fn, object o) => fn(o);

        [Intrinsic]
        internal static DateTime Call(delegate*<object, DateTime> fn, object o) => fn(o);

        [Intrinsic]
        internal static DateTimeOffset Call(delegate*<object, DateTimeOffset> fn, object o) => fn(o);

        [Intrinsic]
        internal static decimal Call(delegate*<object, decimal> fn, object o) => fn(o);

        [Intrinsic]
        internal static double Call(delegate*<object, double> fn, object o) => fn(o);

        [Intrinsic]
        internal static float Call(delegate*<object, float> fn, object o) => fn(o);

        [Intrinsic]
        internal static Guid Call(delegate*<object, Guid> fn, object o) => fn(o);

        [Intrinsic]
        internal static short Call(delegate*<object, short> fn, object o) => fn(o);

        [Intrinsic]
        internal static int Call(delegate*<object, int> fn, object o) => fn(o);

        [Intrinsic]
        internal static long Call(delegate*<object, long> fn, object o) => fn(o);

        [Intrinsic]
        internal static nint Call(delegate*<object, nint> fn, object o) => fn(o);

        [Intrinsic]
        internal static nuint Call(delegate*<object, nuint> fn, object o) => fn(o);

        [Intrinsic]
        internal static object? Call(delegate*<object, object?> fn, object o) => fn(o);

        [Intrinsic]
        internal static sbyte Call(delegate*<object, sbyte> fn, object o) => fn(o);

        [Intrinsic]
        internal static ushort Call(delegate*<object, ushort> fn, object o) => fn(o);

        [Intrinsic]
        internal static uint Call(delegate*<object, uint> fn, object o) => fn(o);

        [Intrinsic]
        internal static ulong Call(delegate*<object, ulong> fn, object o) => fn(o);

        [Intrinsic]
        internal static void Call(delegate*<object, void> fn, object o) => fn(o);

        // One parameter methods with no return such as property setters:

        [Intrinsic]
        internal static void Call(delegate*<object, bool, void> fn, object o, bool arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, byte, void> fn, object o, byte arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, char, void> fn, object o, char arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, DateTime, void> fn, object o, DateTime arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, DateTimeOffset, void> fn, object o, DateTimeOffset arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, decimal, void> fn, object o, decimal arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, double, void> fn, object o, double arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, float, void> fn, object o, float arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, Guid, void> fn, object o, Guid arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, short, void> fn, object o, short arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, int, void> fn, object o, int arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, long, void> fn, object o, long arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, nint, void> fn, object o, nint arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, nuint, void> fn, object o, nuint arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, object?, void> fn, object o, object? arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, sbyte, void> fn, object o, sbyte arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, ushort, void> fn, object o, ushort arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, uint, void> fn, object o, uint arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, ulong, void> fn, object o, ulong arg1) => fn(o, arg1);

        // Other methods:

        [Intrinsic]
        internal static void Call(delegate*<object, object?, object?, void> fn, object o, object? arg1, object? arg2)
            => fn(o, arg1, arg2);

        [Intrinsic]
        internal static void Call(delegate*<object, object?, object?, object?, void> fn, object o, object? arg1, object? arg2, object? arg3)
            => fn(o, arg1, arg2, arg3);

        [Intrinsic]
        internal static void Call(delegate*<object, object?, object?, object?, object?, void> fn, object o, object? arg1, object? arg2, object? arg3, object? arg4)
            => fn(o, arg1, arg2, arg3, arg4);

        [Intrinsic]
        internal static void Call(delegate*<object, object?, object?, object?, object?, object?, void> fn, object o, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5)
            => fn(o, arg1, arg2, arg3, arg4, arg5);

        [Intrinsic]
        internal static void Call(delegate*<object, object?, object?, object?, object?, object?, object?, void> fn, object o, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6)
            => fn(o, arg1, arg2, arg3, arg4, arg5, arg6);

        [Intrinsic]
        internal static void Call(delegate*<object, IEnumerable<object>?, void> fn, object o, IEnumerable<object>? arg1)
            => fn(o, arg1);

        [Intrinsic]
        internal static void Call(delegate*<object, IEnumerable<object>?, IEnumerable<object>?, void> fn, object o, IEnumerable<object>? arg1, IEnumerable<object>? arg2)
            => fn(o, arg1, arg2);
    }
}
