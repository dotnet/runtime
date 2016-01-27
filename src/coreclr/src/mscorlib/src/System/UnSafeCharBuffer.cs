// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** Purpose: A class to detect incorrect usage of UnSafeBuffer
**
** 
===========================================================*/

namespace System {
    using System.Security;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    
    unsafe internal struct UnSafeCharBuffer{
        [SecurityCritical]
        char * m_buffer;
        int m_totalSize;
        int m_length;    
    
        [System.Security.SecurityCritical]  // auto-generated
        public UnSafeCharBuffer( char *buffer,  int bufferSize) {
            Contract.Assert( buffer != null, "buffer pointer can't be null."  );
            Contract.Assert( bufferSize >= 0, "buffer size can't be negative."  );        
            m_buffer = buffer;
            m_totalSize = bufferSize;    
            m_length = 0;
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AppendString(string stringToAppend) {
            if( String.IsNullOrEmpty( stringToAppend ) ) {
                return;
            }
            
            if ( (m_totalSize - m_length) < stringToAppend.Length ) {
                throw new IndexOutOfRangeException();
            }
            
            fixed( char* pointerToString = stringToAppend ) {        
                Buffer.Memcpy( (byte*) (m_buffer + m_length), (byte *) pointerToString, stringToAppend.Length * sizeof(char));
            }    
            
            m_length += stringToAppend.Length;
            Contract.Assert(m_length <= m_totalSize, "Buffer has been overflowed!");
        }
                
        public int Length {
            get {
                return m_length;
            } 
        }   
    }    
} 
