// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static class ThreadOSX
    {
        private static bool CheckEnableAutoreleasePool()
        {
            bool isEnabled = CLRConfig.GetBoolValue("System.Threading.Thread.EnableAutoreleasePool", out bool isSet);
            if (!isSet)
                return false;

            return isEnabled;
        }

        public static bool EnableAutoreleasePool { get; } = CheckEnableAutoreleasePool();
    }

    public sealed partial class Thread
    {
        internal static void AllocateThreadlocalAutoreleasePool()
        {
            if (!ThreadOSX.EnableAutoreleasePool)
                throw new PlatformNotSupportedException();

            Interop.Sys.AllocateThreadlocalAutoreleasePool();
        }
    }
}
