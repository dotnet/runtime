// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices
{
    // This the interface that be implemented by class that want to customize the behavior of QueryInterface.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ICustomQueryInterface
    {
        CustomQueryInterfaceResult GetInterface([In] ref Guid iid, out IntPtr ppv);
    }
}
