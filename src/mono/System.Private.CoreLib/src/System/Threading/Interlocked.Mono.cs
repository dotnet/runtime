// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public static partial class Interlocked
    {
        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int CompareExchange(ref int location1, int value, int comparand);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void CompareExchange(ref object? location1, ref object? value, ref object? comparand, [NotNullIfNotNull(nameof(location1))] ref object? result);

        [Intrinsic]
        [return: NotNullIfNotNull(nameof(location1))]
        public static object? CompareExchange(ref object? location1, object? value, object? comparand)
        {
            // This avoids coop handles, esp. on the output which would be particularly inefficient.
            // Passing everything by ref is equivalent to coop handles -- ref to locals at least.
            //
            // location1's treatment is unclear. But note that passing it by handle would be incorrect,
            // as it would use a local alias, which the coop marshaling does, to avoid the unclarity here,
            // that of a ref being to a managed frame vs. a native frame. Perhaps that could be revisited.
            //
            // So there a hole here, that of calling this function with location1 being in a native frame.
            // Usually it will be to a field, static or not, and not even to managed stack.
            //
            // This is usually intrinsified. Ideally it is always intrinisified.
            //
            object? result = null;
            CompareExchange(ref location1, ref value, ref comparand, ref result);
            return result;
        }

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int Decrement(ref int location);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long Decrement(ref long location);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int Increment(ref int location);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long Increment(ref long location);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int Exchange(ref int location1, int value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void Exchange([NotNullIfNotNull(nameof(value))] ref object? location1, ref object? value, [NotNullIfNotNull(nameof(location1))] ref object? result);

        [return: NotNullIfNotNull(nameof(location1))]
        public static object? Exchange([NotNullIfNotNull(nameof(value))] ref object? location1, object? value)
        {
            // See CompareExchange(object) for comments.
            object? result = null;
            Exchange(ref location1, ref value, ref result);
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long CompareExchange(ref long location1, long value, long comparand);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long Exchange(ref long location1, long value);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long Read(ref long location);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int Add(ref int location1, int value);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long Add(ref long location1, long value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void MemoryBarrierProcessWide();
    }
}
