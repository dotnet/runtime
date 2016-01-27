// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct QUOTA_LIMITS
        {
            internal IntPtr PagedPoolLimit;
            internal IntPtr NonPagedPoolLimit;
            internal IntPtr MinimumWorkingSetSize;
            internal IntPtr MaximumWorkingSetSize;
            internal IntPtr PagefileLimit;
            internal long TimeLimit;
        }
    }
}
