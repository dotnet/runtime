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
using nint_t = System.Int64;
#else
using nint_t = System.Int32;
#endif

namespace System
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct IntPtr : IEquatable<IntPtr>, IComparable, IComparable<IntPtr>, ISpanFormattable, ISerializable
    {
        // WARNING: We allow diagnostic tools to directly inspect this member (_value).
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details.
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools.
        // Get in touch with the diagnostics team if you have questions.
        private readonly unsafe void* _value; // Do not rename (binary serialization)

        [Intrinsic]
        public static readonly IntPtr Zero;

        [NonVersionable]
        public unsafe IntPtr(int value)
        {
            _value = (void*)value;
        }

        [NonVersionable]
        public unsafe IntPtr(long value)
        {
#if TARGET_64BIT
            _value = (void*)value;
#else
            _value = (void*)checked((int)value);
#endif
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public unsafe IntPtr(void* value)
        {
            _value = value;
        }

        private unsafe IntPtr(SerializationInfo info, StreamingContext context)
        {
            long l = info.GetInt64("value");

            if (Size == 4 && (l > int.MaxValue || l < int.MinValue))
                throw new ArgumentException(SR.Serialization_InvalidPtrValue);

            _value = (void*)l;
        }

        unsafe void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("value", ToInt64());
        }

        public override unsafe bool Equals([NotNullWhen(true)] object? obj) =>
            obj is IntPtr other &&
            _value == other._value;

        public override unsafe int GetHashCode()
        {
#if TARGET_64BIT
            long l = (long)_value;
            return unchecked((int)l) ^ (int)(l >> 32);
#else
            return unchecked((int)_value);
#endif
        }

        [NonVersionable]
        public unsafe int ToInt32()
        {
#if TARGET_64BIT
            long l = (long)_value;
            return checked((int)l);
#else
            return (int)_value;
#endif
        }

        [NonVersionable]
        public unsafe long ToInt64() =>
            (nint)_value;

        [NonVersionable]
        public static unsafe explicit operator IntPtr(int value) =>
            new IntPtr(value);

        [NonVersionable]
        public static unsafe explicit operator IntPtr(long value) =>
            new IntPtr(value);

        [CLSCompliant(false)]
        [NonVersionable]
        public static unsafe explicit operator IntPtr(void* value) =>
            new IntPtr(value);

        [CLSCompliant(false)]
        [NonVersionable]
        public static unsafe explicit operator void*(IntPtr value) =>
            value._value;

        [NonVersionable]
        public static unsafe explicit operator int(IntPtr value)
        {
#if TARGET_64BIT
            long l = (long)value._value;
            return checked((int)l);
#else
            return (int)value._value;
#endif
        }

        [NonVersionable]
        public static unsafe explicit operator long(IntPtr value) =>
            (nint)value._value;

        [NonVersionable]
        public static unsafe bool operator ==(IntPtr value1, IntPtr value2) =>
            value1._value == value2._value;

        [NonVersionable]
        public static unsafe bool operator !=(IntPtr value1, IntPtr value2) =>
            value1._value != value2._value;

        [NonVersionable]
        public static IntPtr Add(IntPtr pointer, int offset) =>
            pointer + offset;

        [NonVersionable]
        public static unsafe IntPtr operator +(IntPtr pointer, int offset) =>
            (nint)pointer._value + offset;

        [NonVersionable]
        public static IntPtr Subtract(IntPtr pointer, int offset) =>
            pointer - offset;

        [NonVersionable]
        public static unsafe IntPtr operator -(IntPtr pointer, int offset) =>
            (nint)pointer._value - offset;

        public static int Size
        {
            [NonVersionable]
            get => sizeof(nint_t);
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public unsafe void* ToPointer() => _value;

        public static IntPtr MaxValue
        {
            [NonVersionable]
            get => (IntPtr)nint_t.MaxValue;
        }

        public static IntPtr MinValue
        {
            [NonVersionable]
            get => (IntPtr)nint_t.MinValue;
        }

        // Don't just delegate to nint_t.CompareTo as it needs to throw when not IntPtr
        public unsafe int CompareTo(object? value)
        {
            if (value is null)
            {
                return 1;
            }
            if (value is nint i)
            {
                if ((nint)_value < i) return -1;
                if ((nint)_value > i) return 1;
                return 0;
            }

            throw new ArgumentException(SR.Arg_MustBeIntPtr);
        }

        public unsafe int CompareTo(IntPtr value) => ((nint_t)_value).CompareTo((nint_t)value);

        [NonVersionable]
        public unsafe bool Equals(IntPtr other) => (nint_t)_value == (nint_t)other;

        public unsafe override string ToString() => ((nint_t)_value).ToString();
        public unsafe string ToString(string? format) => ((nint_t)_value).ToString(format);
        public unsafe string ToString(IFormatProvider? provider) => ((nint_t)_value).ToString(provider);
        public unsafe string ToString(string? format, IFormatProvider? provider) => ((nint_t)_value).ToString(format, provider);

        public unsafe bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            ((nint_t)_value).TryFormat(destination, out charsWritten, format, provider);

        public static IntPtr Parse(string s) => (IntPtr)nint_t.Parse(s);
        public static IntPtr Parse(string s, NumberStyles style) => (IntPtr)nint_t.Parse(s, style);
        public static IntPtr Parse(string s, IFormatProvider? provider) => (IntPtr)nint_t.Parse(s, provider);
        public static IntPtr Parse(string s, NumberStyles style, IFormatProvider? provider) => (IntPtr)nint_t.Parse(s, style, provider);
        public static IntPtr Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null) => (IntPtr)nint_t.Parse(s, style, provider);

        public static bool TryParse([NotNullWhen(true)] string? s, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, style, provider, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, style, provider, out Unsafe.As<IntPtr, nint_t>(ref result));
        }
    }
}
