// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    // Helper class to avoid issues with RemoteExecutor and _output and other properties.
    internal class RunTestHelper
    {
        private int _count;
        private int _limit;
        private string _path;

        internal RunTestHelper(int count, int limit, string path)
        {
            _count = count;
            _limit = limit;
            _path = path;
        }

        internal void ExecuteTest()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions{ TimeOut = 600_000 };
            RemoteExecutor.Invoke((countString, openFileLimitString, testDirectory) =>
            {
                ulong maxFd = ulong.Parse(openFileLimitString);
                int count = Int32.Parse(countString);
                Interop.Sys.RLimit limits = new Interop.Sys.RLimit{ CurrentLimit = maxFd, MaximumLimit = maxFd};

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
                            watcher.EnableRaisingEvents = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    // If we use all handles we may not have luck writing out errors.
                    // Try our best here. _output is not available within RemoteExec().
                    Console.WriteLine($"Test failed for count={count}, limnit={maxFd} and path='{testDirectory}'.");
                    Console.WriteLine(e.Message);
                    return 1;
                }

                return RemoteExecutor.SuccessExitCode;
            }, _count.ToString(), _limit.ToString(), _path, options).Dispose();
        }
    }

    public partial class DangerousFileSystemWatcherTests
    {
        [ConditionalFact]
        [OuterLoop("Slow test with significant resource usage.")]
        public void FileSystemWatcher_Unix_DoesNotLeak()
        {
            Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, out Interop.Sys.RLimit limits);
            _output.WriteLine("File descriptor limit is {0}", limits.CurrentLimit);
            _output.WriteLine($"Starting 100/200 test on {TestDirectory}");
            RunTestHelper helper = new RunTestHelper(100, 200,  TestDirectory);
            helper.ExecuteTest();
        }
    }
}
