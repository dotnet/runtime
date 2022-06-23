// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Runtime.InteropServices.Marshalling
{
    [CustomTypeMarshaller(typeof(HandleRef), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling)]
    internal struct HandleRefMarshaller
    {
        private HandleRef _handle;

        public HandleRefMarshaller(HandleRef handle)
        {
            _handle = handle;
        }

        public IntPtr ToNativeValue() => _handle.Handle;

        public void FreeNative() => GC.KeepAlive(_handle.Wrapper);
    }
}
