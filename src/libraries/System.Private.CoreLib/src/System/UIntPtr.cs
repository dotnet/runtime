// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
    [Serializable]
    [CLSCompliant(false)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct UIntPtr : IEquatable<UIntPtr>, ISerializable
    {
        private readonly unsafe void* _value; // Do not rename (binary serialization)

        [Intrinsic]
        public static readonly UIntPtr Zero;

        [Intrinsic]
        [NonVersionable]
        public unsafe UIntPtr(uint value)
        {
            _value = (void*)value;
        }

        [Intrinsic]
        [NonVersionable]
        public unsafe UIntPtr(ulong value)
        {
#if BIT64
            _value = (void*)value;
#else
            _value = (void*)checked((uint)value);
#endif
        }

        [Intrinsic]
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

        public unsafe override bool Equals(object? obj)
        {
            if (obj is UIntPtr)
            {
                return (_value == ((UIntPtr)obj)._value);
            }
            return false;
        }

        unsafe bool IEquatable<UIntPtr>.Equals(UIntPtr other)
        {
            return _value == other._value;
        }

        public unsafe override int GetHashCode()
        {
#if BIT64
            ulong l = (ulong)_value;
            return (unchecked((int)l) ^ (int)(l >> 32));
#else
            return unchecked((int)_value);
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public unsafe uint ToUInt32()
        {
#if BIT64
            return checked((uint)_value);
#else
            return (uint)_value;
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public unsafe ulong ToUInt64()
        {
            return (ulong)_value;
        }

        [Intrinsic]
        [NonVersionable]
        public static explicit operator UIntPtr(uint value)
        {
            return new UIntPtr(value);
        }

        [Intrinsic]
        [NonVersionable]
        public static explicit operator UIntPtr(ulong value)
        {
            return new UIntPtr(value);
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator UIntPtr(void* value)
        {
            return new UIntPtr(value);
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator void* (UIntPtr value)
        {
            return value._value;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator uint(UIntPtr value)
        {
#if BIT64
            return checked((uint)value._value);
#else
            return (uint)value._value;
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe explicit operator ulong(UIntPtr value)
        {
            return (ulong)value._value;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe bool operator ==(UIntPtr value1, UIntPtr value2)
        {
            return value1._value == value2._value;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe bool operator !=(UIntPtr value1, UIntPtr value2)
        {
            return value1._value != value2._value;
        }

        [NonVersionable]
        public static UIntPtr Add(UIntPtr pointer, int offset)
        {
            return pointer + offset;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe UIntPtr operator +(UIntPtr pointer, int offset)
        {
            return new UIntPtr((nuint)pointer._value + (nuint)offset);
        }

        [NonVersionable]
        public static UIntPtr Subtract(UIntPtr pointer, int offset)
        {
            return pointer - offset;
        }

        [Intrinsic]
        [NonVersionable]
        public static unsafe UIntPtr operator -(UIntPtr pointer, int offset)
        {
            return new UIntPtr((nuint)pointer._value - (nuint)offset);
        }

        public static int Size
        {
            [Intrinsic]
            [NonVersionable]
            get
            {
                return sizeof(nuint);
            }
        }

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
            return ((nuint)_value).ToString(CultureInfo.InvariantCulture);
        }
    }
}
