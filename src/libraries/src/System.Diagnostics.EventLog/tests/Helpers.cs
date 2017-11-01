// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    internal class Helpers
    {
        public static bool IsElevatedAndSupportsEventLogs { get => AdminHelpers.IsProcessElevated() && SupportsEventLogs; }
        public static bool SupportsEventLogs { get => PlatformDetection.IsNotWindowsNanoServer; }

        public static void RetryOnWin7(Action func)
        {
            RetryOnWin7<object>(() => { func(); return null; });
        }

        public static T RetryOnWin7<T>(Func<T> func)
        {
            T entry = default(T);
            if (!PlatformDetection.IsWindows7)
            {
                return func();
            }

            // We are retrying on windows 7 because it throws win32exception while some operations like Writing,Retrieveing and Deleting log.
            // So We just try to do the operation again in case of this exception 
            int retries = 10;
            while (retries > 0)
            {
                try
                {
                    entry = func();
                    retries = -1;
                }
                catch (Win32Exception)
                {
                    Thread.Sleep(100);
                    retries--;
                }
            }

            Assert.NotEqual(0, retries);
            return entry;
        }
    }
}
