// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Wrapper that is converted to a variant with VT_BSTR.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
   
    using System;
    using System.Security;
    using System.Security.Permissions;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class BStrWrapper
    {
        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand,Flags=SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        public BStrWrapper(String value)
        {
            m_WrappedObject = value;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        public BStrWrapper(Object value)
        {
            m_WrappedObject = (String)value;
        }

        public String WrappedObject 
        {
            get 
            {
                return m_WrappedObject;
            }
        }

        private String m_WrappedObject;
    }
}
