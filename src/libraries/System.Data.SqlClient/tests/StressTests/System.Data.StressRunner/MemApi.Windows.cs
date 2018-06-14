// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace DPStressHarness
{
    static class MemApi
    {
        [DllImport("KERNEL32")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("KERNEL32")]
        public static extern bool SetProcessWorkingSetSize(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);
    }
}
