// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace DPStressHarness
{
    internal static class MemApi
    {
        public static IntPtr GetCurrentProcess()
        {
            return IntPtr.Zero;
        }

        public static bool SetProcessWorkingSetSize(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize)
        {
            return true;
        }
    }
}
