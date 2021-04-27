// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
#if TARGET_64BIT
using nuint_t = System.UInt64;
#else
using nuint_t = System.UInt32;
#endif

namespace System
{
    [Serializable]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct UIntPtr : IEquatable<UIntPtr>, IComparable, IComparable<UIntPtr>, ISpanFormattable, ISerializable
    {
        private readonly unsafe void* _value; // Do not rename (binary serialization)

        [Intrinsic]
        public static readonly UIntPtr Zero;

        [NonVersionable]
        public unsafe UIntPtr(uint value)
        {
            _value = (void*)value;
        }

        [NonVersionable]
        public unsafe UIntPtr(ulong value)
        {
#if TARGET_64BIT
            _value = (void*)value;
#else
            _value = (void*)checked((uint)value);
#endif
        }

        [NonVersionable]
        public unsafe UIntPtr(void* value)
        {
            _value = value;
        }

        private unsafe UIntPtr(SerializationInfo info, StreamingContext context)
        {
            ulong l = info.GetUInt64("value");

            if (Size == 4 && l > uint.MaxValue)
                throw new ArgumentException(SR.Serialization_InvalidPtrValue);

            _value = (void*)l;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("value", ToUInt64());
        }

        public override unsafe bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is UIntPtr)
            {
                return _value == ((UIntPtr)obj)._value;
            }
            return false;
        }

        public override unsafe int GetHashCode()
        {
#if TARGET_64BIT
            ulong l = (ulong)_value;
            return unchecked((int)l) ^ (int)(l >> 32);
#else
            return unchecked((int)_value);
#endif
        }

        [NonVersionable]
        public unsafe uint ToUInt32()
        {
#if TARGET_64BIT
            return checked((uint)_value);
#else
            return (uint)_value;
#endif
        }

        [NonVersionable]
        public unsafe ulong ToUInt64() => (ulong)_value;

        [NonVersionable]
        public static explicit operator UIntPtr(uint value) =>
            new UIntPtr(value);

        [NonVersionable]
        public static explicit operator UIntPtr(ulong value) =>
            new UIntPtr(value);

        [NonVersionable]
        public static unsafe explicit operator UIntPtr(void* value) =>
            new UIntPtr(value);

        [NonVersionable]
        public static unsafe explicit operator void*(UIntPtr value) =>
            value._value;

        [NonVersionable]
        public static unsafe explicit operator uint(UIntPtr value) =>
#if TARGET_64BIT
            checked((uint)value._value);
#else
            (uint)value._value;
#endif

        [NonVersionable]
        public static unsafe explicit operator ulong(UIntPtr value) =>
            (ulong)value._value;

        [NonVersionable]
        public static unsafe bool operator ==(UIntPtr value1, UIntPtr value2) =>
            value1._value == value2._value;

        [NonVersionable]
        public static unsafe bool operator !=(UIntPtr value1, UIntPtr value2) =>
            value1._value != value2._value;

        [NonVersionable]
        public static UIntPtr Add(UIntPtr pointer, int offset) =>
            pointer + offset;

        [NonVersionable]
        public static unsafe UIntPtr operator +(UIntPtr pointer, int offset) =>
            (nuint)pointer._value + (nuint)offset;

        [NonVersionable]
        public static UIntPtr Subtract(UIntPtr pointer, int offset) =>
            pointer - offset;

        [NonVersionable]
        public static unsafe UIntPtr operator -(UIntPtr pointer, int offset) =>
            (nuint)pointer._value - (nuint)offset;

        public static int Size
        {
            [NonVersionable]
            get => sizeof(nuint_t);
        }

        [NonVersionable]
        public unsafe void* ToPointer() => _value;

        public static UIntPtr MaxValue
        {
            [NonVersionable]
            get => (UIntPtr)nuint_t.MaxValue;
        }

        public static UIntPtr MinValue
        {
            [NonVersionable]
            get => (UIntPtr)nuint_t.MinValue;
        }

        public unsafe int CompareTo(object? value)
        {
            if (value is null)
            {
                return 1;
            }
            if (value is nuint i)
            {
                if ((nuint)_value < i) return -1;
                if ((nuint)_value > i) return 1;
                return 0;
            }

            throw new ArgumentException(SR.Arg_MustBeUIntPtr);
        }

        public unsafe int CompareTo(UIntPtr value) => ((nuint_t)_value).CompareTo((nuint_t)value);

        [NonVersionable]
        public unsafe bool Equals(UIntPtr other) => (nuint)_value == (nuint)other;

        public unsafe override string ToString() => ((nuint_t)_value).ToString();
        public unsafe string ToString(string? format) => ((nuint_t)_value).ToString(format);
        public unsafe string ToString(IFormatProvider? provider) => ((nuint_t)_value).ToString(provider);
        public unsafe string ToString(string? format, IFormatProvider? provider) => ((nuint_t)_value).ToString(format, provider);

        public unsafe bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            ((nuint_t)_value).TryFormat(destination, out charsWritten, format, provider);

        public static UIntPtr Parse(string s) => (UIntPtr)nuint_t.Parse(s);
        public static UIntPtr Parse(string s, NumberStyles style) => (UIntPtr)nuint_t.Parse(s, style);
        public static UIntPtr Parse(string s, IFormatProvider? provider) => (UIntPtr)nuint_t.Parse(s, provider);
        public static UIntPtr Parse(string s, NumberStyles style, IFormatProvider? provider) => (UIntPtr)nuint_t.Parse(s, style, provider);
        public static UIntPtr Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null) => (UIntPtr)nuint_t.Parse(s, style, provider);

        public static bool TryParse([NotNullWhen(true)] string? s, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, style, provider, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, style, provider, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }
    }
}
