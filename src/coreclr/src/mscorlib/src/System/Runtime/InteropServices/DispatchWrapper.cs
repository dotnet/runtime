// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Wrapper that is converted to a variant with VT_DISPATCH.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
   
    using System;
    using System.Security;
    using System.Security.Permissions;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class DispatchWrapper
    {
        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand,Flags=SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        public DispatchWrapper(Object obj)
        {
            if (obj != null)
            {
                // Make sure this guy has an IDispatch
                IntPtr pdisp = Marshal.GetIDispatchForObject(obj);

                // If we got here without throwing an exception, the QI for IDispatch succeeded.
                Marshal.Release(pdisp);
            }
            m_WrappedObject = obj;
        }

        public Object WrappedObject 
        {
            get 
            {
                return m_WrappedObject;
            }
        }

        private Object m_WrappedObject;
    }
}
