// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Wrapper that is converted to a variant with VT_ERROR.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
   
    using System;
    using System.Security.Permissions;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ErrorWrapper
    {
        public ErrorWrapper(int errorCode)
        {
            m_ErrorCode = errorCode;
        }

        public ErrorWrapper(Object errorCode)
        {
            if (!(errorCode is int))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeInt32"), "errorCode");
            m_ErrorCode = (int)errorCode;
        }        

        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        public ErrorWrapper(Exception e)
        {
            m_ErrorCode = Marshal.GetHRForException(e);
        }

        public int ErrorCode 
        {
            get 
            {
                return m_ErrorCode;
            }
        }

        private int m_ErrorCode;
    }
}
