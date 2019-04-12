// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal class ConstantHelper
    {
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static byte GetByteWithAllBitsSet()
        {
            byte value = 0;
            unsafe
            {
                unchecked
                {
                    *((byte*)&value) = (byte)0xff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static sbyte GetSByteWithAllBitsSet()
        {
            sbyte value = 0;
            unsafe
            {
                unchecked
                {
                    *((sbyte*)&value) = (sbyte)0xff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ushort GetUInt16WithAllBitsSet()
        {
            ushort value = 0;
            unsafe
            {
                unchecked
                {
                    *((ushort*)&value) = (ushort)0xffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static short GetInt16WithAllBitsSet()
        {
            short value = 0;
            unsafe
            {
                unchecked
                {
                    *((short*)&value) = (short)0xffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt32WithAllBitsSet()
        {
            uint value = 0;
            unsafe
            {
                unchecked
                {
                    *((uint*)&value) = (uint)0xffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static int GetInt32WithAllBitsSet()
        {
            int value = 0;
            unsafe
            {
                unchecked
                {
                    *((int*)&value) = (int)0xffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ulong GetUInt64WithAllBitsSet()
        {
            ulong value = 0;
            unsafe
            {
                unchecked
                {
                    *((ulong*)&value) = (ulong)0xffffffffffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static long GetInt64WithAllBitsSet()
        {
            long value = 0;
            unsafe
            {
                unchecked
                {
                    *((long*)&value) = (long)0xffffffffffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static float GetSingleWithAllBitsSet()
        {
            float value = 0;
            unsafe
            {
                unchecked
                {
                    *((int*)&value) = (int)0xffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static double GetDoubleWithAllBitsSet()
        {
            double value = 0;
            unsafe
            {
                unchecked
                {
                    *((long*)&value) = (long)0xffffffffffffffff;
                }
            }
            return value;
        }
    }
}