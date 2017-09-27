// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class Kernel32
    {
        // P/Invoke for the methods above. Don't call this from anywhere else.
        [DllImport(Libraries.Kernel32, ExactSpelling=true, SetLastError=true, EntryPoint="WaitForSingleObject")]
        internal static extern int WaitForSingleObjectDontCallThis(SafeWaitHandle handle, int timeout);
    }
}
