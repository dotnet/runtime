// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    // The use of [MethodImpl(MethodImplOptions.NoInlining)] is temporary so current tests can call them.
    internal static unsafe class InvokeHelpers
    {
        // Zero parameter methods with a return value such as property getters.
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static bool InvokeObjectBool(object o, delegate*<object, bool> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static byte InvokeObjectByte(object o, delegate*<object, byte> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static char InvokeObjectChar(object o, delegate*<object, char> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static DateTime InvokeObjectDateTime(object o, delegate*<object, DateTime> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static DateTimeOffset InvokeObjectDateTimeOffset(object o, delegate*<object, DateTimeOffset> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static decimal InvokeObjectDecimal(object o, delegate*<object, decimal> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static double InvokeObjectDouble(object o, delegate*<object, double> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static float InvokeObjectSingle(object o, delegate*<object, float> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static Guid InvokeObjectGuid(object o, delegate*<object, Guid> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static short InvokeObjectInt16(object o, delegate*<object, short> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static int InvokeObjectInt32(object o, delegate*<object, int> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static long InvokeObjectInt64(object o, delegate*<object, long> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static nint InvokeObjectNInt(object o, delegate*<object, nint> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static nuint InvokeObjectNUInt(object o, delegate*<object, nuint> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static object? InvokeObjectObject(object o, delegate*<object, object?> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static sbyte InvokeObjectSByte(object o, delegate*<object, sbyte> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static ushort InvokeObjectUInt16(object o, delegate*<object, ushort> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static uint InvokeObjectUInt32(object o, delegate*<object, uint> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static ulong InvokeObjectUInt64(object o, delegate*<object, ulong> pfn) => pfn(o);
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectVoid(object o, delegate*<object, void> pfn) => pfn(o);

        // One parameter methods with no return such as property setters.
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectBoolVoid(object o, delegate*<object, bool, void> pfn, bool arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectByteVoid(object o, delegate*<object, byte, void> pfn, byte arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectCharVoid(object o, delegate*<object, char, void> pfn, char arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectDateTimeVoid(object o, delegate*<object, DateTime, void> pfn, DateTime arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectDateTimeOffsetVoid(object o, delegate*<object, DateTimeOffset, void> pfn, DateTimeOffset arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectDecimalVoid(object o, delegate*<object, decimal, void> pfn, decimal arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectDoubleVoid(object o, delegate*<object, double, void> pfn, double arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectSingleVoid(object o, delegate*<object, float, void> pfn, float arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectGuidVoid(object o, delegate*<object, Guid, void> pfn, Guid arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectInt16Void(object o, delegate*<object, short, void> pfn, short arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectInt32Void(object o, delegate*<object, int, void> pfn, int arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectInt64Void(object o, delegate*<object, long, void> pfn, long arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectNIntVoid(object o, delegate*<object, nint, void> pfn, nint arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectNUIntVoid(object o, delegate*<object, nuint, void> pfn, nuint arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectObjectVoid(object o, delegate*<object, object?, void> pfn, object? arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectSByteVoid(object o, delegate*<object, sbyte, void> pfn, sbyte arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectUInt16Void(object o, delegate*<object, ushort, void> pfn, ushort arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectUInt32Void(object o, delegate*<object, uint, void> pfn, uint arg1) { pfn(o, arg1); }
        [Intrinsic][MethodImpl(MethodImplOptions.NoInlining)] internal static void InvokeObjectUInt64Void(object o, delegate*<object, ulong, void> pfn, ulong arg1) { pfn(o, arg1); }

        // Other methods.
        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvokeObjectObjectObjectVoid(object o, delegate*<object, object?, object?, void> pfn, object? arg1, object? arg2)
        {
            pfn(o, arg1, arg2);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvokeObjectObjectObjectObjectVoid(object o, delegate*<object, object?, object?, object?, void> pfn, object? arg1, object? arg2, object? arg3)
        {
            pfn(o, arg1, arg2, arg3);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvokeObjectObjectObjectObjectObjectVoid(object o, delegate*<object, object?, object?, object?, object?, void> pfn, object? arg1, object? arg2, object? arg3, object? arg4)
        {
            pfn(o, arg1, arg2, arg3, arg4);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvokeObjectObjectObjectObjectObjectObjectVoid(object o, delegate*<object, object?, object?, object?, object?, object?, void> pfn, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5)
        {
            pfn(o, arg1, arg2, arg3, arg4, arg5);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvokeObjectObjectObjectObjectObjectObjectObjectVoid(object o, delegate*<object, object?, object?, object?, object?, object?, object?, void> pfn, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6)
        {
            pfn(o, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvokeObjectIEnumerableOfObjectVoid(object o, delegate*<object, IEnumerable<object>?, void> pfn, IEnumerable<object>? arg1)
        {
            pfn(o, arg1);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvokeObjectIEnumerableOfObjectIEnumerableOfObjectVoid(object o, delegate*<object, IEnumerable<object>?, IEnumerable<object>?, void> pfn, IEnumerable<object> arg1, IEnumerable<object>? arg2)
        {
            pfn(o, arg1, arg2);
        }

        // We temporarily need to make the methods non-trimmable and called so R2R includes them.
        // Once the intrinsics are used by reflection, we don't need to do this.
        internal static unsafe void KeepIntrinsics()
        {
            InvokeObjectBool(null!, default);
            InvokeObjectByte(null!, default);
            InvokeObjectChar(null!, default);
            InvokeObjectDateTime(null!, default);
            InvokeObjectDateTimeOffset(null!, default);
            InvokeObjectDecimal(null!, default);
            InvokeObjectDouble(null!, default);
            InvokeObjectSingle(null!, default);
            InvokeObjectGuid(null!, default);
            InvokeObjectInt16(null!, default);
            InvokeObjectInt32(null!, default);
            InvokeObjectInt64(null!, default);
            InvokeObjectNInt(null!, default);
            InvokeObjectNUInt(null!, default);
            InvokeObjectObject(null!, default);
            InvokeObjectSByte(null!, default);
            InvokeObjectUInt16(null!, default);
            InvokeObjectUInt32(null!, default);
            InvokeObjectUInt64(null!, default);
            InvokeObjectVoid(null!, default);

            InvokeObjectBoolVoid(null!, default, default);
            InvokeObjectByteVoid(null!, default, default);
            InvokeObjectCharVoid(null!, default, default);
            InvokeObjectDateTimeVoid(null!, default, default);
            InvokeObjectDateTimeOffsetVoid(null!, default, default);
            InvokeObjectDecimalVoid(null!, default, default);
            InvokeObjectDoubleVoid(null!, default, default);
            InvokeObjectSingleVoid(null!, default, default);
            InvokeObjectGuidVoid(null!, default, default);
            InvokeObjectInt16Void(null!, default, default);
            InvokeObjectInt32Void(null!, default, default);
            InvokeObjectInt64Void(null!, default, default);
            InvokeObjectNIntVoid(null!, default, default);
            InvokeObjectNUIntVoid(null!, default, default);
            InvokeObjectObjectVoid(null!, default, null);
            InvokeObjectSByteVoid(null!, default, default);
            InvokeObjectUInt16Void(null!, default, default);
            InvokeObjectUInt32Void(null!, default, default);
            InvokeObjectUInt64Void(null!, default, default);

            InvokeObjectObjectObjectVoid(null!, default, null, null);
            InvokeObjectObjectObjectObjectVoid(null!, default, null, null, null);
            InvokeObjectObjectObjectObjectObjectVoid(null!, default, null, null, null, null);
            InvokeObjectObjectObjectObjectObjectObjectVoid(null!, default, null, null, null, null, null);
            InvokeObjectObjectObjectObjectObjectObjectObjectVoid(null!, default, null, null, null, null, null, null);
            InvokeObjectIEnumerableOfObjectVoid(null!, default, Array.Empty<object>());
            InvokeObjectIEnumerableOfObjectIEnumerableOfObjectVoid(null!, default, Array.Empty<object>(), Array.Empty<object>());
        }
    }
}
