// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    public struct HandleRef
    {
        // ! Do not add or rearrange fields as the EE depends on this layout.
        //------------------------------------------------------------------
        private object _wrapper;
        private IntPtr _handle;
        //------------------------------------------------------------------

        public HandleRef(object wrapper, IntPtr handle)
        {
            _wrapper = wrapper;
            _handle = handle;
        }

        public object Wrapper
        {
            get
            {
                return _wrapper;
            }
        }

        public IntPtr Handle
        {
            get
            {
                return _handle;
            }
        }

        public static explicit operator IntPtr(HandleRef value)
        {
            return value._handle;
        }

        public static IntPtr ToIntPtr(HandleRef value)
        {
            return value._handle;
        }
    }
}
