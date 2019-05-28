// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
    /// <summary>
    /// After much discussion, we decided the Interlocked class doesn't need 
    /// any HPA's for synchronization or external threading.  They hurt C#'s 
    /// codegen for the yield keyword, and arguably they didn't protect much.  
    /// Instead, they penalized people (and compilers) for writing threadsafe 
    /// code.
    /// </summary>
    public static class Interlocked
    {
        /// <summary>
        /// Implemented: int, long
        /// </summary>
        public static int Increment(ref int location)
        {
            return Add(ref location, 1);
        }

        public static long Increment(ref long location)
        {
            return Add(ref location, 1);
        }

        /// <summary>
        /// Implemented: int, long
        /// </summary>
        public static int Decrement(ref int location)
        {
            return Add(ref location, -1);
        }

        public static long Decrement(ref long location)
        {
            return Add(ref location, -1);
        }

        /// <summary>
        /// Implemented: int, long, float, double, Object, IntPtr
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int Exchange(ref int location1, int value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long Exchange(ref long location1, long value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Exchange(ref float location1, float value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double Exchange(ref double location1, double value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [return: NotNullIfNotNull("value")]
        public static extern object? Exchange([NotNullIfNotNull("value")] ref object? location1, object? value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern IntPtr Exchange(ref IntPtr location1, IntPtr value);

        // This whole method reduces to a single call to Exchange(ref object, object) but
        // the JIT thinks that it will generate more native code than it actually does.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull("value")]
        public static T Exchange<T>([NotNullIfNotNull("value")] ref T location1, T value) where T : class?
        {
            return Unsafe.As<T>(Exchange(ref Unsafe.As<T, object?>(ref location1), value));
        }

        /// <summary>
        /// Implemented: int, long, float, double, Object, IntPtr
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int CompareExchange(ref int location1, int value, int comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long CompareExchange(ref long location1, long value, long comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float CompareExchange(ref float location1, float value, float comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double CompareExchange(ref double location1, double value, double comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern object? CompareExchange(ref object? location1, object? value, object? comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern IntPtr CompareExchange(ref IntPtr location1, IntPtr value, IntPtr comparand);

        // Note that getILIntrinsicImplementationForInterlocked() in vm\jitinterface.cpp replaces
        // the body of this method with the the following IL:
        //     ldarg.0 
        //     ldarg.1
        //     ldarg.2
        //     call System.Threading.Interlocked::CompareExchange(ref Object, Object, Object)
        //     ret
        // The workaround is no longer strictly necessary now that we have Unsafe.As but it does
        // have the advantage of being less sensitive to JIT's inliner decisions.
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class?
        {
            return Unsafe.As<T>(CompareExchange(ref Unsafe.As<T, object?>(ref location1), value, comparand));
        }

        // BCL-internal overload that returns success via a ref bool param, useful for reliable spin locks.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int CompareExchange(ref int location1, int value, int comparand, ref bool succeeded);

        /// <summary>
        /// Implemented: int, long
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int ExchangeAdd(ref int location1, int value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern long ExchangeAdd(ref long location1, long value);

        public static int Add(ref int location1, int value)
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        public static long Add(ref long location1, long value)
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        public static long Read(ref long location)
        {
            return Interlocked.CompareExchange(ref location, 0, 0);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void MemoryBarrier();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void _MemoryBarrierProcessWide();

        public static void MemoryBarrierProcessWide()
        {
            _MemoryBarrierProcessWide();
        }
    }
}
