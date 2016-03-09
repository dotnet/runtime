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
    using System.Runtime;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security;
    using System.Diagnostics.Contracts;
    
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct IntPtr : ISerializable
    {
        [SecurityCritical]
        unsafe private void* m_value; // The compiler treats void* closest to uint hence explicit casts are required to preserve int behavior
                
        public static readonly IntPtr Zero;

        // fast way to compare IntPtr to (IntPtr)0 while IntPtr.Zero doesn't work due to slow statics access
        [System.Security.SecuritySafeCritical]  // auto-generated
        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe bool IsNull()
        {
            return (this.m_value == null);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe IntPtr(int value)
        {
            #if WIN32
                m_value = (void *)value;
            #else
                m_value = (void *)(long)value;
            #endif
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe IntPtr(long value)
        {
            #if WIN32
                m_value = (void *)checked((int)value);
            #else
                m_value = (void *)value;
            #endif
        }

        [System.Security.SecurityCritical]
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe IntPtr(void* value)
        {
            m_value = value;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe IntPtr(SerializationInfo info, StreamingContext context) {
            long l = info.GetInt64("value");

            if (Size==4 && (l>Int32.MaxValue || l<Int32.MinValue)) {
                throw new ArgumentException(Environment.GetResourceString("Serialization_InvalidPtrValue"));
            }

            m_value = (void *)l;
        }

#if FEATURE_SERIALIZATION
        [System.Security.SecurityCritical]
        unsafe void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();
            #if WIN32
                info.AddValue("value", (long)((int)m_value));
            #else
                info.AddValue("value", (long)(m_value));
            #endif
        }
#endif

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override bool Equals(Object obj) {
            if (obj is IntPtr) {
                return (m_value == ((IntPtr)obj).m_value);
            }
            return false;
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override int GetHashCode() {
#if FEATURE_CORECLR
    #if WIN32
            return unchecked((int)m_value);
    #else
            long l = (long)m_value;
            return (unchecked((int)l) ^ (int)(l >> 32));
    #endif
#else
            return unchecked((int)((long)m_value));
#endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe int ToInt32() {
            #if WIN32
                return (int)m_value;
            #else
                long l = (long)m_value;
                return checked((int)l);
            #endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe long ToInt64() {
            #if WIN32
                return (long)(int)m_value;
            #else
                return (long)m_value;
            #endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override String ToString() {
            #if WIN32
                return ((int)m_value).ToString(CultureInfo.InvariantCulture);
            #else
                return ((long)m_value).ToString(CultureInfo.InvariantCulture);
            #endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe  String ToString(String format) 
        {
            Contract.Ensures(Contract.Result<String>() != null);

            #if WIN32
                return ((int)m_value).ToString(format, CultureInfo.InvariantCulture);
            #else
                return ((long)m_value).ToString(format, CultureInfo.InvariantCulture);
            #endif
        }


        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator IntPtr (int value) 
        {
            return new IntPtr(value);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static explicit operator IntPtr (long value) 
        {
            return new IntPtr(value);
        }

        [System.Security.SecurityCritical]
        [CLSCompliant(false), ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator IntPtr (void* value)
        {
            return new IntPtr(value);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static unsafe explicit operator void* (IntPtr value)
        {
            return value.m_value;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator int (IntPtr  value) 
        {
            #if WIN32
                return (int)value.m_value;
            #else
                long l = (long)value.m_value;
                return checked((int)l);
            #endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static explicit operator long (IntPtr  value) 
        {
            #if WIN32
                return (long)(int)value.m_value;
            #else
                return (long)value.m_value;
            #endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator == (IntPtr value1, IntPtr value2) 
        {
            return value1.m_value == value2.m_value;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool operator != (IntPtr value1, IntPtr value2) 
        {
            return value1.m_value != value2.m_value;
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr Add(IntPtr pointer, int offset)
        {
            return pointer + offset;
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr operator +(IntPtr pointer, int offset) 
        {
            #if WIN32
                return new IntPtr(pointer.ToInt32() + offset);
            #else
                return new IntPtr(pointer.ToInt64() + offset);
            #endif
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr Subtract(IntPtr pointer, int offset) {
            return pointer - offset;
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr operator -(IntPtr pointer, int offset) {
            #if WIN32
                return new IntPtr(pointer.ToInt32() - offset);
            #else
                return new IntPtr(pointer.ToInt64() - offset);
            #endif
        }

        public static int Size
        {
            [Pure]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [System.Runtime.Versioning.NonVersionable]
            get
            {
                #if WIN32
                    return 4;
                #else
                    return 8;
                #endif
            }
        }
    

        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe void* ToPointer()
        {
            return m_value;
        }
    }
}


