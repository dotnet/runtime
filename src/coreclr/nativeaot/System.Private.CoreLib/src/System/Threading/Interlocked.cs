// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

using Internal.Runtime.CompilerServices;

namespace System.Threading
{
    public static partial class Interlocked
    {
        #region CompareExchange

        [Intrinsic]
        public static int CompareExchange(ref int location1, int value, int comparand)
        {
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
        }

        [Intrinsic]
        public static long CompareExchange(ref long location1, long value, long comparand)
        {
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
        }

        [Intrinsic]
        public static unsafe float CompareExchange(ref float location1, float value, float comparand)
        {
            float ret;
            *(int*)&ret = CompareExchange(ref Unsafe.As<float, int>(ref location1), *(int*)&value, *(int*)&comparand);
            return ret;
        }

        [Intrinsic]
        public static unsafe double CompareExchange(ref double location1, double value, double comparand)
        {
            double ret;
            *(long*)&ret = CompareExchange(ref Unsafe.As<double, long>(ref location1), *(long*)&value, *(long*)&comparand);
            return ret;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(location1))]
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class?
        {
            return Unsafe.As<T>(RuntimeImports.InterlockedCompareExchange(ref Unsafe.As<T, object?>(ref location1), value, comparand));
        }

        [Intrinsic]
        [return: NotNullIfNotNull(nameof(location1))]
        public static object? CompareExchange(ref object? location1, object? value, object? comparand)
        {
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
        }

        #endregion

        #region Exchange

        [Intrinsic]
        public static int Exchange(ref int location1, int value)
        {
            int oldValue;

            do
            {
                oldValue = location1;
            } while (CompareExchange(ref location1, value, oldValue) != oldValue);

            return oldValue;
        }

        [Intrinsic]
        public static long Exchange(ref long location1, long value)
        {
            long oldValue;

            do
            {
                oldValue = location1;
            } while (CompareExchange(ref location1, value, oldValue) != oldValue);

            return oldValue;
        }

        [Intrinsic]
        public static unsafe float Exchange(ref float location1, float value)
        {
            float ret;
            *(int*)&ret = Exchange(ref Unsafe.As<float, int>(ref location1), *(int*)&value);
            return ret;
        }

        [Intrinsic]
        public static unsafe double Exchange(ref double location1, double value)
        {
            double ret;
            *(long*)&ret = Exchange(ref Unsafe.As<double, long>(ref location1), *(long*)&value);
            return ret;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(location1))]
        public static T Exchange<T>([NotNullIfNotNull(nameof(value))] ref T location1, T value) where T : class?
        {
            return Unsafe.As<T>(RuntimeImports.InterlockedExchange(ref Unsafe.As<T, object?>(ref location1), value));
        }

        [Intrinsic]
        [return: NotNullIfNotNull(nameof(location1))]
        public static object? Exchange([NotNullIfNotNull(nameof(value))] ref object? location1, object? value)
        {
            return RuntimeImports.InterlockedExchange(ref location1, value);
        }

        #endregion

        #region Increment

        [Intrinsic]
        public static int Increment(ref int location)
        {
            return ExchangeAdd(ref location, 1) + 1;
        }

        [Intrinsic]
        public static long Increment(ref long location)
        {
            return ExchangeAdd(ref location, 1) + 1;
        }

        #endregion

        #region Decrement

        [Intrinsic]
        public static int Decrement(ref int location)
        {
            return ExchangeAdd(ref location, -1) - 1;
        }

        [Intrinsic]
        public static long Decrement(ref long location)
        {
            return ExchangeAdd(ref location, -1) - 1;
        }

        #endregion

        #region Add

        [Intrinsic]
        public static int Add(ref int location1, int value)
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        [Intrinsic]
        public static long Add(ref long location1, long value)
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ExchangeAdd(ref int location1, int value)
        {
            int oldValue;

            do
            {
                oldValue = location1;
            } while (CompareExchange(ref location1, oldValue + value, oldValue) != oldValue);

            return oldValue;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ExchangeAdd(ref long location1, long value)
        {
            long oldValue;

            do
            {
                oldValue = location1;
            } while (CompareExchange(ref location1, oldValue + value, oldValue) != oldValue);

            return oldValue;
        }

        #endregion

        #region MemoryBarrier
        [Intrinsic]
        public static void MemoryBarrier()
        {
            RuntimeImports.MemoryBarrier();
        }
        #endregion

        #region Read
        public static long Read(ref long location)
        {
            return CompareExchange(ref location, 0, 0);
        }
        #endregion

        public static void MemoryBarrierProcessWide()
        {
            RuntimeImports.RhFlushProcessWriteBuffers();
        }
    }
}
