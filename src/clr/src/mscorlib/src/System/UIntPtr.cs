// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Platform independent integer
**
** 
===========================================================*/

namespace System
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Diagnostics.Contracts;

    [Serializable]
    [CLSCompliant(false)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    public struct UIntPtr : IEquatable<UIntPtr>, ISerializable
    {
        unsafe private void* _value; // Do not rename (binary serialization)

        public static readonly UIntPtr Zero;


        [System.Runtime.Versioning.NonVersionable]
        public unsafe UIntPtr(uint value)
        {
            _value = (void*)value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe UIntPtr(ulong value)
        {
#if BIT64
            _value = (void*)value;
#else // 32
            _value = (void*)checked((uint)value);
#endif
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe UIntPtr(void* value)
        {
            _value = value;
        }

        private unsafe UIntPtr(SerializationInfo info, StreamingContext context)
        {
            ulong l = info.GetUInt64("value");

            if (Size == 4 && l > UInt32.MaxValue)
            {
                throw new ArgumentException(SR.Serialization_InvalidPtrValue);
            }

            _value = (void*)l;
        }

        unsafe void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            Contract.EndContractBlock();
            info.AddValue("value", (ulong)_value);
        }

        public unsafe override bool Equals(Object obj)
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
#else // 32
            return unchecked((int)_value);
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe uint ToUInt32()
        {
#if BIT64
            return checked((uint)_value);
#else // 32
            return (uint)_value;
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe ulong ToUInt64()
        {
            return (ulong)_value;
        }

        public unsafe override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);

#if BIT64
            return ((ulong)_value).ToString(CultureInfo.InvariantCulture);
#else // 32
            return ((uint)_value).ToString(CultureInfo.InvariantCulture);
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator UIntPtr(uint value)
        {
            return new UIntPtr(value);
        }

        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator UIntPtr(ulong value)
        {
            return new UIntPtr(value);
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator uint(UIntPtr value)
        {
#if BIT64
            return checked((uint)value._value);
#else // 32
            return (uint)value._value;
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator ulong(UIntPtr value)
        {
            return (ulong)value._value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator UIntPtr(void* value)
        {
            return new UIntPtr(value);
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator void* (UIntPtr value)
        {
            return value._value;
        }


        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator ==(UIntPtr value1, UIntPtr value2)
        {
            return value1._value == value2._value;
        }


        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator !=(UIntPtr value1, UIntPtr value2)
        {
            return value1._value != value2._value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr Add(UIntPtr pointer, int offset)
        {
            return pointer + offset;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr operator +(UIntPtr pointer, int offset)
        {
#if BIT64
            return new UIntPtr(pointer.ToUInt64() + (ulong)offset);
#else // 32
                return new UIntPtr(pointer.ToUInt32() + (uint)offset);
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr Subtract(UIntPtr pointer, int offset)
        {
            return pointer - offset;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr operator -(UIntPtr pointer, int offset)
        {
#if BIT64
            return new UIntPtr(pointer.ToUInt64() - (ulong)offset);
#else // 32
                return new UIntPtr(pointer.ToUInt32() - (uint)offset);
#endif
        }

        public static int Size
        {
            [System.Runtime.Versioning.NonVersionable]
            get
            {
#if BIT64
                return 8;
#else // 32
                return 4;
#endif
            }
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe void* ToPointer()
        {
            return _value;
        }
    }
}


