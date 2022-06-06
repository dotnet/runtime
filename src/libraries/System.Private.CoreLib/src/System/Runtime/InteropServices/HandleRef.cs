// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public readonly struct HandleRef
    {
        // ! Do not add or rearrange fields as the EE depends on this layout.
        //------------------------------------------------------------------
        private readonly object? _wrapper;
        private readonly nint _handle;
        //------------------------------------------------------------------

        public HandleRef(object? wrapper, nint handle)
        {
            _wrapper = wrapper;
            _handle = handle;
        }

        public object? Wrapper => _wrapper;

        public nint Handle => _handle;

        public static explicit operator nint(HandleRef value) => value._handle;

        public static nint ToIntPtr(HandleRef value) => value._handle;
    }
}
