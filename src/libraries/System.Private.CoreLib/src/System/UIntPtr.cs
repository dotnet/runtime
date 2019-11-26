// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
#if TARGET_64BIT
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
    [Serializable]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct UIntPtr : IEquatable<UIntPtr>, IComparable, IComparable<UIntPtr>, IFormattable, ISerializable
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

        public override unsafe bool Equals(object? obj)
        {
            if (obj is UIntPtr)
            {
                return _value == ((UIntPtr)obj)._value;
            }
            return false;
        }

        unsafe bool IEquatable<UIntPtr>.Equals(UIntPtr other) =>
            _value == other._value;

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
            new UIntPtr((nuint)pointer._value + (nuint)offset);

        [NonVersionable]
        public static UIntPtr Subtract(UIntPtr pointer, int offset) =>
            pointer - offset;

        [NonVersionable]
        public static unsafe UIntPtr operator -(UIntPtr pointer, int offset) =>
            new UIntPtr((nuint)pointer._value - (nuint)offset);

        public static int Size
        {
            [NonVersionable]
            get => sizeof(nuint);
        }

        [NonVersionable]
        public unsafe void* ToPointer() => _value;

        public static UIntPtr MaxValue => (UIntPtr)nuint.MaxValue;
        public static UIntPtr MinValue => (UIntPtr)nuint.MinValue;

        public int CompareTo(object? value) => ((nuint)this).CompareTo(value);

        public int CompareTo(UIntPtr value) => ((nuint)this).CompareTo((nuint)value);

        public bool Equals(UIntPtr other) => (nuint)this == (nuint)other;

        public override string ToString() => ((nuint)this).ToString(CultureInfo.InvariantCulture);
        public string ToString(string format) => ((nuint)this).ToString(format, CultureInfo.InvariantCulture);
        public string ToString(IFormatProvider provider) => ((nuint)this).ToString(provider);
        public string ToString(string format, IFormatProvider provider) => ((nuint)this).ToString(format, provider);

        public static UIntPtr Parse(string s) => (UIntPtr)nuint.Parse(s);
        public static UIntPtr Parse(string s, NumberStyles style) => (UIntPtr)nuint.Parse(s, style);
        public static UIntPtr Parse(string s, IFormatProvider? provider) => (UIntPtr)nuint.Parse(s, provider);
        public static UIntPtr Parse(string s, NumberStyles style, IFormatProvider? provider) => (UIntPtr)nuint.Parse(s, style, provider);

        public static bool TryParse(string? s, out UIntPtr result)
        {
            var res = nuint.TryParse(s, out var value);
            result = (UIntPtr)value;
            return res;
        }

        public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out UIntPtr result)
        {
            var res = nuint.TryParse(s, style, provider, out var value);
            result = (UIntPtr)value;
            return res;
        }
    }
}
