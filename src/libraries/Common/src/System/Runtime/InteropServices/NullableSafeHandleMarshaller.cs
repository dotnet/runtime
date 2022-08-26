// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices.Marshalling
{
    //[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.UnmanagedToManagedOut, typeof(NullableSafeHandleMarshaller<>.Out))]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.ManagedToUnmanagedOut, typeof(NullableSafeHandleMarshaller<>.Out))]
    internal static class NullableSafeHandleMarshaller<T> where T : SafeHandle, new()
    {

        public struct Out
        {
            private T? _handle;
            public Out()
            {
           //     _newHandle = new T();
            }

            public void FromManaged(T handle)
            {
                _handle = handle;
            }

            public IntPtr ToUnmanaged() => _handle != null ? _handle.DangerousGetHandle() : IntPtr.Zero;

            public void FromUnmanaged(IntPtr value)
            {
                if (value != IntPtr.Zero)
                {
                    _handle = new T();
                    Marshal.InitHandle(_handle, value);
                }
            }

            public T? ToManaged() => _handle;

            public void Free()
            {
            }
        }

/*
        internal struct NullableSafeHandleMarsshaller
        {
            private SafeHandle? _handle;

            public void FromManaged(SafeHandle handle)
            {
                _handle = handle;
            }

            public IntPtr ToUnmanaged() => _handle.Handle;

            public void FromUnmanaged(IntPtr value)
            {
                if (value != IntPtr.Zero)
                {
                    Marshal.InitHandle(_newHandle, value);
                }
            }

            public T ToManaged() => _handle;

            public void Free() { }
        }
*/
    }
}
