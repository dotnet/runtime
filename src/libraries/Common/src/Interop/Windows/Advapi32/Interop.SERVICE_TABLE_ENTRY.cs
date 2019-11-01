// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Advapi32
    {
        public delegate void ServiceMainCallback(int argCount, IntPtr argPointer);

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_TABLE_ENTRY
        {
            public IntPtr name;
            public ServiceMainCallback callback;
        }
    }
}
