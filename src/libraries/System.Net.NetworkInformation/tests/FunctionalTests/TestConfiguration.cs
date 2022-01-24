// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.NetworkInformation.Tests
{
    internal static class TestConfiguration
    {
        public static bool SupportsGettingThreadsWithPsCommand { get { return s_supportsGettingThreadsWithPsCommand.Value; } }

        private static Lazy<bool> s_supportsGettingThreadsWithPsCommand = new Lazy<bool>(() =>
        {
            if (OperatingSystem.IsWindows())
            {
                return false;
            }

            // On other platforms we will try it.
            try
            {
                _ = ProcessUtil.GetProcessThreadsWithPsCommand(Process.GetCurrentProcess().Id);
                return true;
            }
            catch { return false; }
        });
    }
}