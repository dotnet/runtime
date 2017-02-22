// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Runtime.InteropServices
{
    public struct HandleRef
    {
        // ! Do not add or rearrange fields as the EE depends on this layout.
        //------------------------------------------------------------------
        internal Object m_wrapper;
        internal IntPtr m_handle;
        //------------------------------------------------------------------


        public HandleRef(Object wrapper, IntPtr handle)
        {
            m_wrapper = wrapper;
            m_handle = handle;
        }

        public Object Wrapper
        {
            get
            {
                return m_wrapper;
            }
        }

        public IntPtr Handle
        {
            get
            {
                return m_handle;
            }
        }


        public static explicit operator IntPtr(HandleRef value)
        {
            return value.m_handle;
        }

        public static IntPtr ToIntPtr(HandleRef value)
        {
            return value.m_handle;
        }
    }
}
