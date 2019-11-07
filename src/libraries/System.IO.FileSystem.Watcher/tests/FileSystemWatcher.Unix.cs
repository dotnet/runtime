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
using Xunit;
using Xunit.Abstractions;

namespace System.IO.Tests
{
    public partial class DangerousFileSystemWatcherTests
    {
        [ConditionalFact]
        [OuterLoop("Slow test with significant resource usage.")]
        public void FileSystemWatcher_Unix_DoesNotLeak()
        {
            Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, out Interop.Sys.RLimit limits);
            _output.WriteLine("File descriptor limit is {0}", limits.CurrentLimit);

            if (limits.CurrentLimit > 50_000)
            {
                throw new SkipTestException($"File descriptor limit is too high {limits.CurrentLimit}.");
            }

            try {
                for (int i = 0; i < (int)limits.CurrentLimit; i++)
                {
                    using (var watcher = new FileSystemWatcher(TestDirectory))
                    {
                        watcher.Created += (s, e) => { } ;
                        watcher.EnableRaisingEvents = true;
                    }

                    // On some OSes system resource is released in finalizer.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception e)
            {
                // If we use all handles we may not have luck writing out errors.
                // Try our best here.
                Console.WriteLine(e.Message);
                _output.WriteLine(e.Message);
                throw;
            }
        }
    }
}
