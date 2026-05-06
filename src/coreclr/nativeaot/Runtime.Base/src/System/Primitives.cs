// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains the basic primitive type definitions (int etc)
// These types are well known to the compiler and the runtime and are basic interchange types that do not change

// CONTRACT with Runtime
// Each of the data types has a data contract with the runtime. See the contract in the type definition
//

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System
{
    // CONTRACT with Runtime
    // Place holder type for type hierarchy, Compiler/Runtime requires this class
    public abstract class ValueType
    {
    }

    // CONTRACT with Runtime, Compiler/Runtime requires this class
    // Place holder type for type hierarchy
    public abstract class Enum : ValueType
    {
    }

    /*============================================================
    **
    ** Class:  Boolean
    **
    **
    ** Purpose: The boolean class serves as a wrapper for the primitive
    ** type boolean.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The Boolean type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type bool

    public struct Boolean
    {
        // Disable compile warning about unused _value field
#pragma warning disable 0169
        private bool _value;
#pragma warning restore 0169
    }


    /*============================================================
    **
    ** Class:  Char
    **
    **
    ** Purpose: This is the value class representing a Unicode character
    **
    **
    ===========================================================*/


    // CONTRACT with Runtime
    // The Char type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type char
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct Char
    {
        private char _value;

        public const char MaxValue = (char)0xFFFF;
        public const char MinValue = (char)0x00;
    }


    /*============================================================
    **
    ** Class:  SByte
    **
    **
    ** Purpose: A representation of a 8 bit 2's complement integer.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The SByte type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type sbyte
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct SByte
    {
        private sbyte _value;

        public const sbyte MaxValue = (sbyte)0x7F;
        public const sbyte MinValue = unchecked((sbyte)0x80);
    }


    /*============================================================
    **
    ** Class:  Byte
    **
    **
    ** Purpose: A representation of a 8 bit integer (byte)
    **
    **
    ===========================================================*/


    // CONTRACT with Runtime
    // The Byte type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type bool
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct Byte
    {
        private byte _value;

        public const byte MaxValue = (byte)0xFF;
        public const byte MinValue = 0;
    }


    /*============================================================
    **
    ** Class:  Int16
    **
    **
    ** Purpose: A representation of a 16 bit 2's complement integer.
    **
    **
    ===========================================================*/


    // CONTRACT with Runtime
    // The Int16 type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type short
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct Int16
    {
        private short _value;

        public const short MaxValue = (short)0x7FFF;
        public const short MinValue = unchecked((short)0x8000);
    }

    /*============================================================
    **
    ** Class:  UInt16
    **
    **
    ** Purpose: A representation of a short (unsigned 16-bit) integer.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The Uint16 type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type ushort
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct UInt16
    {
        private ushort _value;

        public const ushort MaxValue = (ushort)0xffff;
        public const ushort MinValue = 0;
    }

    /*============================================================
    **
    ** Class:  Int32
    **
    **
    ** Purpose: A representation of a 32 bit 2's complement integer.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The Int32 type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type int
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct Int32
    {
        private int _value;

        public const int MaxValue = 0x7fffffff;
        public const int MinValue = unchecked((int)0x80000000);
    }


    /*============================================================
    **
    ** Class:  UInt32
    **
    **
    ** Purpose: A representation of a 32 bit unsigned integer.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The Uint32 type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type uint
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct UInt32
    {
        private uint _value;

        public const uint MaxValue = (uint)0xffffffff;
        public const uint MinValue = 0;
    }


    /*============================================================
    **
    ** Class:  Int64
    **
    **
    ** Purpose: A representation of a 64 bit 2's complement integer.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The Int64 type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type long
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct Int64
    {
        private long _value;

        public const long MaxValue = 0x7fffffffffffffffL;
        public const long MinValue = unchecked((long)0x8000000000000000L);
    }


    /*============================================================
    **
    ** Class:  UInt64
    **
    **
    ** Purpose: A representation of a 64 bit unsigned integer.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The UInt64 type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type ulong
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct UInt64
    {
        private ulong _value;

        public const ulong MaxValue = (ulong)0xffffffffffffffffL;
        public const ulong MinValue = 0;
    }


    /*============================================================
    **
    ** Class:  Single
    **
    **
    ** Purpose: A wrapper class for the primitive type float.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The Single type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type float
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct Single
    {
        private float _value;
    }


    /*============================================================
    **
    ** Class:  Double
    **
    **
    ** Purpose: A representation of an IEEE double precision
    **          floating point number.
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The Double type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type double
    // This type is LayoutKind Sequential

    [StructLayout(LayoutKind.Sequential)]
    public struct Double
    {
        private double _value;
    }


    /*============================================================
    **
    ** Class:  IntPtr
    **
    **
    ** Purpose: Platform independent integer
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The IntPtr type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type void *

    // This type implements == without overriding GetHashCode, Equals, disable compiler warning
#pragma warning disable 0660, 0661
    public struct IntPtr
    {
        private unsafe void* _value; // The compiler treats void* closest to uint hence explicit casts are required to preserve int behavior

        [Intrinsic]
        public static readonly IntPtr Zero;

        public static unsafe int Size
        {
            [Intrinsic]
            get
            {
#if TARGET_64BIT
                return 8;
#else
                return 4;
#endif
            }
        }

        [Intrinsic]
        public unsafe IntPtr(void* value)
        {
            _value = value;
        }

        [Intrinsic]
        public unsafe IntPtr(int value)
        {
            _value = (void*)value;
        }

        [Intrinsic]
        public unsafe IntPtr(long value)
        {
            _value = (void*)value;
        }

        [Intrinsic]
        public unsafe long ToInt64()
        {
#if TARGET_64BIT
            return (long)_value;
#else
            return (long)(int)_value;
#endif
        }

        [Intrinsic]
        public static unsafe explicit operator IntPtr(int value)
        {
            return new IntPtr(value);
        }

        [Intrinsic]
        public static unsafe explicit operator IntPtr(long value)
        {
            return new IntPtr(value);
        }

        [Intrinsic]
        public static unsafe explicit operator IntPtr(void* value)
        {
            return new IntPtr(value);
        }

        [Intrinsic]
        public static unsafe explicit operator void* (IntPtr value)
        {
            return value._value;
        }

        [Intrinsic]
        public static unsafe explicit operator int(IntPtr value)
        {
            return unchecked((int)value._value);
        }

        [Intrinsic]
        public static unsafe explicit operator long(IntPtr value)
        {
            return unchecked((long)value._value);
        }

        [Intrinsic]
        public static unsafe bool operator ==(IntPtr value1, IntPtr value2)
        {
            return value1._value == value2._value;
        }

        [Intrinsic]
        public static unsafe bool operator !=(IntPtr value1, IntPtr value2)
        {
            return value1._value != value2._value;
        }

        [Intrinsic]
        public static unsafe IntPtr operator +(IntPtr pointer, int offset)
        {
#if TARGET_64BIT
            return new IntPtr((long)pointer._value + offset);
#else
            return new IntPtr((int)pointer._value + offset);
#endif
        }
    }
#pragma warning restore 0660, 0661


    /*============================================================
    **
    ** Class:  UIntPtr
    **
    **
    ** Purpose: Platform independent integer
    **
    **
    ===========================================================*/

    // CONTRACT with Runtime
    // The UIntPtr type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type void *

    // This type implements == without overriding GetHashCode, Equals, disable compiler warning
#pragma warning disable 0660, 0661
    public struct UIntPtr
    {
        private unsafe void* _value;

        [Intrinsic]
        public static readonly UIntPtr Zero;

        [Intrinsic]
        public unsafe UIntPtr(uint value)
        {
            _value = (void*)value;
        }

        [Intrinsic]
        public unsafe UIntPtr(ulong value)
        {
#if TARGET_64BIT
            _value = (void*)value;
#else
            _value = (void*)checked((uint)value);
#endif
        }

        [Intrinsic]
        public unsafe UIntPtr(void* value)
        {
            _value = value;
        }

        [Intrinsic]
        public static unsafe explicit operator UIntPtr(void* value)
        {
            return new UIntPtr(value);
        }

        [Intrinsic]
        public static unsafe explicit operator void* (UIntPtr value)
        {
            return value._value;
        }

        [Intrinsic]
        public static unsafe explicit operator uint (UIntPtr value)
        {
#if TARGET_64BIT
            return checked((uint)value._value);
#else
            return (uint)value._value;
#endif
        }

        [Intrinsic]
        public static unsafe explicit operator ulong (UIntPtr value)
        {
            return (ulong)value._value;
        }

        [Intrinsic]
        public static unsafe bool operator ==(UIntPtr value1, UIntPtr value2)
        {
            return value1._value == value2._value;
        }

        [Intrinsic]
        public static unsafe bool operator !=(UIntPtr value1, UIntPtr value2)
        {
            return value1._value != value2._value;
        }
    }
#pragma warning restore 0660, 0661
}
