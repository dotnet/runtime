// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct StackAllocatedBox<T>
    {
        // These fields are only accessed from jitted code
        private IntPtr _pMethodTable;
        private T _value;
    }
}
