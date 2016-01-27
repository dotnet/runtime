// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{
    using System.Runtime.InteropServices;
    using System;
    using System.Security.Permissions;

    // This class is describing the fieldmarshal.
    [Serializable]
    [HostProtection(MayLeakOnAbort = true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    [Obsolete("An alternate API is available: Emit the MarshalAs custom attribute instead. http://go.microsoft.com/fwlink/?linkid=14202")]
    public sealed class UnmanagedMarshal
    {
        /******************************
[System.Runtime.InteropServices.ComVisible(true)]
        * public static constructors. You can only construct
        * UnmanagedMarshal using these static constructors. 
        ******************************/
        public static UnmanagedMarshal DefineUnmanagedMarshal(UnmanagedType unmanagedType)
        {
            if (unmanagedType == UnmanagedType.ByValTStr ||
                unmanagedType == UnmanagedType.SafeArray ||
                unmanagedType == UnmanagedType.CustomMarshaler ||
                unmanagedType == UnmanagedType.ByValArray ||
                unmanagedType == UnmanagedType.LPArray)
            {
                // not a simple native marshal
                throw new ArgumentException(Environment.GetResourceString("Argument_NotASimpleNativeType"));
            }
            return new UnmanagedMarshal(unmanagedType, Guid.Empty, 0, (UnmanagedType) 0);
        }
        public static UnmanagedMarshal DefineByValTStr(int elemCount)
        {
            return new UnmanagedMarshal(UnmanagedType.ByValTStr, Guid.Empty, elemCount, (UnmanagedType) 0);
        }
  
        public static UnmanagedMarshal DefineSafeArray(UnmanagedType elemType)
        {
            return new UnmanagedMarshal(UnmanagedType.SafeArray, Guid.Empty, 0, elemType);
        }

        public static UnmanagedMarshal DefineByValArray(int elemCount)
        {
            return new UnmanagedMarshal(UnmanagedType.ByValArray, Guid.Empty, elemCount, (UnmanagedType) 0);
        }
    
        public static UnmanagedMarshal DefineLPArray(UnmanagedType elemType)
        {
            return new UnmanagedMarshal(UnmanagedType.LPArray, Guid.Empty, 0, elemType);
        }
    
 
         
         
        

        // accessor function for the native type
        public UnmanagedType GetUnmanagedType 
        {
            get { return m_unmanagedType; }
        }
    
        public Guid IIDGuid 
        {
            get 
            { 
                if (m_unmanagedType == UnmanagedType.CustomMarshaler) 
                    return m_guid; 

                // throw exception here. There is Guid only if CustomMarshaler
                throw new ArgumentException(Environment.GetResourceString("Argument_NotACustomMarshaler"));
            }
        }
        public int ElementCount 
        {
            get 
            { 
                if (m_unmanagedType != UnmanagedType.ByValArray &&
                    m_unmanagedType != UnmanagedType.ByValTStr) 
                {
                    // throw exception here. There is NumElement only if NativeTypeFixedArray
                    throw new ArgumentException(Environment.GetResourceString("Argument_NoUnmanagedElementCount"));
                }
                return m_numElem;
            } 
        }
        public UnmanagedType BaseType 
        {
            get 
            { 
                if (m_unmanagedType != UnmanagedType.LPArray && m_unmanagedType != UnmanagedType.SafeArray) 
                {
                    // throw exception here. There is NestedUnmanagedType only if LPArray or SafeArray
                    throw new ArgumentException(Environment.GetResourceString("Argument_NoNestedMarshal"));
                }
                return m_baseType;
            } 
        }
    
        private UnmanagedMarshal(UnmanagedType unmanagedType, Guid guid, int numElem, UnmanagedType type)
        {
            m_unmanagedType = unmanagedType;
            m_guid = guid;
            m_numElem = numElem;
            m_baseType = type;
        }
    
        /************************
        *
        * Data member
        *
        *************************/
        internal UnmanagedType       m_unmanagedType;
        internal Guid                m_guid;
        internal int                 m_numElem;
        internal UnmanagedType       m_baseType;
    
    
        /************************
        * this function return the byte representation of the marshal info.
        *************************/
        internal byte[] InternalGetBytes()
        {
            byte[] buf;
            if (m_unmanagedType == UnmanagedType.SafeArray || m_unmanagedType == UnmanagedType.LPArray)
            {
    
                // syntax for NativeTypeSafeArray is 
                // <SafeArray | LPArray> <base type>
                //
                int     cBuf = 2;
                buf = new byte[cBuf];
                buf[0] = (byte) (m_unmanagedType);
                buf[1] = (byte) (m_baseType);
                return buf;
            }
            else
            if (m_unmanagedType == UnmanagedType.ByValArray || 
                    m_unmanagedType == UnmanagedType.ByValTStr) 
            {
                // <ByValArray | ByValTStr> <encoded integer>
                //
                int     cBuf;
                int     iBuf = 0;
    
                if (m_numElem <= 0x7f)
                    cBuf = 1;
                else if (m_numElem <= 0x3FFF)
                    cBuf = 2;
                else
                    cBuf = 4;
    
                // the total buffer size is the one byte + encoded integer size 
                cBuf = cBuf + 1;
                buf = new byte[cBuf];
    
                
                buf[iBuf++] = (byte) (m_unmanagedType);
                if (m_numElem <= 0x7F) 
                {
                    buf[iBuf++] = (byte)(m_numElem & 0xFF);
                } else if (m_numElem <= 0x3FFF) 
                {
                    buf[iBuf++] = (byte)((m_numElem >> 8) | 0x80);
                    buf[iBuf++] = (byte)(m_numElem & 0xFF);
                } else if (m_numElem <= 0x1FFFFFFF) 
                {
                    buf[iBuf++] = (byte)((m_numElem >> 24) | 0xC0);
                    buf[iBuf++] = (byte)((m_numElem >> 16) & 0xFF);
                    buf[iBuf++] = (byte)((m_numElem >> 8)  & 0xFF);
                    buf[iBuf++] = (byte)((m_numElem)     & 0xFF);
                }            
                return buf;
            }
            buf = new byte[1];
            buf[0] = (byte) (m_unmanagedType);
            return buf;
        }
    }
}
