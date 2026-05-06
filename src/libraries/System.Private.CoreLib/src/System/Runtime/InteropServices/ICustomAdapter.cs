// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices
{
    // This the base interface that custom adapters can chose to implement when they want to expose the underlying object.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ICustomAdapter
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetUnderlyingObject();
    }
}
