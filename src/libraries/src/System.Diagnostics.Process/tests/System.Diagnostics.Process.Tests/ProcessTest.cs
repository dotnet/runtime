// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public partial class ProcessTest : IDisposable
    {
        private const int WaitInMS = 100 * 1000; 
        private const string CoreRunName = "corerun";
        private const string TestExeName = "System.Diagnostics.Process.TestConsoleApp.exe";

        private Process _process;
        private List<Process> _processes = new List<Process>();

        public ProcessTest()
        {
            _process = CreateProcessInfinite();
            _process.Start();
        }

        public void Dispose()
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

        Process CreateProcess(string optionalArgument = ""/*String.Empty is not a constant*/)
        {
            Process p = new Process();
            _processes.Add(p);

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

        Process CreateProcessInfinite()
        {
            return CreateProcess("infinite");
        }

        public void SetAndCheckBasePriority(ProcessPriorityClass exPriorityClass, int priority)
        {
            _process.PriorityClass = exPriorityClass;
            _process.Refresh();
            Assert.Equal(priority, _process.BasePriority);
        }

        [Fact]
        public void Process_BasePriority()
        {
            ProcessPriorityClass originalPriority = _process.PriorityClass;

            try
            {
                //SetAndCheckBasePriority(ProcessPriorityClass.RealTime, 24);
                SetAndCheckBasePriority(ProcessPriorityClass.High, 13);
                SetAndCheckBasePriority(ProcessPriorityClass.Idle, 4);
                SetAndCheckBasePriority(ProcessPriorityClass.Normal, 8);
            }
            finally
            {
                _process.PriorityClass = originalPriority;
            }
        }

        public void Sleep(double delayInMs)
        {
            Task.Delay(TimeSpan.FromMilliseconds(delayInMs)).Wait();
        }

        public void Sleep()
        {
            Sleep(50D);
        }

        public void StartAndKillProcessWithDelay(Process p)
        {
            p.Start();
            Sleep();
            p.Kill();
            Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact]
        public void Process_EnableRaiseEvents()
        {
            {
                bool isExitedInvoked = false;

                // Test behavior when EnableRaisingEvent = true;
                // Ensure event is called.
                Process p = CreateProcessInfinite();
                p.EnableRaisingEvents = true;
                p.Exited += delegate { isExitedInvoked = true; };
                StartAndKillProcessWithDelay(p);
                Assert.True(isExitedInvoked, String.Format("Process_CanRaiseEvents0001: {0}", "isExited Event not called when EnableRaisingEvent is set to true."));
            }

            {
                bool isExitedInvoked = false;

                // Check with the default settings (false, events will not be raised)
                Process p = CreateProcessInfinite();
                p.Exited += delegate { isExitedInvoked = true; };
                StartAndKillProcessWithDelay(p);
                Assert.False(isExitedInvoked, String.Format("Process_CanRaiseEvents0002: {0}", "isExited Event called with the default settings for EnableRaiseEvents"));
            }

            {
                bool isExitedInvoked = false;

                // Same test, this time explicitly set the property to false
                Process p = CreateProcessInfinite();
                p.EnableRaisingEvents = false;
                p.Exited += delegate { isExitedInvoked = true; }; ;
                StartAndKillProcessWithDelay(p);
                Assert.False(isExitedInvoked, String.Format("Process_CanRaiseEvents0003: {0}", "isExited Event called with the EnableRaiseEvents = false"));
            }
        }

        [Fact]
        public void Process_ExitCode()
        {
            {
                Process p = CreateProcess();
                p.Start();
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(p.ExitCode, 100);
            }

            {
                Process p = CreateProcessInfinite();
                StartAndKillProcessWithDelay(p);
                Assert.True(p.ExitCode < 0, String.Format("Process_ExitCode: Unexpected Exit code {0}", p.ExitCode));
            }
        }

        [Fact]
        public void Process_ExitTime()
        {
            DateTime timeBeforeProcessStart = DateTime.UtcNow;
            Process p = CreateProcessInfinite();
            StartAndKillProcessWithDelay(p);
            Assert.True(p.ExitTime.ToUniversalTime() > timeBeforeProcessStart, "Process_ExitTime is incorrect.");
        }


        [Fact]
        public void Process_GetHandle()
        {
            Assert.Equal(_process.Id, Interop.GetProcessId(_process.SafeHandle));
        }

        [Fact]
        public void Process_HasExited()
        {
            {
                Process p = CreateProcess();
                p.Start();
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.True(p.HasExited, "Process_HasExited001 failed");
            }

            {
                Process p = CreateProcessInfinite();
                p.Start();
                try
                {
                    Assert.False(p.HasExited, "Process_HasExited002 failed");
                }
                finally
                {
                    p.Kill();
                    Assert.True(p.WaitForExit(WaitInMS));
                }

                Assert.True(p.HasExited, "Process_HasExited003 failed");
            }
        }

        [Fact]
        public void Process_MachineName()
        {
            // Checking that the MachineName returns some value.
            Assert.NotNull(_process.MachineName);
        }

        [Fact]
        [ActiveIssue(606)]
        public void Process_MainModule()
        {
            // Get MainModule property from a Process object
            string moduleName = _process.MainModule.ModuleName;
            Assert.Equal(CoreRunName, moduleName);

            // Check that the mainModule is present in the modules list.
            bool foundMainModule = false;
            foreach (ProcessModule pModule in _process.Modules)
            {
                if (String.Equals(moduleName, CoreRunName, StringComparison.OrdinalIgnoreCase))
                {
                    foundMainModule = true;
                    break;
                }
            }
            Assert.True(foundMainModule, "Could not found Module " + moduleName);
        }

        [Fact]
        public void Process_MaxWorkingSet()
        {
            IntPtr min, max;
            uint flags;

            int intCurrValue = (Int32)_process.MaxWorkingSet;

            try
            {
                _process.MaxWorkingSet = (IntPtr)(intCurrValue + 1024);
                Interop.GetProcessWorkingSetSizeEx(_process.SafeHandle, out min, out max, out flags);
                intCurrValue = (int)max;
                _process.Refresh();
                Assert.Equal(intCurrValue, (int)_process.MaxWorkingSet);
            }
            finally
            {
                _process.MaxWorkingSet = (IntPtr)intCurrValue;
            }
        }

        [Fact]
        public void Process_MinWorkingSet()
        {
            int intCurrValue = (Int32)_process.MinWorkingSet;
            IntPtr min;
            IntPtr max;
            uint flags;

            try
            {
                _process.MinWorkingSet = (IntPtr)(intCurrValue - 1024);
                Interop.GetProcessWorkingSetSizeEx(_process.SafeHandle, out min, out max, out flags);
                intCurrValue = (int)min;
                _process.Refresh();
                Assert.Equal(intCurrValue, (int)_process.MinWorkingSet);
            }
            finally
            {
                _process.MinWorkingSet = (IntPtr)intCurrValue;
            }
        }

        [Fact]
        public void Process_Modules()
        {
            foreach (ProcessModule pModule in _process.Modules)
            {
                // Validated that we can get a value for each of the following.
                Assert.NotNull(pModule);
                Assert.NotNull(pModule.BaseAddress);
                Assert.NotNull(pModule.EntryPointAddress);
                Assert.NotNull(pModule.FileName);
                int memSize = pModule.ModuleMemorySize;
                Assert.NotNull(pModule.ModuleName);
            }
        }

        public void DoNothing(TimeSpan ignoreValue)
        {
            // This method does nothing.
        }

        [Fact]
        public void Process_NonpagedSystemMemorySize64()
        {
            Assert.NotEqual(0L, _process.NonpagedSystemMemorySize64);
        }

        [Fact]
        public void Process_PagedMemorySize64()
        {
            Assert.NotEqual(0L, _process.PagedMemorySize64);
        }

        [Fact]
        public void Process_PagedSystemMemorySize64()
        {
            Assert.NotEqual(0L, _process.PagedSystemMemorySize64);
        }

        [Fact]
        public void Process_PeakPagedMemorySize64()
        {
            Assert.NotEqual(0L, _process.PeakPagedMemorySize64);
        }

        [Fact]
        public void Process_PeakVirtualMemorySize64()
        {
            Assert.NotEqual(0L, _process.PeakVirtualMemorySize64);
        }

        [Fact]
        public void Process_PeakWorkingSet64()
        {
            Assert.NotEqual(0L, _process.PeakWorkingSet64);
        }

        [Fact]
        public void Process_PrivateMemorySize64()
        {
            Assert.NotEqual(0L, _process.PrivateMemorySize64);
        }

        [Fact]
        public void Process_PrivilegedProcessorTime()
        {
            // There is no good way to test the actual values of these
            // w/o the user of Performance Counters or a ton of pinvokes, and so for now we simply check
            // they do not throw exception when called.
            DoNothing(_process.UserProcessorTime);
            DoNothing(_process.PrivilegedProcessorTime);
            DoNothing(_process.TotalProcessorTime);
        }

        [Fact]
        public void Process_ProcessorAffinity()
        {
            IntPtr curProcessorAffinity = _process.ProcessorAffinity;
            try
            {
                _process.ProcessorAffinity = new IntPtr(0x1);
                Assert.Equal(new IntPtr(0x1), _process.ProcessorAffinity);
            }
            finally
            {
                _process.ProcessorAffinity = curProcessorAffinity;
                Assert.Equal(curProcessorAffinity, _process.ProcessorAffinity);
            }
        }

        [Fact]
        public void Process_PriorityBoostEnabled()
        {
            bool isPriorityBoostEnabled = _process.PriorityBoostEnabled;

            try
            {
                _process.PriorityBoostEnabled = true;
                Assert.True(_process.PriorityBoostEnabled, "Process_PriorityBoostEnabled001 failed");

                _process.PriorityBoostEnabled = false;
                Assert.False(_process.PriorityBoostEnabled, "Process_PriorityBoostEnabled002 failed");
            }

            finally
            {
                _process.PriorityBoostEnabled = isPriorityBoostEnabled;
            }

        }

        public void Process_PriorityClass()
        {

            ProcessPriorityClass priorityClass = _process.PriorityClass;

            try
            {
                _process.PriorityClass = ProcessPriorityClass.High;
                Assert.Equal(_process.PriorityClass, ProcessPriorityClass.High);

                _process.PriorityClass = ProcessPriorityClass.Normal;
                Assert.Equal(_process.PriorityClass, ProcessPriorityClass.Normal);
            }
            finally
            {
                _process.PriorityClass = priorityClass;
            }
        }

        [Fact]
        public void Process_InvalidPriorityClass()
        {
            Process p = new Process();
            Assert.Throws<ArgumentException>(() => { p.PriorityClass = ProcessPriorityClass.Normal | ProcessPriorityClass.Idle; });
        }

        [Fact]
        public void ProcessProcessName()
        {
            Assert.Equal(_process.ProcessName, CoreRunName, StringComparer.OrdinalIgnoreCase);
        }


        [DllImport("api-ms-win-core-processthreads-l1-1-0.dll")]
        internal static extern int GetCurrentProcessId();

        [Fact]
        public void Process_GetCurrentProcess()
        {
            int currentProcessId = ProcessTest.GetCurrentProcessId();

            Assert.Equal(currentProcessId, Process.GetCurrentProcess().Id);
            Assert.Equal(Process.GetProcessById(currentProcessId).ProcessName, Process.GetCurrentProcess().ProcessName);
        }

        [Fact]
        public void Process_GetProcesses()
        {
            // Get all the processes running on the machine.
            Process currentProcess = Process.GetCurrentProcess();

            var foundCurrentProcess = (from p in Process.GetProcesses()
                            where (p.Id == currentProcess.Id) && (p.ProcessName.Equals(currentProcess.ProcessName))
                            select p).Any();

            Assert.True(foundCurrentProcess, "Process_GetProcesses001 failed");

            foundCurrentProcess = (from p in Process.GetProcesses(currentProcess.MachineName)
                                  where (p.Id == currentProcess.Id) && (p.ProcessName.Equals(currentProcess.ProcessName))
                                  select p).Any();
            Assert.True(foundCurrentProcess, "Process_GetProcesses002 failed");
        }

        [Fact]
        public void Process_GetProcessesByName()
        {
            // Get the current process using its name
            Process currentProcess = Process.GetCurrentProcess();

            Assert.True(Process.GetProcessesByName(currentProcess.ProcessName).Count() > 0, "Process_GetProcessesByName001 failed");
            Assert.True(Process.GetProcessesByName(currentProcess.ProcessName, currentProcess.MachineName).Count() > 0, "Process_GetProcessesByName001 failed");
        }

        [Fact]
        public void Process_Environment()
        {
            Assert.NotEqual(0, new Process().StartInfo.Environment.Count);

            ProcessStartInfo psi = new ProcessStartInfo();

            // Creating a detached ProcessStartInfo will pre-populate the environment
            // with current environmental variables.

            var Environment2 = psi.Environment;

            Assert.NotEqual(Environment2.Count, 0);

            int CountItems = Environment2.Count;

            Environment2.Add("NewKey", "NewValue");
            Environment2.Add("NewKey2", "NewValue2");

            Assert.Equal(CountItems + 2, Environment2.Count);
            Environment2.Remove("NewKey");
            Assert.Equal(CountItems + 1, Environment2.Count);

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentException>(() => { Environment2.Add("NewKey2", "NewValue2"); });

            //Clear
            Environment2.Clear();
            Assert.Equal(0, Environment2.Count);

            //ContainsKey 
            Environment2.Add("NewKey", "NewValue");
            Environment2.Add("NewKey2", "NewValue2");
            Assert.True(Environment2.ContainsKey("NewKey"));
            Assert.True(Environment2.ContainsKey("newkey")); // Windows is case-insensitive (will need to adapt this when we support tests on Unix)
            Assert.False(Environment2.ContainsKey("NewKey99"));

            //Iterating
            string result = null;
            int index = 0;
            foreach (string e1 in Environment2.Values)
            {
                index++;
                result += e1;
            }
            Assert.Equal(2, index);
            Assert.Equal("NewValueNewValue2", result);

            result = null;
            index = 0;
            foreach (string e1 in Environment2.Keys)
            {
                index++;
                result += e1;
            }
            Assert.Equal("NewKeyNewKey2", result);
            Assert.Equal(2, index);

            result = null;
            index = 0;
            foreach (System.Collections.Generic.KeyValuePair<string, string> e1 in Environment2)
            {
                index++;
                result += e1.Key;
            }
            Assert.Equal("NewKeyNewKey2", result);
            Assert.Equal(2, index);

            //Contains
            Assert.True(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("NewKey", "NewValue")));
            Assert.True(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("nEwKeY", "NewValue"))); // case-insensitive keys on Windows
            Assert.False(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("NewKey99", "NewValue99")));

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() =>
            {
                Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>(null, "NewValue99"));
            }
            );

            Environment2.Add(new System.Collections.Generic.KeyValuePair<string, string>("NewKey98", "NewValue98"));

            //Indexed
            string newIndexItem = Environment2["NewKey98"];
            Assert.Equal("NewValue98", newIndexItem);

            //TryGetValue
            string stringout = null;
            bool retval = false;
            retval = Environment2.TryGetValue("NewKey", out stringout);
            Assert.True(retval);
            Assert.Equal("NewValue", stringout);
            retval = Environment2.TryGetValue("NeWkEy", out stringout);
            Assert.True(retval);
            Assert.Equal("NewValue", stringout);

            stringout = null;
            retval = false;
            retval = Environment2.TryGetValue("NewKey99", out stringout);
            Assert.Equal(null, stringout);
            Assert.False(retval);

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() =>
            {
                string stringout1 = null;
                bool retval1 = false;
                retval1 = Environment2.TryGetValue(null, out stringout1);
            }
            );

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() =>
            {
                Environment2.Add(null, "NewValue2");
            }
            );

            //Invalid Key to add
            Assert.Throws<ArgumentException>(() =>
            {
                Environment2.Add("NewKey2", "NewValue2");
            }
            );
            //Remove Item
            Environment2.Remove("NewKey98");
            Environment2.Remove("NewKey98");   //2nd occurrence should not assert

            //Exception not thrown with null key
            Assert.Throws<ArgumentNullException>(() => { Environment2.Remove(null); });

            //"Exception not thrown with null key"
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => { string a1 = Environment2["1bB"]; });

            Assert.True(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("NewKey2", "NewValue2")));
            Assert.True(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("NEWKeY2", "NewValue2"))); // case-insensitive keys on Windows
            Assert.False(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("NewKey2", "newvalue2")));
            Assert.False(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("newkey2", "newvalue2")));

            //Use KeyValuePair Enumerator
            var x = Environment2.GetEnumerator();
            x.MoveNext();
            var y1 = x.Current;
            Assert.Equal("NewKey NewValue", y1.Key + " " + y1.Value);
            x.MoveNext();
            y1 = x.Current;
            Assert.Equal("NewKey2 NewValue2", y1.Key + " " + y1.Value);

            //IsReadonly
            Assert.False(Environment2.IsReadOnly);

            Environment2.Add(new System.Collections.Generic.KeyValuePair<string, string>("NewKey3", "NewValue3"));
            Environment2.Add(new System.Collections.Generic.KeyValuePair<string, string>("NewKey4", "NewValue4"));


            //CopyTo
            System.Collections.Generic.KeyValuePair<String, String>[] kvpa = new System.Collections.Generic.KeyValuePair<string, string>[10];
            Environment2.CopyTo(kvpa, 0);
            Assert.Equal("NewKey", kvpa[0].Key);
            Assert.Equal("NewKey3", kvpa[2].Key);

            Environment2.CopyTo(kvpa, 6);
            Assert.Equal("NewKey", kvpa[6].Key);

            //Exception not thrown with null key
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { Environment2.CopyTo(kvpa, -1); });

            //Exception not thrown with null key
            Assert.Throws<System.ArgumentException>(() => { Environment2.CopyTo(kvpa, 9); });

            //Exception not thrown with null key
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                System.Collections.Generic.KeyValuePair<String, String>[] kvpanull = null;
                Environment2.CopyTo(kvpanull, 0);
            }
            );
        }

        [Fact]
        public void Process_StartInfo()
        {
            {
                Process process = CreateProcessInfinite();
                process.Start();

                Assert.Equal(CoreRunName, process.StartInfo.FileName);

                process.Kill();
                Assert.True(process.WaitForExit(WaitInMS));
            }

            {
                Process process = CreateProcessInfinite();
                process.Start();

                Assert.Throws<System.InvalidOperationException>(() => (process.StartInfo = new ProcessStartInfo()));

                process.Kill();
                Assert.True(process.WaitForExit(WaitInMS));
            }

            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(TestExeName);
                Assert.Equal(TestExeName, process.StartInfo.FileName);
            }

            {
                Process process = Process.GetCurrentProcess();
                Assert.Throws<System.InvalidOperationException>(() => process.StartInfo);
            }
        }
    }
}