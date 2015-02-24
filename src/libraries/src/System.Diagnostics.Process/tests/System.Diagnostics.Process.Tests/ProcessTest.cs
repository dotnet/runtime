// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using System.Threading;
using Microsoft.Win32.SafeHandles;
using Xunit;
using System.Text;

namespace System.Diagnostics.ProcessTests
{
    public partial class ProcessTest
    {
        static ProcessTest()
        {
            TryKillExistingTestProcesses();
        }

        private static void TryKillExistingTestProcesses()
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName(s_ProcessName.Replace(".exe", "")))
                {
                    p.Kill();
                }
            }
            catch { }; // do nothing 
        }

        private const string s_ProcessName = "ProcessTest_ConsoleApp.exe";
        static Process CreateProcessInfinite()
        {
            // Create a process that will not exit 
            Process p = new Process();
            p.StartInfo.FileName = s_ProcessName;
            p.StartInfo.Arguments = "infinite";
            return p;
        }

        static Process CreateProcessError()
        {
            // Create a process that will fail and write to std error stream
            Process p = new Process();
            p.StartInfo.FileName = s_ProcessName;
            p.StartInfo.Arguments = "error";
            return p;
        }

        static Process CreateProcessInput()
        {
            // Create a process that reads one line from std in, writes it back out to the console, and exits cleanly
            Process p = new Process();
            p.StartInfo.FileName = s_ProcessName;
            p.StartInfo.Arguments = "input";
            return p;
        }

        static Process CreateProcess()
        {
            // Create a process that immediately exits
            Process p = new Process();
            p.StartInfo.FileName = s_ProcessName;
            return p;
        }

        [Fact, ActiveIssue(541)]
        public static void Process_BasePriority()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.Equal(8, p.BasePriority);
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        private static bool s_Process_EnableRaiseEvents_isExitedEventHandlerCalled = false;

        [Fact]
        public static void Process_EnableRaiseEvents()
        {
            // Test behavior when EnableRaisingEvent = true;
            // Ensure event is called.
            Process p = CreateProcessInfinite();
            p.EnableRaisingEvents = true;
            p.Exited += p_Exited;
            p.Start();
            p.Kill();
            p.WaitForExit();
            Assert.True(s_Process_EnableRaiseEvents_isExitedEventHandlerCalled, String.Format("Process_CanRaiseEvents0001: {0}", "isExited Event not called when EnableRaisingEvent is set to true."));

            s_Process_EnableRaiseEvents_isExitedEventHandlerCalled = false;

            // Check with the default settings (false, events will not be raised)
            p.Refresh();
            p = CreateProcessInfinite();
            p.Exited += p_Exited;
            p.Start();
            p.Kill();
            p.WaitForExit();
            Assert.False(s_Process_EnableRaiseEvents_isExitedEventHandlerCalled, String.Format("Process_CanRaiseEvents0002: {0}", "isExited Event called with the default settings for EnableRaiseEvents"));

            s_Process_EnableRaiseEvents_isExitedEventHandlerCalled = false;

            // Same test, this time explicitly set the property to false
            p = CreateProcessInfinite();
            p.EnableRaisingEvents = false;
            p.Exited += p_Exited;
            p.Start();
            p.Kill();
            p.WaitForExit();
            Assert.False(s_Process_EnableRaiseEvents_isExitedEventHandlerCalled, String.Format("Process_CanRaiseEvents0003: {0}", "isExited Event called with the EnableRaiseEvents = false"));

            s_Process_EnableRaiseEvents_isExitedEventHandlerCalled = false;
        }

        static void p_Exited(object sender, EventArgs e)
        {
            s_Process_EnableRaiseEvents_isExitedEventHandlerCalled = true;
        }

        [Fact]
        public static void Process_ExitCode()
        {
            Process p = CreateProcess();
            p.Start();
            p.WaitForExit();
            Assert.Equal(p.ExitCode, 100);

            p = CreateProcessInfinite();
            p.Start();
            p.Kill();
            p.WaitForExit();
            Assert.True(p.ExitCode < 0, String.Format("Process_ExitCode: Unexpected Exit code {0}", p.ExitCode));
        }

        [Fact]
        public static void Process_ExitTime()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            p.Kill();
            p.WaitForExit();
            DateTime exitTime = p.ExitTime;
            DateTime dt2 = DateTime.Now;
            TimeSpan elapsedTime = new TimeSpan(dt2.Ticks - exitTime.Ticks);
            Assert.True(elapsedTime.TotalSeconds <= 1, "Process_ExitTime is incorrect.");
        }



        [Fact]
        public static void Process_GetHandle()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.Equal(p.Id, Interop.GetProcessId(p.SafeHandle));
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_HasExited()
        {
            Process p = CreateProcess();
            p.Start();
            p.WaitForExit();
            Assert.True(p.HasExited, "Process_HasExited001 failed");

            p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.False(p.HasExited, "Process_HasExited002 failed");
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
            
            Assert.True(p.HasExited, "Process_HasExited003 failed");
        }

        [Fact]
        public static void Process_MachineName()
        {
            // Checking that the MachineName returns some value.
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Console.WriteLine(p.MachineName);
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_MainModule()
        {
            // Get MainModule property from a Process object
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                ProcessModule pMainModule = p.MainModule;
                Assert.Equal(s_ProcessName, pMainModule.ModuleName);
                Assert.True(pMainModule.FileName.Contains(pMainModule.ModuleName), "MainModule FileName invalid. " + GetModuleDescription(pMainModule));

                // Check that the mainModule is present in the modules list.
                ProcessModuleCollection allModules = p.Modules;
                bool foundMainModule = false;
                foreach (ProcessModule pModule in allModules)
                {
                    if ((pModule.BaseAddress == pMainModule.BaseAddress) && pModule.FileName.Equals(pMainModule.FileName))
                    {
                        foundMainModule = true;
                        break;
                    }
                }
                Assert.True(foundMainModule, "MainModule incorrect: " + GetModuleDescription(pMainModule) + Environment.NewLine + GetModulesDescription(allModules));
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        private static string GetModuleDescription(ProcessModule module)
        {
            return string.Format("Module: Base:{0} FileName:{1} ModuleName:{2}", module.BaseAddress, module.FileName, module.ModuleName);
        }

        private static string GetModulesDescription(ProcessModuleCollection modules)
        {
            var text = new StringBuilder();
            text.AppendLine("Modules Collection:");
            foreach (ProcessModule module in modules)
            {
                text.Append("    ");
                text.AppendLine(GetModuleDescription(module));
            }
            return text.ToString();
        }

        [Fact]
        public static void Process_MaxWorkingSet()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.NotNull(p.MaxWorkingSet);
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_MinWorkingSet()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.NotNull(p.MinWorkingSet);
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_Modules()
        {
            // Checking that the MachineName returns some value.
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                foreach (ProcessModule pModule in p.Modules)
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
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }
        [Fact]
        public static void Process_WorkingSet()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                IntPtr minWorkingSet, maxWorkingset;
                uint flags;
                Interop.GetProcessWorkingSetSizeEx(p.SafeHandle, out minWorkingSet, out maxWorkingset, out flags);
                Assert.Equal(p.MinWorkingSet, minWorkingSet);
                Assert.Equal(p.MaxWorkingSet, maxWorkingset);
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }


        [Fact]
        public static void Process_NonpagedSystemMemorySize64()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                long x = p.NonpagedSystemMemorySize64;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PagedMemorySize64()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                long x = p.PagedMemorySize64;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PagedSystemMemorySize64()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                long x = p.PagedSystemMemorySize64;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PeakPagedMemorySize64()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                long x = p.PeakPagedMemorySize64;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PeakVirtualMemorySize64()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                long x = p.PeakVirtualMemorySize64;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PeakWorkingSet64()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                long x = p.PeakWorkingSet64;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PrivateMemorySize64()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                long x = p.PrivateMemorySize64;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PrivilegedProcessorTime()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                TimeSpan x = p.PrivilegedProcessorTime;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_ProcessorAffinity()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                IntPtr x = p.ProcessorAffinity;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_SessionId()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                int x = p.SessionId;
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_PriorityBoostEnabled()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.True(p.PriorityBoostEnabled, "Process_PriorityBoostEnabled001 failed");
                p.PriorityBoostEnabled = false;
                Assert.False(p.PriorityBoostEnabled, "Process_PriorityBoostEnabled002 failed");
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact, ActiveIssue(541)]
        public static void Process_PriorityClass()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.Equal(p.PriorityClass, ProcessPriorityClass.Normal);
                p.PriorityClass = ProcessPriorityClass.High;
                Assert.Equal(p.PriorityClass, ProcessPriorityClass.High);
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_InvalidPriorityClass()
        {
            Process p = new Process();
            Assert.Throws<ArgumentException>(() => { p.PriorityClass = ProcessPriorityClass.Normal | ProcessPriorityClass.Idle; });
        }

        [Fact]
        public static void Process_ProcessName()
        {
            Process p = CreateProcessInfinite();
            p.Start();
            try
            {
                Assert.Equal(p.ProcessName, "ProcessTest_ConsoleApp");
            }
            finally
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public static void Process_GetCurrentProcess()
        {
            string processName = Process.GetCurrentProcess().ProcessName;
            Assert.Equal(processName, "CoreRun");
        }

        [Fact]
        public static void Process_GetProcessById()
        {
            Process p = Process.GetCurrentProcess();
            Process p_copy = Process.GetProcessById(p.Id);
            Assert.Equal(p.ProcessName, p_copy.ProcessName);
            Assert.Equal(p.Id, p_copy.Id);
        }

        [Fact]
        public static void Process_GetProcesses()
        {
            // Get all the processes running on the machine.
            Process currentProcess = Process.GetCurrentProcess();
            bool foundCurrentProcess = false;
            foreach (Process p in Process.GetProcesses())
            {
                if ((p.Id == currentProcess.Id) && (p.ProcessName.Equals(currentProcess.ProcessName)))
                    foundCurrentProcess = true;
            }
            Assert.True(foundCurrentProcess, "Process_GetProcesses001 failed");

            foundCurrentProcess = false;
            string machineName = currentProcess.MachineName;
            foreach (Process p in Process.GetProcesses(machineName))
            {
                if ((p.Id == currentProcess.Id) && (p.ProcessName.Equals(currentProcess.ProcessName)))
                    foundCurrentProcess = true;
            }
            Assert.True(foundCurrentProcess, "Process_GetProcesses002 failed");
        }

        [Fact]
        public static void Process_GetProcessesByName()
        {
            // Get the current process using its name
            Process currentProcess = Process.GetCurrentProcess();
            string processName = currentProcess.ProcessName;
            string machineName = currentProcess.MachineName;
            bool found = false;
            foreach (Process p in Process.GetProcessesByName(processName))
            {
                found = true;
                break;
            }
            Assert.True(found, "Process_GetProcessesByName001 failed");

            found = false;
            foreach (Process p in Process.GetProcessesByName(processName, machineName))
            {
                found = true;
                break;
            }
            Assert.True(found, "Process_GetProcessesByName002 failed");
        }

        [Fact]
        public static void Process_Environment()
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            // Creating a detached ProcessStartInfo will pre-populate the environment
            // with current environmental variables. 

            // When used with an existing Process.ProcessStartInfo the following behavior
            //  * Desktop - Populates with current EnvironmentVariable
            //  * Project K - Does NOT pre-populate environment.

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
            Assert.Equal("NewValue", stringout);
            Assert.True(retval);

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
            Assert.False(Environment2.Contains(new System.Collections.Generic.KeyValuePair<string, string>("newkey2", "NewValue2")));
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
    }
}