// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace System.Diagnostics.Tests
{
    internal static partial class Interop
    {
        [DllImport("libc")]
        internal static extern int getsid(int pid);
    }
}
