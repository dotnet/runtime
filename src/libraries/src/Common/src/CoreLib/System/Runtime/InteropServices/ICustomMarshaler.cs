// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // This the base interface that must be implemented by all custom marshalers.
    public interface ICustomMarshaler
    {
        object MarshalNativeToManaged(IntPtr pNativeData);

        IntPtr MarshalManagedToNative(object ManagedObj);

        void CleanUpNativeData(IntPtr pNativeData);

        void CleanUpManagedData(object ManagedObj);

        int GetNativeDataSize();
    }
}
