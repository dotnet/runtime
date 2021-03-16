// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    // Mono runtime relies on exact layout
    [StructLayout(LayoutKind.Sequential)]
    public abstract partial class SafeHandle
    {
    }
}
