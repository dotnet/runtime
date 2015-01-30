// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: This the base interface that must be implemented by all custom
**          marshalers.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface ICustomMarshaler
    {        
        Object MarshalNativeToManaged( IntPtr pNativeData );

        IntPtr MarshalManagedToNative( Object ManagedObj );

        void CleanUpNativeData( IntPtr pNativeData );

        void CleanUpManagedData( Object ManagedObj );

        int GetNativeDataSize();
    }
}
