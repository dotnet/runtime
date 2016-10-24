// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        private class FinalizingProcess : Process
        {
            public static volatile bool WasFinalized;

            public static void CreateAndRelease()
            {
                new FinalizingProcess();
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    WasFinalized = true;
                }

                base.Dispose(disposing);
            }
        }

        private void SetAndCheckBasePriority(ProcessPriorityClass exPriorityClass, int priority)
        {
            _process.PriorityClass = exPriorityClass;
            _process.Refresh();
            Assert.Equal(priority, _process.BasePriority);
        }

        private void AssertNonZeroWindowsZeroUnix(long value)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.NotEqual(0, value);
            }
            else
            {
                Assert.Equal(0, value);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TestBasePriorityOnWindows()
        {
            ProcessPriorityClass originalPriority = _process.PriorityClass;
            Assert.Equal(ProcessPriorityClass.Normal, originalPriority);

            try
            {
                // We are not checking for RealTime case here, as RealTime priority process can 
                // preempt the threads of all other processes, including operating system processes
                // performing important tasks, which may cause the machine to be unresponsive.

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

        [Fact] 
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [OuterLoop]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void TestBasePriorityOnUnix()
        {
            ProcessPriorityClass originalPriority = _process.PriorityClass;
            Assert.Equal(ProcessPriorityClass.Normal, originalPriority);

            try
            {
                SetAndCheckBasePriority(ProcessPriorityClass.High, -11);
                SetAndCheckBasePriority(ProcessPriorityClass.Idle, 19);
                SetAndCheckBasePriority(ProcessPriorityClass.Normal, 0);
            }
            finally
            {
                _process.PriorityClass = originalPriority;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void TestEnableRaiseEvents(bool? enable)
        {
            bool exitedInvoked = false;

            Process p = CreateProcessLong();
            if (enable.HasValue)
            {
                p.EnableRaisingEvents = enable.Value;
            }
            p.Exited += delegate { exitedInvoked = true; };
            StartSleepKillWait(p);

            if (enable.GetValueOrDefault())
            {
                // There's no guarantee that the Exited callback will be invoked by
                // the time Process.WaitForExit completes, though it's extremely likely.
                // There could be a race condition where WaitForExit is returning from
                // its wait and sees that the callback is already running asynchronously,
                // at which point it returns to the caller even if the callback hasn't
                // entirely completed. As such, we spin until the value is set.
                Assert.True(SpinWait.SpinUntil(() => exitedInvoked, WaitInMS));
            }
            else
            {
                Assert.False(exitedInvoked);
            }
        }

        [Fact]
        public void TestExitCode()
        {
            {
                Process p = CreateProcess();
                p.Start();
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(SuccessExitCode, p.ExitCode);
            }

            {
                Process p = CreateProcessLong();
                StartSleepKillWait(p);
                Assert.NotEqual(0, p.ExitCode);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void TestUseShellExecute_Unix_Succeeds()
        {
            using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = "exit", Arguments = "42" }))
            {
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(42, p.ExitCode);
            }
        }

        [Fact]
        public void TestExitTime()
        {
            DateTime timeBeforeProcessStart = DateTime.UtcNow;
            Process p = CreateProcessLong();
            p.Start();
            Assert.Throws<InvalidOperationException>(() => p.ExitTime);
            p.Kill();
            Assert.True(p.WaitForExit(WaitInMS));
            Assert.True(p.ExitTime.ToUniversalTime() >= timeBeforeProcessStart, "TestExitTime is incorrect.");
        }

        [Fact]
        public void TestId()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(_process.Id, Interop.GetProcessId(_process.SafeHandle));
            }
            else
            {
                IEnumerable<int> testProcessIds = Process.GetProcessesByName(HostRunner).Select(p => p.Id);
                Assert.Contains(_process.Id, testProcessIds);
            }
        }

        [Fact]
        public void TestHasExited()
        {
            {
                Process p = CreateProcess();
                p.Start();
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.True(p.HasExited, "TestHasExited001 failed");
            }

            {
                Process p = CreateProcessLong();
                p.Start();
                try
                {
                    Assert.False(p.HasExited, "TestHasExited002 failed");
                }
                finally
                {
                    p.Kill();
                    Assert.True(p.WaitForExit(WaitInMS));
                }

                Assert.True(p.HasExited, "TestHasExited003 failed");
            }
        }

        [Fact]
        public void TestMachineName()
        {
            // Checking that the MachineName returns some value.
            Assert.NotNull(_process.MachineName);
        }

        [Fact]
        public void TestMainModuleOnNonOSX()
        {
            string fileName = "corerun";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                fileName = "CoreRun.exe";

            Process p = Process.GetCurrentProcess();
            Assert.True(p.Modules.Count > 0);
            Assert.Equal(fileName, p.MainModule.ModuleName);
            Assert.EndsWith(fileName, p.MainModule.FileName);
            Assert.Equal(string.Format("System.Diagnostics.ProcessModule ({0})", fileName), p.MainModule.ToString());
        }

        [Fact]
        public void TestMaxWorkingSet()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True((long)p.MaxWorkingSet > 0);
                Assert.True((long)p.MinWorkingSet >= 0);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return; // doesn't support getting/setting working set for other processes

            long curValue = (long)_process.MaxWorkingSet;
            Assert.True(curValue >= 0);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    _process.MaxWorkingSet = (IntPtr)((int)curValue + 1024);

                    IntPtr min, max;
                    uint flags;
                    Interop.GetProcessWorkingSetSizeEx(_process.SafeHandle, out min, out max, out flags);
                    curValue = (int)max;
                    _process.Refresh();
                    Assert.Equal(curValue, (int)_process.MaxWorkingSet);
                }
                finally
                {
                    _process.MaxWorkingSet = (IntPtr)curValue;
                }
            }
        }

        [Fact]
        public void TestMinWorkingSet()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True((long)p.MaxWorkingSet > 0);
                Assert.True((long)p.MinWorkingSet >= 0);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return; // doesn't support getting/setting working set for other processes

            long curValue = (long)_process.MinWorkingSet;
            Assert.True(curValue >= 0);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    _process.MinWorkingSet = (IntPtr)((int)curValue - 1024);

                    IntPtr min, max;
                    uint flags;
                    Interop.GetProcessWorkingSetSizeEx(_process.SafeHandle, out min, out max, out flags);
                    curValue = (int)min;
                    _process.Refresh();
                    Assert.Equal(curValue, (int)_process.MinWorkingSet);
                }
                finally
                {
                    _process.MinWorkingSet = (IntPtr)curValue;
                }
            }
        }

        [Fact]
        public void TestModules()
        {
            ProcessModuleCollection moduleCollection = Process.GetCurrentProcess().Modules;
            foreach (ProcessModule pModule in moduleCollection)
            {
                // Validated that we can get a value for each of the following.
                Assert.NotNull(pModule);
                Assert.NotNull(pModule.FileName);
                Assert.NotNull(pModule.ModuleName);

                // Just make sure these don't throw
                IntPtr baseAddr = pModule.BaseAddress;
                IntPtr entryAddr = pModule.EntryPointAddress;
                int memSize = pModule.ModuleMemorySize;
            }
        }

        [Fact]
        public void TestNonpagedSystemMemorySize64()
        {
            AssertNonZeroWindowsZeroUnix(_process.NonpagedSystemMemorySize64);
        }

        [Fact]
        public void TestPagedMemorySize64()
        {
            AssertNonZeroWindowsZeroUnix(_process.PagedMemorySize64);
        }

        [Fact]
        public void TestPagedSystemMemorySize64()
        {
            AssertNonZeroWindowsZeroUnix(_process.PagedSystemMemorySize64);
        }

        [Fact]
        public void TestPeakPagedMemorySize64()
        {
            AssertNonZeroWindowsZeroUnix(_process.PeakPagedMemorySize64);
        }

        [Fact]
        public void TestPeakVirtualMemorySize64()
        {
            AssertNonZeroWindowsZeroUnix(_process.PeakVirtualMemorySize64);
        }

        [Fact]
        public void TestPeakWorkingSet64()
        {
            AssertNonZeroWindowsZeroUnix(_process.PeakWorkingSet64);
        }

        [Fact]
        public void TestPrivateMemorySize64()
        {
            AssertNonZeroWindowsZeroUnix(_process.PrivateMemorySize64);
        }

        [Fact]
        public void TestVirtualMemorySize64()
        {
            Assert.True(_process.VirtualMemorySize64 > 0);
        }

        [Fact]
        public void TestWorkingSet64()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // resident memory can be 0 on OSX.
                Assert.True(_process.WorkingSet64 >= 0);
                return;
            }

            Assert.True(_process.WorkingSet64 > 0);
        }

        [Fact]
        public void TestProcessorTime()
        {
            Assert.True(_process.UserProcessorTime.TotalSeconds >= 0);
            Assert.True(_process.PrivilegedProcessorTime.TotalSeconds >= 0);

            double processorTimeBeforeSpin = Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds;
            double processorTimeAtHalfSpin = 0;
            // Perform loop to occupy cpu, takes less than a second.
            int i = int.MaxValue / 16;
            while (i > 0)
            {
                i--;
                if (i == int.MaxValue / 32)
                    processorTimeAtHalfSpin = Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds;
            }

            Assert.InRange(processorTimeAtHalfSpin, processorTimeBeforeSpin, Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/974
        public void TestProcessStartTime()
        {
            TimeSpan allowedWindow = TimeSpan.FromSeconds(3);
            DateTime testStartTime = DateTime.UtcNow;
            using (var remote = RemoteInvoke(() => { Console.Write(Process.GetCurrentProcess().StartTime.ToUniversalTime()); return SuccessExitCode; },
                new RemoteInvokeOptions { StartInfo = new ProcessStartInfo { RedirectStandardOutput = true } }))
            {
                DateTime remoteStartTime = DateTime.Parse(remote.Process.StandardOutput.ReadToEnd());
                DateTime curTime = DateTime.UtcNow;
                Assert.InRange(remoteStartTime, testStartTime - allowedWindow, curTime + allowedWindow);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/968
        [PlatformSpecific(~TestPlatforms.OSX)] // getting/setting affinity not supported on OSX
        public void TestProcessorAffinity()
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
        public void TestPriorityBoostEnabled()
        {
            bool isPriorityBoostEnabled = _process.PriorityBoostEnabled;
            try
            {
                _process.PriorityBoostEnabled = true;
                Assert.True(_process.PriorityBoostEnabled, "TestPriorityBoostEnabled001 failed");

                _process.PriorityBoostEnabled = false;
                Assert.False(_process.PriorityBoostEnabled, "TestPriorityBoostEnabled002 failed");
            }
            finally
            {
                _process.PriorityBoostEnabled = isPriorityBoostEnabled;
            }
        }

        [Fact] 
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [OuterLoop]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void TestPriorityClassUnix()
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

        [Fact, PlatformSpecific(TestPlatforms.Windows)]
        public void TestPriorityClassWindows()
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
        public void TestInvalidPriorityClass()
        {
            Process p = new Process();
            Assert.Throws<ArgumentException>(() => { p.PriorityClass = ProcessPriorityClass.Normal | ProcessPriorityClass.Idle; });
        }

        [Fact]
        public void TestProcessName()
        {
            Assert.Equal(Path.GetFileNameWithoutExtension(_process.ProcessName), Path.GetFileNameWithoutExtension(HostRunner), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void TestSafeHandle()
        {
            Assert.False(_process.SafeHandle.IsInvalid);
        }

        [Fact]
        public void TestSessionId()
        {
            uint sessionId;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Interop.ProcessIdToSessionId((uint)_process.Id, out sessionId);
            }
            else
            {
                sessionId = (uint)Interop.getsid(_process.Id);
            }

            Assert.Equal(sessionId, (uint)_process.SessionId);
        }

        [Fact]
        public void TestGetCurrentProcess()
        {
            Process current = Process.GetCurrentProcess();
            Assert.NotNull(current);

            int currentProcessId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                Interop.GetCurrentProcessId() :
                Interop.getpid();

            Assert.Equal(currentProcessId, current.Id);
        }

        [Fact]
        public void TestGetProcessById()
        {
            Process p = Process.GetProcessById(_process.Id);
            Assert.Equal(_process.Id, p.Id);
            Assert.Equal(_process.ProcessName, p.ProcessName);
        }

        [Fact]
        public void TestGetProcesses()
        {
            Process currentProcess = Process.GetCurrentProcess();

            // Get all the processes running on the machine, and check if the current process is one of them.
            var foundCurrentProcess = (from p in Process.GetProcesses()
                                       where (p.Id == currentProcess.Id) && (p.ProcessName.Equals(currentProcess.ProcessName))
                                       select p).Any();

            Assert.True(foundCurrentProcess, "TestGetProcesses001 failed");

            foundCurrentProcess = (from p in Process.GetProcesses(currentProcess.MachineName)
                                   where (p.Id == currentProcess.Id) && (p.ProcessName.Equals(currentProcess.ProcessName))
                                   select p).Any();

            Assert.True(foundCurrentProcess, "TestGetProcesses002 failed");
        }

        [Fact]
        public void TestGetProcessesByName()
        {
            // Get the current process using its name
            Process currentProcess = Process.GetCurrentProcess();

            Assert.True(Process.GetProcessesByName(currentProcess.ProcessName).Count() > 0, "TestGetProcessesByName001 failed");
            Assert.True(Process.GetProcessesByName(currentProcess.ProcessName, currentProcess.MachineName).Count() > 0, "TestGetProcessesByName001 failed");
        }

        public static IEnumerable<object[]> GetTestProcess()
        {
            Process currentProcess = Process.GetCurrentProcess();
            yield return new object[] { currentProcess, Process.GetProcessById(currentProcess.Id, "127.0.0.1") };
            yield return new object[] { currentProcess, Process.GetProcessesByName(currentProcess.ProcessName, "127.0.0.1").Where(p => p.Id == currentProcess.Id).Single() };
        }

        private static bool ProcessPeformanceCounterEnabled()
        {
            try
            {
                int? value = (int?)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PerfProc\Performance", "Disable Performance Counters", null);
                return !value.HasValue || value.Value == 0;
            }
            catch (Exception)
            {
                // Ignore exceptions, and just assume the counter is enabled.
            }

            return true;
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalTheory(nameof(ProcessPeformanceCounterEnabled))]
        [MemberData(nameof(GetTestProcess))]
        public void TestProcessOnRemoteMachineWindows(Process currentProcess, Process remoteProcess)
        {
            Assert.Equal(currentProcess.Id, remoteProcess.Id);
            Assert.Equal(currentProcess.BasePriority, remoteProcess.BasePriority);
            Assert.Equal(currentProcess.EnableRaisingEvents, remoteProcess.EnableRaisingEvents);
            Assert.Equal("127.0.0.1", remoteProcess.MachineName);
            // This property throws exception only on remote processes.
            Assert.Throws<NotSupportedException>(() => remoteProcess.MainModule);
        }

        [Fact, PlatformSpecific(TestPlatforms.AnyUnix)]
        public void TestProcessOnRemoteMachineUnix()
        {
            Process currentProcess = Process.GetCurrentProcess();

            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessesByName(currentProcess.ProcessName, "127.0.0.1"));
            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessById(currentProcess.Id, "127.0.0.1"));
        }

        [Fact]
        public void TestStartInfo()
        {
            {
                Process process = CreateProcessLong();
                process.Start();

                Assert.Equal(HostRunner, process.StartInfo.FileName);

                process.Kill();
                Assert.True(process.WaitForExit(WaitInMS));
            }

            {
                Process process = CreateProcessLong();
                process.Start();

                Assert.Throws<System.InvalidOperationException>(() => (process.StartInfo = new ProcessStartInfo()));

                process.Kill();
                Assert.True(process.WaitForExit(WaitInMS));
            }

            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(TestConsoleApp);
                Assert.Equal(TestConsoleApp, process.StartInfo.FileName);
            }

            {
                Process process = new Process();
                Assert.Throws<ArgumentNullException>(() => process.StartInfo = null);
            }

            {
                Process process = Process.GetCurrentProcess();
                Assert.Throws<System.InvalidOperationException>(() => process.StartInfo);
            }
        }
        [Theory]
        [InlineData(@"""abc"" d e", @"abc,d,e")]
        [InlineData(@"""abc""      d e", @"abc,d,e")]
        [InlineData("\"abc\"\t\td\te", @"abc,d,e")]
        [InlineData(@"a\\b d""e f""g h", @"a\\b,de fg,h")]
        [InlineData(@"\ \\ \\\", @"\,\\,\\\")]
        [InlineData(@"a\\\""b c d", @"a\""b,c,d")]
        [InlineData(@"a\\\\""b c"" d e", @"a\\b c,d,e")]
        [InlineData(@"a""b c""d e""f g""h i""j k""l", @"ab cd,ef gh,ij kl")]
        [InlineData(@"a b c""def", @"a,b,cdef")]
        [InlineData(@"""\a\"" \\""\\\ b c", @"\a"" \\\\,b,c")]
        public void TestArgumentParsing(string inputArguments, string expectedArgv)
        {
            using (var handle = RemoteInvokeRaw((Func<string, string, string, int>)ConcatThreeArguments,
                inputArguments,
                new RemoteInvokeOptions { Start = true, StartInfo = new ProcessStartInfo { RedirectStandardOutput = true } }))
            {
                Assert.Equal(expectedArgv, handle.Process.StandardOutput.ReadToEnd());
            }
        }

        private static int ConcatThreeArguments(string one, string two, string three)
        {
            Console.Write(string.Join(",", one, two, three));
            return SuccessExitCode;
        }

        // [Fact] // uncomment for diagnostic purposes to list processes to console
        public void TestDiagnosticsWithConsoleWriteLine()
        {
            foreach (var p in Process.GetProcesses().OrderBy(p => p.Id))
            {
                Console.WriteLine("{0} : \"{1}\" (Threads: {2})", p.Id, p.ProcessName, p.Threads.Count);
                p.Dispose();
            }
        }

        [Fact]
        public void CanBeFinalized()
        {
            FinalizingProcess.CreateAndRelease();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.True(FinalizingProcess.WasFinalized);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestStartWithMissingFile(bool fullPath)
        {
            string path = Guid.NewGuid().ToString("N");
            if (fullPath)
            {
                path = Path.GetFullPath(path);
                Assert.True(Path.IsPathRooted(path));
            }
            else
            {
                Assert.False(Path.IsPathRooted(path));
            }
            Assert.False(File.Exists(path));

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(path));
            Assert.NotEqual(0, e.NativeErrorCode);
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        // NativeErrorCode not 193 on Windows Nano for ERROR_BAD_EXE_FORMAT, issue #10290
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestStartOnWindowsWithBadFileFormat()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(path));
            Assert.NotEqual(0, e.NativeErrorCode);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void TestStartOnUnixWithBadPermissions()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, chmod(path, 644)); // no execute permissions

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(path));
            Assert.NotEqual(0, e.NativeErrorCode);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void TestStartOnUnixWithBadFormat()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, chmod(path, 744)); // execute permissions

            using (Process p = Process.Start(path))
            {
                p.WaitForExit();
                Assert.NotEqual(0, p.ExitCode);
            }
        }

        [DllImport("libc")]
        private static extern int chmod(string path, int mode);
    }
}
