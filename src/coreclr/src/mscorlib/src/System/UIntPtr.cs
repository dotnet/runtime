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

namespace System {
    
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Diagnostics.Contracts;

    [Serializable]
    [CLSCompliant(false)] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct UIntPtr : IEquatable<UIntPtr>, ISerializable
    {
        [SecurityCritical]
        unsafe private void* m_value;

        public static readonly UIntPtr Zero;

                
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe UIntPtr(uint value)
        {
            m_value = (void *)value;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe UIntPtr(ulong value)
        {
#if BIT64
            m_value = (void *)value;
#else // 32
            m_value = (void*)checked((uint)value);
#endif
        }

        [System.Security.SecurityCritical]
        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe UIntPtr(void* value)
        {
            m_value = value;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe UIntPtr(SerializationInfo info, StreamingContext context) {
            ulong l = info.GetUInt64("value");

            if (Size==4 && l>UInt32.MaxValue) {
                throw new ArgumentException(Environment.GetResourceString("Serialization_InvalidPtrValue"));
            }

            m_value = (void *)l;
        }

        [System.Security.SecurityCritical]
        unsafe void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();
            info.AddValue("value", (ulong)m_value);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override bool Equals(Object obj) {
            if (obj is UIntPtr) {
                return (m_value == ((UIntPtr)obj).m_value);
            }
            return false;
        }

        [SecuritySafeCritical]
        unsafe bool IEquatable<UIntPtr>.Equals(UIntPtr other)
        {
            return m_value == other.m_value;
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override int GetHashCode() {
#if FEATURE_CORECLR
#if BIT64
            ulong l = (ulong)m_value;
            return (unchecked((int)l) ^ (int)(l >> 32));
#else // 32
            return unchecked((int)m_value);
#endif
#else
            return unchecked((int)((long)m_value)) & 0x7fffffff;
#endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe uint ToUInt32() {
#if BIT64
            return checked((uint)m_value);
#else // 32
            return (uint)m_value;
#endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe ulong ToUInt64() {
            return (ulong)m_value;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);

#if BIT64
            return ((ulong)m_value).ToString(CultureInfo.InvariantCulture);
#else // 32
            return ((uint)m_value).ToString(CultureInfo.InvariantCulture);
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator UIntPtr (uint value) 
        {
            return new UIntPtr(value);
        }

        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator UIntPtr (ulong value) 
        {
            return new UIntPtr(value);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator uint(UIntPtr value)
        {
#if BIT64
            return checked((uint)value.m_value);
#else // 32
            return (uint)value.m_value;
#endif
        }   

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator ulong (UIntPtr  value) 
        {
            return (ulong)value.m_value;
        }

        [System.Security.SecurityCritical]
        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator UIntPtr (void* value)
        {
            return new UIntPtr(value);
        }

        [System.Security.SecurityCritical]
        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator void* (UIntPtr value)
        {
            return value.m_value;
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator == (UIntPtr value1, UIntPtr value2) 
        {
            return value1.m_value == value2.m_value;
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator != (UIntPtr value1, UIntPtr value2) 
        {
            return value1.m_value != value2.m_value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr Add(UIntPtr pointer, int offset) {
            return pointer + offset;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr operator +(UIntPtr pointer, int offset) {
#if BIT64
                return new UIntPtr(pointer.ToUInt64() + (ulong)offset);
#else // 32
                return new UIntPtr(pointer.ToUInt32() + (uint)offset);
#endif
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr Subtract(UIntPtr pointer, int offset) {
            return pointer - offset;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr operator -(UIntPtr pointer, int offset) {
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
       
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe void* ToPointer()
        {
            return m_value;
        }

     }
}


