// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    
    using System;

    [System.Runtime.InteropServices.ComVisible(true)]
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
            m_handle  = handle;
        }

        public Object Wrapper {
            get {
                return m_wrapper;
            }
        }
    
        public IntPtr Handle {
            get {
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
