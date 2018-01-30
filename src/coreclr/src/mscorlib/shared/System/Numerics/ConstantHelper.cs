// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal class ConstantHelper
    {
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Byte GetByteWithAllBitsSet()
        {
            Byte value = 0;
            unsafe
            {
                unchecked
                {
                    *((Byte*)&value) = (Byte)0xff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static SByte GetSByteWithAllBitsSet()
        {
            SByte value = 0;
            unsafe
            {
                unchecked
                {
                    *((SByte*)&value) = (SByte)0xff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static UInt16 GetUInt16WithAllBitsSet()
        {
            UInt16 value = 0;
            unsafe
            {
                unchecked
                {
                    *((UInt16*)&value) = (UInt16)0xffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Int16 GetInt16WithAllBitsSet()
        {
            Int16 value = 0;
            unsafe
            {
                unchecked
                {
                    *((Int16*)&value) = (Int16)0xffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static UInt32 GetUInt32WithAllBitsSet()
        {
            UInt32 value = 0;
            unsafe
            {
                unchecked
                {
                    *((UInt32*)&value) = (UInt32)0xffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Int32 GetInt32WithAllBitsSet()
        {
            Int32 value = 0;
            unsafe
            {
                unchecked
                {
                    *((Int32*)&value) = (Int32)0xffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static UInt64 GetUInt64WithAllBitsSet()
        {
            UInt64 value = 0;
            unsafe
            {
                unchecked
                {
                    *((UInt64*)&value) = (UInt64)0xffffffffffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Int64 GetInt64WithAllBitsSet()
        {
            Int64 value = 0;
            unsafe
            {
                unchecked
                {
                    *((Int64*)&value) = (Int64)0xffffffffffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Single GetSingleWithAllBitsSet()
        {
            Single value = 0;
            unsafe
            {
                unchecked
                {
                    *((Int32*)&value) = (Int32)0xffffffff;
                }
            }
            return value;
        }
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Double GetDoubleWithAllBitsSet()
        {
            Double value = 0;
            unsafe
            {
                unchecked
                {
                    *((Int64*)&value) = (Int64)0xffffffffffffffff;
                }
            }
            return value;
        }
    }
}
