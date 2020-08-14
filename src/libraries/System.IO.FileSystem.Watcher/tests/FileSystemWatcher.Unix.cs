// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.IO.Tests
{
    public partial class DangerousFileSystemWatcherTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [OuterLoop("Slow test with significant resource usage.")]
        public void FileSystemWatcher_Unix_DoesNotLeak()
        {
            Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, out Interop.Sys.RLimit limits);
            _output.WriteLine("File descriptor limit is {0}", limits.CurrentLimit);
            _output.WriteLine($"Starting 100/200 test on {TestDirectory}");

            RemoteInvokeOptions options = new RemoteInvokeOptions { TimeOut = 600_000 };
            RemoteExecutor.Invoke(testDirectory =>
            {
                ulong maxFd = 200;
                int count = 100;
                Interop.Sys.RLimit limits = new Interop.Sys.RLimit { CurrentLimit = maxFd, MaximumLimit = maxFd};

                // Set open file limit to given value.
                Assert.Equal(0, Interop.Sys.SetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, ref limits));
                Assert.Equal(0, Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, out limits));
                Assert.Equal(maxFd, limits.CurrentLimit);

                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        using (var watcher = new FileSystemWatcher(testDirectory))
                        {
                            watcher.Created += (s, e) => { } ;
                            watcher.EnableRaisingEvents = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    // If we use all handles we may not have luck writing out errors.
                    // Try our best here. _output is not available within RemoteExec().
                    // When we run out of fd, exception may be weird from OutOfMem to "unable load type".
                    Console.WriteLine($"Test failed for count={count}, limit={maxFd} and path='{testDirectory}'.");
                    Console.WriteLine(e.Message);
                    return 1;
                }

                return RemoteExecutor.SuccessExitCode;
            }, TestDirectory, options).Dispose();
        }
    }
}
