// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices
{
    [NonVersionable]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct StackAllocatedBox<T>
    {
        // These fields are only accessed from jitted code
        private IntPtr _pMethodTable;
        private T _value;
    }
}
