// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MONO
using System.Diagnostics.CodeAnalysis;
#endif

namespace System.Runtime.InteropServices
{
    // This the base interface that must be implemented by all custom marshalers.
    public interface ICustomMarshaler
    {
#if MONO
        // ILLinker marks all methods of this type equally so the attribute can be on any of them
        [DynamicDependency(nameof(Marshal.GetCustomMarshalerInstance), typeof(Marshal))]
#endif
        object MarshalNativeToManaged(IntPtr pNativeData);

        IntPtr MarshalManagedToNative(object ManagedObj);

        void CleanUpNativeData(IntPtr pNativeData);

        void CleanUpManagedData(object ManagedObj);

        int GetNativeDataSize();
    }
}
