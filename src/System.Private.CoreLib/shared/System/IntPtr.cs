// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

#if BIT64
using nint = System.Int64;
#else
using nint = System.Int32;
#endif

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct IntPtr : IEquatable<IntPtr>, ISerializable
    {
        // WARNING: We allow diagnostic tools to directly inspect this member (_value). 
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details. 
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools. 
        // Get in touch with the diagnostics team if you have questions.
        private readonly unsafe void* _value; // Do not rename (binary serialization)

        [Intrinsic]
        public static readonly IntPtr Zero;

        [Intrinsic]
        [NonVersionable]
        public unsafe IntPtr(int value)
        {
            _value = (void*)value;
        }

        [Intrinsic]
        [NonVersionable]
        public unsafe IntPtr(long value)
        {
#if BIT64
            _value = (void*)value;
#else
            _value = (void*)checked((int)value);
#endif
        }

        [CLSCompliant(false)]
        [Intrinsic]
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

        public unsafe override bool Equals(object? obj)
        {
            if (obj is IntPtr)
            {
                return (_value == ((IntPtr)obj)._value);
            }
            return false;
        }

        unsafe bool IEquatable<IntPtr>.Equals(IntPtr other)
        {
            return _value == other._value;
        }

        public unsafe override int GetHashCode()
        {
#if BIT64
            long l = (long)_value;
            return (unchecked((int)l) ^ (int)(l >> 32));
#else
            return unchecked((int)_value);
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public unsafe int ToInt32()
        {
#if BIT64
            long l = (long)_value;
            return checked((int)l);
#else
            return (int)_value;
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public unsafe long ToInt64()
        {
            return (nint)_value;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator IntPtr(int value)
        {
            return new IntPtr(value);
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator IntPtr(long value)
        {
            return new IntPtr(value);
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator IntPtr(void* value)
        {
            return new IntPtr(value);
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator void* (IntPtr value)
        {
            return value._value;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator int(IntPtr value)
        {
#if BIT64
            long l = (long)value._value;
            return checked((int)l);
#else
            return (int)value._value;
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator long(IntPtr value)
        {
            return (nint)value._value;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe bool operator ==(IntPtr value1, IntPtr value2)
        {
            return value1._value == value2._value;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe bool operator !=(IntPtr value1, IntPtr value2)
        {
            return value1._value != value2._value;
        }

        [NonVersionable]
        public static IntPtr Add(IntPtr pointer, int offset)
        {
            return pointer + offset;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe IntPtr operator +(IntPtr pointer, int offset)
        {
            return new IntPtr((nint)pointer._value + offset);
        }

        [NonVersionable]
        public static IntPtr Subtract(IntPtr pointer, int offset)
        {
            return pointer - offset;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe IntPtr operator -(IntPtr pointer, int offset)
        {
            return new IntPtr((nint)pointer._value - offset);
        }

        public static int Size
        {
            [Intrinsic]
            [NonVersionable]
            get
            {
                return sizeof(nint);
            }
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
#if PROJECTN
        [System.Runtime.CompilerServices.DependencyReductionRootAttribute]
#endif
        public unsafe void* ToPointer()
        {
            return _value;
        }

        public unsafe override string ToString()
        {
            return ((nint)_value).ToString(CultureInfo.InvariantCulture);
        }

        public unsafe string ToString(string format)
        {
            return ((nint)_value).ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
