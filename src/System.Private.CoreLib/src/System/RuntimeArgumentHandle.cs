// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    //  This value type is used for constructing System.ArgIterator. 
    // 
    //  SECURITY : m_ptr cannot be set to anything other than null by untrusted
    //  code.  
    // 
    //  This corresponds to EE VARARGS cookie.

    // Cannot be serialized
    public ref struct RuntimeArgumentHandle
    {
        private IntPtr m_ptr;

        internal IntPtr Value { get { return m_ptr; } }
    }
}
