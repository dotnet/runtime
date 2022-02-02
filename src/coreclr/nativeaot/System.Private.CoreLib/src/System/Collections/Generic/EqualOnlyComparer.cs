// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace System.Collections.Generic
{
    internal static class EqualOnlyComparerHelper
    {
        public static bool Equals(sbyte x, sbyte y)
        {
            return x == y;
        }

        public static bool Equals(byte x, byte y)
        {
            return x == y;
        }

        public static bool Equals(short x, short y)
        {
            return x == y;
        }

        public static bool Equals(ushort x, ushort y)
        {
            return x == y;
        }

        public static bool Equals(int x, int y)
        {
            return x == y;
        }

        public static bool Equals(uint x, uint y)
        {
            return x == y;
        }

        public static bool Equals(long x, long y)
        {
            return x == y;
        }

        public static bool Equals(ulong x, ulong y)
        {
            return x == y;
        }

        public static bool Equals(IntPtr x, IntPtr y)
        {
            return x == y;
        }

        public static bool Equals(UIntPtr x, UIntPtr y)
        {
            return x == y;
        }

        public static bool Equals(float x, float y)
        {
            return x == y;
        }

        public static bool Equals(double x, double y)
        {
            return x == y;
        }

        public static bool Equals(decimal x, decimal y)
        {
            return x == y;
        }

        public static bool Equals(string? x, string? y)
        {
            return x == y;
        }
    }

    /// <summary>
    /// Minimum comparer for Array.IndexOf/Contains which each Array needs. So it's important to be small.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class EqualOnlyComparer<T>
    {
        // Force the compiler to inline this method. Normally the compiler will shy away from inlining such
        // a large function, however in this case the method compiles down to almost nothing so help the
        // compiler out a bit with this hint. Once the compiler supports bottom-up codegen analysis it should
        // inline this without a hint.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool Equals(T x, T y)
        {
            // Specialized Comparers
            if (typeof(T) == typeof(sbyte))
                return EqualOnlyComparerHelper.Equals(((sbyte)(object)(x!)), ((sbyte)(object)(y!)));
            else if (typeof(T) == typeof(byte))
                return EqualOnlyComparerHelper.Equals(((byte)(object)(x!)), ((byte)(object)(y!)));
            else if (typeof(T) == typeof(short))
                return EqualOnlyComparerHelper.Equals(((short)(object)(x!)), ((short)(object)(y!)));
            else if (typeof(T) == typeof(ushort))
                return EqualOnlyComparerHelper.Equals(((ushort)(object)(x!)), ((ushort)(object)(y!)));
            else if (typeof(T) == typeof(int))
                return EqualOnlyComparerHelper.Equals(((int)(object)(x!)), ((int)(object)(y!)));
            else if (typeof(T) == typeof(uint))
                return EqualOnlyComparerHelper.Equals(((uint)(object)(x!)), ((uint)(object)(y!)));
            else if (typeof(T) == typeof(long))
                return EqualOnlyComparerHelper.Equals(((long)(object)(x!)), ((long)(object)(y!)));
            else if (typeof(T) == typeof(ulong))
                return EqualOnlyComparerHelper.Equals(((ulong)(object)(x!)), ((ulong)(object)(y!)));
            else if (typeof(T) == typeof(System.IntPtr))
                return EqualOnlyComparerHelper.Equals(((System.IntPtr)(object)(x!)), ((System.IntPtr)(object)(y!)));
            else if (typeof(T) == typeof(System.UIntPtr))
                return EqualOnlyComparerHelper.Equals(((System.UIntPtr)(object)(x!)), ((System.UIntPtr)(object)(y!)));
            else if (typeof(T) == typeof(float))
                return EqualOnlyComparerHelper.Equals(((float)(object)(x!)), ((float)(object)(y!)));
            else if (typeof(T) == typeof(double))
                return EqualOnlyComparerHelper.Equals(((double)(object)(x!)), ((double)(object)(y!)));
            else if (typeof(T) == typeof(decimal))
                return EqualOnlyComparerHelper.Equals(((decimal)(object)(x!)), ((decimal)(object)(y!)));
            else if (typeof(T) == typeof(string))
                return EqualOnlyComparerHelper.Equals(((string?)(object?)(x)), ((string?)(object?)(y)));

            // Default Comparer

            if (x == null)
            {
                return y == null;
            }

            if (y == null)
            {
                return false;
            }

            return x.Equals(y);
        }
    }
}
