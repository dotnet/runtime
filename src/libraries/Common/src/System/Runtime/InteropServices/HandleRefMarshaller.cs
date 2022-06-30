// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Runtime.InteropServices.Marshalling
{
    [CustomMarshaller(typeof(HandleRef), Scenario.ManagedToUnmanagedIn, typeof(KeepAliveMarshaller))]
    internal static class HandleRefMarshaller
    {
        internal struct KeepAliveMarshaller
        {
            private HandleRef _handle;

            public void FromManaged(HandleRef handle)
            {
                _handle = handle;
            }

            public IntPtr ToUnmanaged() => _handle.Handle;

            public void NotifyInvokeSucceeded() => GC.KeepAlive(_handle.Wrapper);
        }
    }
}
