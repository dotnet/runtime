// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: This the base interface that must be implemented by all custom
**          marshalers.
**
**
=============================================================================*/

using System;

namespace System.Runtime.InteropServices
{
    public interface ICustomMarshaler
    {
        Object MarshalNativeToManaged(IntPtr pNativeData);

        IntPtr MarshalManagedToNative(Object ManagedObj);

        void CleanUpNativeData(IntPtr pNativeData);

        void CleanUpManagedData(Object ManagedObj);

        int GetNativeDataSize();
    }
}
