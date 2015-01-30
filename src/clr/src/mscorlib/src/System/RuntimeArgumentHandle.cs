// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System {
    
    using System;
    //  This value type is used for constructing System.ArgIterator. 
    // 
    //  SECURITY : m_ptr cannot be set to anything other than null by untrusted
    //  code.  
    // 
    //  This corresponds to EE VARARGS cookie.

    // Cannot be serialized
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct RuntimeArgumentHandle
    {
        private IntPtr m_ptr;

        internal IntPtr Value { get { return m_ptr; } }
    }

}
