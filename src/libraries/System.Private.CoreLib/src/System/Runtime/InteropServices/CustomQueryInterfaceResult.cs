// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices
{
    // The enum of the return value of ICustomQueryInterface.GetInterface
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum CustomQueryInterfaceResult
    {
        Handled = 0,
        NotHandled = 1,
        Failed = 2,
    }
}
