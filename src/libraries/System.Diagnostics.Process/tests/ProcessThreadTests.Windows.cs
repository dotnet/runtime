// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessThreadTests : ProcessTestBase
    {
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invokes
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void PriorityLevel_Roundtrips()
        {
            using (Barrier b = new Barrier(2))
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                int targetThreadId = 0;

                // Launch a thread whose priority we'll manipulate.
                var t = new Thread(() =>
                {
                    targetThreadId = GetCurrentThreadId();
                    b.SignalAndWait();
                    b.SignalAndWait(); // wait until the main test is done targeting this thread
                });
                t.IsBackground = true;
                t.Start();

                b.SignalAndWait(); // wait until targetThreadId is valid
                try
                {
                    // Find the relevant ProcessThread in this process
                    ProcessThread targetThread = currentProcess.Threads.Cast<ProcessThread>().Single(pt => pt.Id == targetThreadId);

                    // Try setting and getting its priority
                    foreach (ThreadPriorityLevel level in new[] { ThreadPriorityLevel.AboveNormal, ThreadPriorityLevel.BelowNormal, ThreadPriorityLevel.Normal })
                    {
                        targetThread.PriorityLevel = ThreadPriorityLevel.AboveNormal;
                        Assert.Equal(ThreadPriorityLevel.AboveNormal, targetThread.PriorityLevel);
                    }
                }
                finally
                {
                    // Allow the thread to exit
                    b.SignalAndWait();
                }

                t.Join();
            }
        }

        [DllImport("kernel32")]
        private extern static int GetCurrentThreadId();
    }
}
