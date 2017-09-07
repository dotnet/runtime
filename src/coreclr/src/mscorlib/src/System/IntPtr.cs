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
    using System.Runtime;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security;
    using System.Diagnostics.Contracts;

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct IntPtr : IEquatable<IntPtr>, ISerializable
    {
        unsafe private void* _value; // The compiler treats void* closest to uint hence explicit casts are required to preserve int behavior. Do not rename (binary serialization)

        public static readonly IntPtr Zero;

        // fast way to compare IntPtr to (IntPtr)0 while IntPtr.Zero doesn't work due to slow statics access
        [Pure]
        internal unsafe bool IsNull()
        {
            return (_value == null);
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe IntPtr(int value)
        {
#if BIT64
            _value = (void*)(long)value;
#else // !BIT64 (32)
                _value = (void *)value;
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe IntPtr(long value)
        {
#if BIT64
            _value = (void*)value;
#else // !BIT64 (32)
                _value = (void *)checked((int)value);
#endif
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe IntPtr(void* value)
        {
            _value = value;
        }

        private unsafe IntPtr(SerializationInfo info, StreamingContext context)
        {
            long l = info.GetInt64("value");

            if (Size == 4 && (l > Int32.MaxValue || l < Int32.MinValue))
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
#if BIT64
            info.AddValue("value", (long)(_value));
#else // !BIT64 (32)
                info.AddValue("value", (long)((int)_value));
#endif
        }

        public unsafe override bool Equals(Object obj)
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
#else // !BIT64 (32)
            return unchecked((int)_value);
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe int ToInt32()
        {
#if BIT64
            long l = (long)_value;
            return checked((int)l);
#else // !BIT64 (32)
                return (int)_value;
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe long ToInt64()
        {
#if BIT64
            return (long)_value;
#else // !BIT64 (32)
                return (long)(int)_value;
#endif
        }

        public unsafe override String ToString()
        {
#if BIT64
            return ((long)_value).ToString(CultureInfo.InvariantCulture);
#else // !BIT64 (32)
                return ((int)_value).ToString(CultureInfo.InvariantCulture);
#endif
        }

        public unsafe String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);

#if BIT64
            return ((long)_value).ToString(format, CultureInfo.InvariantCulture);
#else // !BIT64 (32)
                return ((int)_value).ToString(format, CultureInfo.InvariantCulture);
#endif
        }


        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator IntPtr(int value)
        {
            return new IntPtr(value);
        }

        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator IntPtr(long value)
        {
            return new IntPtr(value);
        }

        [CLSCompliant(false), ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator IntPtr(void* value)
        {
            return new IntPtr(value);
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator void* (IntPtr value)
        {
            return value._value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator int(IntPtr value)
        {
#if BIT64
            long l = (long)value._value;
            return checked((int)l);
#else // !BIT64 (32)
                return (int)value._value;
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator long(IntPtr value)
        {
#if BIT64
            return (long)value._value;
#else // !BIT64 (32)
                return (long)(int)value._value;
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator ==(IntPtr value1, IntPtr value2)
        {
            return value1._value == value2._value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator !=(IntPtr value1, IntPtr value2)
        {
            return value1._value != value2._value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr Add(IntPtr pointer, int offset)
        {
            return pointer + offset;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr operator +(IntPtr pointer, int offset)
        {
#if BIT64
            return new IntPtr(pointer.ToInt64() + offset);
#else // !BIT64 (32)
                return new IntPtr(pointer.ToInt32() + offset);
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr Subtract(IntPtr pointer, int offset)
        {
            return pointer - offset;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr operator -(IntPtr pointer, int offset)
        {
#if BIT64
            return new IntPtr(pointer.ToInt64() - offset);
#else // !BIT64 (32)
                return new IntPtr(pointer.ToInt32() - offset);
#endif
        }

        public static int Size
        {
            [Pure]
            [System.Runtime.Versioning.NonVersionable]
            get
            {
#if BIT64
                return 8;
#else // !BIT64 (32)
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


