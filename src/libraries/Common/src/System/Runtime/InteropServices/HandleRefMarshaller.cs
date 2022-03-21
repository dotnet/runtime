// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable


namespace System.Runtime.InteropServices.GeneratedMarshalling
{
    internal struct HandleRefMarshaller
    {
        private HandleRef _handle;

        public HandleRefMarshaller(HandleRef handle)
        {
            _handle = handle;
        }

        public IntPtr Value => _handle.Handle;

        public void FreeNative() => GC.KeepAlive(_handle.Wrapper);
    }
}
