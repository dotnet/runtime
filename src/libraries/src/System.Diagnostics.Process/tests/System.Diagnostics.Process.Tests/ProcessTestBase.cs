// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public class ProcessTestBase : IDisposable
    {
        protected const int WaitInMS = 100 * 1000;
        protected const string CoreRunName = "corerun";
        protected const string TestExeName = "System.Diagnostics.Process.TestConsoleApp.exe";
        protected const int SuccessExitCode = 100;

        protected Process _process;
        protected List<Process> _processes = new List<Process>();

        public ProcessTestBase()
        {
            _process = CreateProcessInfinite();
            _process.Start();
        }

        protected Process CreateProcessInfinite()
        {
            return CreateProcess("infinite");
        }

        protected Process CreateProcess(string optionalArgument = ""/*String.Empty is not a constant*/)
        {
            Process p = new Process();
            lock (_processes)
            {
                _processes.Add(p);
            }

            p.StartInfo.FileName = CoreRunName;
            p.StartInfo.Arguments = string.IsNullOrWhiteSpace(optionalArgument) ?
                TestExeName :
                TestExeName + " " + optionalArgument;

            // Profilers / code coverage tools doing coverage of the test process set environment
            // variables to tell the targeted process what profiler to load.  We don't want the child process 
            // to be profiled / have code coverage, so we remove these environment variables for that process 
            // before it's started.
            p.StartInfo.Environment.Remove("Cor_Profiler");
            p.StartInfo.Environment.Remove("Cor_Enable_Profiling");
            p.StartInfo.Environment.Remove("CoreClr_Profiler");
            p.StartInfo.Environment.Remove("CoreClr_Enable_Profiling");

            return p;
        }

        protected void Sleep(double delayInMs)
        {
            Task.Delay(TimeSpan.FromMilliseconds(delayInMs)).Wait();
        }

        protected void Sleep()
        {
            Sleep(50D);
        }

        protected void StartAndKillProcessWithDelay(Process p)
        {
            p.Start();
            Sleep();
            p.Kill();
            Assert.True(p.WaitForExit(WaitInMS));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                CleanUp();
        }

        internal void CleanUp()
        {
            // Ensure that there are no open processes with the same name as ProcessName
            foreach (Process p in _processes)
            {
                if (!p.HasExited)
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (InvalidOperationException) { } // in case it was never started

                    Assert.True(p.WaitForExit(WaitInMS));
                }
            }
        }
    }
}
