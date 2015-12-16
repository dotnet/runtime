// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Managed declaration of the IAgileObject COM interface.
    /// </summary>
    [Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    interface IAgileObject
    {
        // this is an empty marker interface

    }  // interface IAgileObject
}  // namespace 

// IAgileObject.cs
