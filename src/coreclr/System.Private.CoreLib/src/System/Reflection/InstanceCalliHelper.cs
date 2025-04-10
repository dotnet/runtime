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
        internal static bool Call(object o, delegate*<object, bool> fn) => fn(o);

        [Intrinsic]
        internal static byte Call(object o, delegate*<object, byte> fn) => fn(o);

        [Intrinsic]
        internal static char Call(object o, delegate*<object, char> fn) => fn(o);

        [Intrinsic]
        internal static DateTime Call(object o, delegate*<object, DateTime> fn) => fn(o);

        [Intrinsic]
        internal static DateTimeOffset Call(object o, delegate*<object, DateTimeOffset> fn) => fn(o);

        [Intrinsic]
        internal static decimal Call(object o, delegate*<object, decimal> fn) => fn(o);

        [Intrinsic]
        internal static double Call(object o, delegate*<object, double> fn) => fn(o);

        [Intrinsic]
        internal static float Call(object o, delegate*<object, float> fn) => fn(o);

        [Intrinsic]
        internal static Guid Call(object o, delegate*<object, Guid> fn) => fn(o);

        [Intrinsic]
        internal static short Call(object o, delegate*<object, short> fn) => fn(o);

        [Intrinsic]
        internal static int Call(object o, delegate*<object, int> fn) => fn(o);

        [Intrinsic]
        internal static long Call(object o, delegate*<object, long> fn) => fn(o);

        [Intrinsic]
        internal static nint Call(object o, delegate*<object, nint> fn) => fn(o);

        [Intrinsic]
        internal static nuint Call(object o, delegate*<object, nuint> fn) => fn(o);

        [Intrinsic]
        internal static object? Call(object o, delegate*<object, object?> fn) => fn(o);

        [Intrinsic]
        internal static sbyte Call(object o, delegate*<object, sbyte> fn) => fn(o);

        [Intrinsic]
        internal static ushort Call(object o, delegate*<object, ushort> fn) => fn(o);

        [Intrinsic]
        internal static uint Call(object o, delegate*<object, uint> fn) => fn(o);

        [Intrinsic]
        internal static ulong Call(object o, delegate*<object, ulong> fn) => fn(o);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, void> fn) => fn(o);


        // One parameter methods with no return such as property setters:

        [Intrinsic]
        internal static void Call(object o, delegate*<object, bool, void> fn, bool arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, byte, void> fn, byte arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, char, void> fn, char arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, DateTime, void> fn, DateTime arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, DateTimeOffset, void> fn, DateTimeOffset arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, decimal, void> fn, decimal arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, double, void> fn, double arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, float, void> fn, float arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, Guid, void> fn, Guid arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, short, void> fn, short arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, int, void> fn, int arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, long, void> fn, long arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, nint, void> fn, nint arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, nuint, void> fn, nuint arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, object?, void> fn, object? arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, sbyte, void> fn, sbyte arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, ushort, void> fn, ushort arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, uint, void> fn, uint arg1) => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, ulong, void> fn, ulong arg1) => fn(o, arg1);

        // Other methods:

        [Intrinsic]
        internal static void Call(object o, delegate*<object, object?, object?, void> fn, object? arg1, object? arg2)
            => fn(o, arg1, arg2);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, object?, object?, object?, void> fn, object? arg1, object? arg2, object? arg3)
            => fn(o, arg1, arg2, arg3);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, object?, object?, object?, object?, void> fn, object? arg1, object? arg2, object? arg3, object? arg4)
            => fn(o, arg1, arg2, arg3, arg4);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, object?, object?, object?, object?, object?, void> fn, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5)
            => fn(o, arg1, arg2, arg3, arg4, arg5);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, object?, object?, object?, object?, object?, object?, void> fn, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6)
            => fn(o, arg1, arg2, arg3, arg4, arg5, arg6);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, IEnumerable<object>?, void> fn, IEnumerable<object>? arg1)
            => fn(o, arg1);

        [Intrinsic]
        internal static void Call(object o, delegate*<object, IEnumerable<object>?, IEnumerable<object>?, void> fn, IEnumerable<object>? arg1, IEnumerable<object>? arg2)
            => fn(o, arg1, arg2);
    }
}
