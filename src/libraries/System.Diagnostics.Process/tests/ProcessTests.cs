// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
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
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior varies on Windows and Unix
        public void TestBasePriorityOnWindows()
        {
            CreateDefaultProcess();

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

        [Fact]
        public void TestExitTime()
        {
            // ExitTime resolution on some platforms is less accurate than our DateTime.UtcNow resolution, so
            // we subtract ms from the begin time to account for it.
            DateTime timeBeforeProcessStart = DateTime.UtcNow.AddMilliseconds(-25);
            Process p = CreateProcessLong();
            p.Start();
            Assert.Throws<InvalidOperationException>(() => p.ExitTime);
            p.Kill();
            Assert.True(p.WaitForExit(WaitInMS));

            Assert.True(p.ExitTime.ToUniversalTime() >= timeBeforeProcessStart,
                $@"TestExitTime is incorrect. " +
                $@"TimeBeforeStart {timeBeforeProcessStart} Ticks={timeBeforeProcessStart.Ticks}, " +
                $@"ExitTime={p.ExitTime}, Ticks={p.ExitTime.Ticks}, " +
                $@"ExitTimeUniversal {p.ExitTime.ToUniversalTime()} Ticks={p.ExitTime.ToUniversalTime().Ticks}, " +
                $@"NowUniversal {DateTime.Now.ToUniversalTime()} Ticks={DateTime.Now.Ticks}");
        }

        [Fact]
        public void StartTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.StartTime);
        }

        [Fact]
        public void TestId()
        {
            CreateDefaultProcess();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(_process.Id, Interop.GetProcessId(_process.SafeHandle));
            }
            else
            {
                IEnumerable<int> testProcessIds = Process.GetProcessesByName(HostRunnerName).Select(p => p.Id);
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
        public void HasExited_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.HasExited);
        }

        [Fact]
        public void Kill_NotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.Kill());
        }

        [Fact]
        public void TestMachineName()
        {
            CreateDefaultProcess();
            
            // Checking that the MachineName returns some value.
            Assert.NotNull(_process.MachineName);
        }

        [Fact]
        public void MachineName_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MachineName);
        }

        [Fact]
        public void TestMainModuleOnNonOSX()
        {
            Process p = Process.GetCurrentProcess();
            Assert.True(p.Modules.Count > 0);
            Assert.Equal(HostRunnerName, p.MainModule.ModuleName);
            Assert.EndsWith(HostRunnerName, p.MainModule.FileName);
            Assert.Equal(string.Format("System.Diagnostics.ProcessModule ({0})", HostRunnerName), p.MainModule.ToString());
        }

        [Fact]
        public void TestMaxWorkingSet()
        {
            CreateDefaultProcess();
            
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
        [PlatformSpecific(~TestPlatforms.OSX)] // Getting MaxWorkingSet is not supported on OSX.
        public void MaxWorkingSet_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MaxWorkingSet);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void MaxValueWorkingSet_GetSetMacos_ThrowsPlatformSupportedException()
        {
            var process = new Process();
            Assert.Throws<PlatformNotSupportedException>(() => process.MaxWorkingSet);
            Assert.Throws<PlatformNotSupportedException>(() => process.MaxWorkingSet = (IntPtr)1);
        }

        [Fact]
        public void TestMinWorkingSet()
        {
            CreateDefaultProcess();
            
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
        [PlatformSpecific(~TestPlatforms.OSX)] // Getting MinWorkingSet is not supported on OSX.
        public void MinWorkingSet_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MinWorkingSet);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void MinWorkingSet_GetMacos_ThrowsPlatformSupportedException()
        {
            var process = new Process();
            Assert.Throws<PlatformNotSupportedException>(() => process.MinWorkingSet);
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
            CreateDefaultProcess();
            
            AssertNonZeroWindowsZeroUnix(_process.NonpagedSystemMemorySize64);
        }

        [Fact]
        public void NonpagedSystemMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.NonpagedSystemMemorySize64);
        }

        [Fact]
        public void TestPagedMemorySize64()
        {
            CreateDefaultProcess();

            AssertNonZeroWindowsZeroUnix(_process.PagedMemorySize64);
        }

        [Fact]
        public void PagedMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PagedMemorySize64);
        }

        [Fact]
        public void TestPagedSystemMemorySize64()
        {
            CreateDefaultProcess();

            AssertNonZeroWindowsZeroUnix(_process.PagedSystemMemorySize64);
        }

        [Fact]
        public void PagedSystemMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PagedSystemMemorySize64);
        }

        [Fact]
        public void TestPeakPagedMemorySize64()
        {
            CreateDefaultProcess();

            AssertNonZeroWindowsZeroUnix(_process.PeakPagedMemorySize64);
        }

        [Fact]
        public void PeakPagedMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PeakPagedMemorySize64);
        }

        [Fact]
        public void TestPeakVirtualMemorySize64()
        {
            CreateDefaultProcess();

            AssertNonZeroWindowsZeroUnix(_process.PeakVirtualMemorySize64);
        }

        [Fact]
        public void PeakVirtualMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PeakVirtualMemorySize64);
        }

        [Fact]
        public void TestPeakWorkingSet64()
        {
            CreateDefaultProcess();

            AssertNonZeroWindowsZeroUnix(_process.PeakWorkingSet64);
        }

        [Fact]
        public void PeakWorkingSet64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PeakWorkingSet64);
        }

        [Fact]
        public void TestPrivateMemorySize64()
        {
            CreateDefaultProcess();

            AssertNonZeroWindowsZeroUnix(_process.PrivateMemorySize64);
        }

        [Fact]
        public void PrivateMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PrivateMemorySize64);
        }

        [Fact]
        public void TestVirtualMemorySize64()
        {
            CreateDefaultProcess();

            Assert.True(_process.VirtualMemorySize64 > 0);
        }

        [Fact]
        public void VirtualMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.VirtualMemorySize64);
        }

        [Fact]
        public void TestWorkingSet64()
        {
            CreateDefaultProcess();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // resident memory can be 0 on OSX.
                Assert.True(_process.WorkingSet64 >= 0);
                return;
            }

            Assert.True(_process.WorkingSet64 > 0);
        }

        [Fact]
        public void WorkingSet64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.WorkingSet64);
        }

        [Fact]
        public void TestProcessorTime()
        {
            CreateDefaultProcess();
            
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

        [Fact]
        public void UserProcessorTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.UserProcessorTime);
        }

        [Fact]
        public void PriviledgedProcessorTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PrivilegedProcessorTime);
        }

        [Fact]
        public void TotalProcessorTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.TotalProcessorTime);
        }

        [Fact]
        public void TestProcessStartTime()
        {
            TimeSpan allowedWindow = TimeSpan.FromSeconds(3);
            DateTime testStartTime = DateTime.UtcNow;
            using (var remote = RemoteInvoke(() => { Console.Write(Process.GetCurrentProcess().StartTime.ToUniversalTime()); return SuccessExitCode; },
                new RemoteInvokeOptions { StartInfo = new ProcessStartInfo { RedirectStandardOutput = true } }))
            {
                Assert.Equal(remote.Process.StartTime, remote.Process.StartTime);

                DateTime remoteStartTime = DateTime.Parse(remote.Process.StandardOutput.ReadToEnd());
                DateTime curTime = DateTime.UtcNow;
                Assert.InRange(remoteStartTime, testStartTime - allowedWindow, curTime + allowedWindow);
            }
        }

        [Fact]
        public void ExitTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.ExitTime);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)] // getting/setting affinity not supported on OSX
        public void TestProcessorAffinity()
        {
            CreateDefaultProcess();
            
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
            CreateDefaultProcess();

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
        [PlatformSpecific(TestPlatforms.Windows)] // PriorityBoostEnabled is a no-op on Unix.
        public void PriorityBoostEnabled_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PriorityBoostEnabled);
            Assert.Throws<InvalidOperationException>(() => process.PriorityBoostEnabled = true);
        }

        [Fact, PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior varies on Windows and Unix
        public void TestPriorityClassWindows()
        {
            CreateDefaultProcess();

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

        [Theory]
        [InlineData((ProcessPriorityClass)0)]
        [InlineData(ProcessPriorityClass.Normal | ProcessPriorityClass.Idle)]
        public void TestInvalidPriorityClass(ProcessPriorityClass priorityClass)
        {
            var process = new Process();
            Assert.Throws<InvalidEnumArgumentException>(() => process.PriorityClass = priorityClass);
        }

        [Fact]
        public void PriorityClass_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PriorityClass);
        }

        [Fact]
        public void TestProcessName()
        {
            CreateDefaultProcess();

            // Processes are not hosted by dotnet in the full .NET Framework.
            string expected = PlatformDetection.IsFullFramework ? TestConsoleApp : HostRunner;
            Assert.Equal(Path.GetFileNameWithoutExtension(_process.ProcessName), Path.GetFileNameWithoutExtension(expected), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ProcessName_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.ProcessName);
        }

        [Fact]
        public void TestSafeHandle()
        {
            CreateDefaultProcess();

            Assert.False(_process.SafeHandle.IsInvalid);
        }

        [Fact]
        public void SafeHandle_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.SafeHandle);
        }

        [Fact]
        public void TestSessionId()
        {
            CreateDefaultProcess();

            uint sessionId;
#if TargetsWindows
                Interop.ProcessIdToSessionId((uint)_process.Id, out sessionId);
#else
                sessionId = (uint)Interop.getsid(_process.Id);
#endif

            Assert.Equal(sessionId, (uint)_process.SessionId);
        }

        [Fact]
        public void SessionId_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.SessionId);
        }

        [Fact]
        public void TestGetCurrentProcess()
        {
            Process current = Process.GetCurrentProcess();
            Assert.NotNull(current);

            int currentProcessId =
#if TargetsWindows
                Interop.GetCurrentProcessId();
#else
                Interop.getpid();
#endif

            Assert.Equal(currentProcessId, current.Id);
        }

        [Fact]
        public void TestGetProcessById()
        {
            CreateDefaultProcess();

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
        public void GetProcesseses_NullMachineName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("machineName", () => Process.GetProcesses(null));
        }

        [Fact]
        public void GetProcesses_EmptyMachineName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(null, () => Process.GetProcesses(""));
        }

        [Fact]
        public void GetProcessesByName_ProcessName_ReturnsExpected()
        {
            // Get the current process using its name
            Process currentProcess = Process.GetCurrentProcess();

            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            Assert.NotEmpty(processes);
            Assert.All(processes, process => Assert.Equal(".", process.MachineName));
        }

        public static IEnumerable<object[]> MachineName_TestData()
        {
            string currentProcessName = Process.GetCurrentProcess().MachineName;
            yield return new object[] { currentProcessName };
            yield return new object[] { "." };
            yield return new object[] { Dns.GetHostName() };
        }

        public static IEnumerable<object[]> MachineName_Remote_TestData()
        {
            yield return new object[] { Guid.NewGuid().ToString("N") };
            yield return new object[] { "\\" + Guid.NewGuid().ToString("N") };
        }

        [Theory]
        [MemberData(nameof(MachineName_TestData))]
        public void GetProcessesByName_ProcessNameMachineName_ReturnsExpected(string machineName)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName, machineName);
            Assert.NotEmpty(processes);

            Assert.All(processes, process => Assert.Equal(machineName, process.MachineName));
        }

        [MemberData(nameof(MachineName_Remote_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)] // Accessing processes on remote machines is only supported on Windows.
        public void GetProcessesByName_RemoteMachineNameWindows_ReturnsExpected(string machineName)
        {
            try
            {
                GetProcessesByName_ProcessNameMachineName_ReturnsExpected(machineName);
            }
            catch (InvalidOperationException)
            {
                // As we can't detect reliably if performance counters are enabled 
                // we let possible InvalidOperationExceptions pass silently.
            }
        }

        [Fact]
        public void GetProcessesByName_NoSuchProcess_ReturnsEmpty()
        {
            string processName = Guid.NewGuid().ToString("N");
            Assert.Empty(Process.GetProcessesByName(processName));
        }

        [Fact]
        public void GetProcessesByName_NullMachineName_ThrowsArgumentNullException()
        {
            Process currentProcess = Process.GetCurrentProcess();
            AssertExtensions.Throws<ArgumentNullException>("machineName", () => Process.GetProcessesByName(currentProcess.ProcessName, null));
        }

        [Fact]
        public void GetProcessesByName_EmptyMachineName_ThrowsArgumentException()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Assert.Throws<ArgumentException>(null, () => Process.GetProcessesByName(currentProcess.ProcessName, ""));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Behavior differs on Windows and Unix
        [ActiveIssue("https://github.com/dotnet/corefx/issues/18212", TargetFrameworkMonikers.UapAot)]
        public void TestProcessOnRemoteMachineWindows()
        {
            Process currentProccess = Process.GetCurrentProcess();

            void TestRemoteProccess(Process remoteProcess)
            {
                Assert.Equal(currentProccess.Id, remoteProcess.Id);
                Assert.Equal(currentProccess.BasePriority, remoteProcess.BasePriority);
                Assert.Equal(currentProccess.EnableRaisingEvents, remoteProcess.EnableRaisingEvents);
                Assert.Equal("127.0.0.1", remoteProcess.MachineName);
                // This property throws exception only on remote processes.
                Assert.Throws<NotSupportedException>(() => remoteProcess.MainModule);
            }

            try
            {
                TestRemoteProccess(Process.GetProcessById(currentProccess.Id, "127.0.0.1"));
                TestRemoteProccess(Process.GetProcessesByName(currentProccess.ProcessName, "127.0.0.1").Where(p => p.Id == currentProccess.Id).Single());
            }
            catch (InvalidOperationException)
            {
                // As we can't detect reliably if performance counters are enabled 
                // we let possible InvalidOperationExceptions pass silently.
            }
        }

        [Fact]
        public void StartInfo_GetFileName_ReturnsExpected()
        {
            Process process = CreateProcessLong();
            process.Start();

            // Processes are not hosted by dotnet in the full .NET Framework.
            string expectedFileName = PlatformDetection.IsFullFramework ? TestConsoleApp : HostRunner;
            Assert.Equal(expectedFileName, process.StartInfo.FileName);

            process.Kill();
            Assert.True(process.WaitForExit(WaitInMS));
        }
        
        [Fact]
        public void StartInfo_SetOnRunningProcess_ThrowsInvalidOperationException()
        {
            Process process = CreateProcessLong();
            process.Start();

            // .NET Core fixes a bug where Process.StartInfo for a unrelated process would
            // return information about the current process, not the unrelated process.
            // See https://github.com/dotnet/corefx/issues/1100.
            if (PlatformDetection.IsFullFramework)
            {
                var startInfo = new ProcessStartInfo();
                process.StartInfo = startInfo;
                Assert.Equal(startInfo, process.StartInfo);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => process.StartInfo = new ProcessStartInfo());
            }

            process.Kill();
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [Fact]
        public void StartInfo_SetGet_ReturnsExpected()
        {
            var process = new Process() { StartInfo = new ProcessStartInfo(TestConsoleApp) };
            Assert.Equal(TestConsoleApp, process.StartInfo.FileName);
        }

        [Fact]
        public void StartInfo_SetNull_ThrowsArgumentNullException()
        {
            var process = new Process();
            Assert.Throws<ArgumentNullException>("value", () => process.StartInfo = null);
        }

        [Fact]
        public void StartInfo_GetOnRunningProcess_ThrowsInvalidOperationException()
        {
            Process process = Process.GetCurrentProcess();

            // .NET Core fixes a bug where Process.StartInfo for an unrelated process would
            // return information about the current process, not the unrelated process.
            // See https://github.com/dotnet/corefx/issues/1100.
            if (PlatformDetection.IsFullFramework)
            {
                Assert.NotNull(process.StartInfo);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => process.StartInfo);
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

        [Fact]
        public void StandardInput_GetNotRedirected_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.StandardInput);
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

        [Fact]
        public void Start_NullStartInfo_ThrowsArgumentNullExceptionException()
        {
            AssertExtensions.Throws<ArgumentNullException>("startInfo", () => Process.Start((ProcessStartInfo)null));
        }

        [Fact]
        public void Start_EmptyFileName_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [Fact]
        public void Start_HasStandardOutputEncodingNonRedirected_ThrowsInvalidOperationException()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "FileName",
                    RedirectStandardOutput = false,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [Fact]
        public void Start_HasStandardErrorEncodingNonRedirected_ThrowsInvalidOperationException()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "FileName",
                    RedirectStandardError = false,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [Fact]
        public void Start_Disposed_ThrowsObjectDisposedException()
        {
            var process = new Process();
            process.StartInfo.FileName = "Nothing";
            process.Dispose();

            Assert.Throws<ObjectDisposedException>(() => process.Start());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Windows)]  // Expected process HandleCounts differs on OSX
        public void TestHandleCount()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True(p.HandleCount > 0);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // Expected process HandleCounts differs on OSX
        public void TestHandleCount_OSX()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.Equal(0, p.HandleCount);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Windows)]  // Expected process HandleCounts differs on OSX
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Handle count change is not reliable, but seems less robust on NETFX")]
        public void HandleCountChanges()
        {
            RemoteInvoke(() =>
            {
                Process p = Process.GetCurrentProcess();
                int handleCount = p.HandleCount;
                using (var fs1 = File.Open(GetTestFilePath(), FileMode.OpenOrCreate))
                using (var fs2 = File.Open(GetTestFilePath(), FileMode.OpenOrCreate))
                using (var fs3 = File.Open(GetTestFilePath(), FileMode.OpenOrCreate))
                {
                    p.Refresh();
                    int secondHandleCount = p.HandleCount;
                    Assert.True(handleCount < secondHandleCount);
                    handleCount = secondHandleCount;
                }
                p.Refresh();
                int thirdHandleCount = p.HandleCount;
                Assert.True(thirdHandleCount < handleCount);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void HandleCount_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.HandleCount);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // MainWindowHandle is not supported on Unix.
        public void MainWindowHandle_NoWindow_ReturnsEmptyHandle()
        {
            CreateDefaultProcess();

            Assert.Equal(IntPtr.Zero, _process.MainWindowHandle);
            Assert.Equal(_process.MainWindowHandle, _process.MainWindowHandle);
        }

        [Fact]
        public void MainWindowHandle_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MainWindowHandle);
        }

        [Fact]
        public void MainWindowTitle_NoWindow_ReturnsEmpty()
        {
            CreateDefaultProcess();

            Assert.Empty(_process.MainWindowTitle);
            Assert.Same(_process.MainWindowTitle, _process.MainWindowTitle);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // MainWindowTitle is a no-op and always returns string.Empty on Unix.
        public void MainWindowTitle_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MainWindowTitle);
        }

        [Fact]
        public void CloseMainWindow_NoWindow_ReturnsFalse()
        {
            CreateDefaultProcess();

            Assert.False(_process.CloseMainWindow());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // CloseMainWindow is a no-op and always returns false on Unix. 
        public void CloseMainWindow_NotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.CloseMainWindow());
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Needs to get the process Id from OS
        [Fact]
        public void TestRespondingWindows()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True(p.Responding);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Responding always returns true on Unix.
        public void Responding_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.Responding);
        }

        [Fact]
        public void TestNonpagedSystemMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroWindowsZeroUnix(_process.NonpagedSystemMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void NonpagedSystemMemorySize_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.NonpagedSystemMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void TestPagedMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroWindowsZeroUnix(_process.PagedMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void PagedMemorySize_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.PagedMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void TestPagedSystemMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroWindowsZeroUnix(_process.PagedSystemMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void PagedSystemMemorySize_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.PagedSystemMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void TestPeakPagedMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroWindowsZeroUnix(_process.PeakPagedMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void PeakPagedMemorySize_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.PeakPagedMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void TestPeakVirtualMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroWindowsZeroUnix(_process.PeakVirtualMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void PeakVirtualMemorySize_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.PeakVirtualMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void TestPeakWorkingSet()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroWindowsZeroUnix(_process.PeakWorkingSet);
#pragma warning restore 0618
        }

        [Fact]
        public void PeakWorkingSet_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.PeakWorkingSet);
#pragma warning restore 0618
        }

        [Fact]
        public void TestPrivateMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroWindowsZeroUnix(_process.PrivateMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void PrivateMemorySize_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.PrivateMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void TestVirtualMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            Assert.Equal(unchecked((int)_process.VirtualMemorySize64), _process.VirtualMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void VirtualMemorySize_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.VirtualMemorySize);
#pragma warning restore 0618
        }

        [Fact]
        public void TestWorkingSet()
        {
            CreateDefaultProcess();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // resident memory can be 0 on OSX.
#pragma warning disable 0618
                Assert.True(_process.WorkingSet >= 0);
#pragma warning restore 0618
                return;
            }

#pragma warning disable 0618
            Assert.True(_process.WorkingSet > 0);
#pragma warning restore 0618
        }

        [Fact]
        public void WorkingSet_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
#pragma warning disable 0618
            Assert.Throws<InvalidOperationException>(() => process.WorkingSet);
#pragma warning restore 0618
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Starting process with authentication not supported on Unix
        public void Process_StartInvalidNamesTest()
        {
            Assert.Throws<InvalidOperationException>(() => Process.Start(null, "userName", new SecureString(), "thisDomain"));
            Assert.Throws<InvalidOperationException>(() => Process.Start(string.Empty, "userName", new SecureString(), "thisDomain"));
            Assert.Throws<Win32Exception>(() => Process.Start("exe", string.Empty, new SecureString(), "thisDomain"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Starting process with authentication not supported on Unix
        public void Process_StartWithInvalidUserNamePassword()
        {
            SecureString password = AsSecureString("Value");
            Assert.Throws<Win32Exception>(() => Process.Start(GetCurrentProcessName(), "userName", password, "thisDomain"));
            Assert.Throws<Win32Exception>(() => Process.Start(GetCurrentProcessName(), Environment.UserName, password, Environment.UserDomainName));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Starting process with authentication not supported on Unix
        public void Process_StartTest()
        {
            string currentProcessName = GetCurrentProcessName();
            string userName = string.Empty;
            string domain = "thisDomain";
            SecureString password = AsSecureString("Value");

            Process p = Process.Start(currentProcessName, userName, password, domain);
            Assert.NotNull(p);
            Assert.Equal(currentProcessName, p.StartInfo.FileName);
            Assert.Equal(userName, p.StartInfo.UserName);
            Assert.Same(password, p.StartInfo.Password);
            Assert.Equal(domain, p.StartInfo.Domain);

            Assert.True(p.WaitForExit(WaitInMS));
            password.Dispose();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Starting process with authentication not supported on Unix
        public void Process_StartWithArgumentsTest()
        {
            string currentProcessName = GetCurrentProcessName();
            string userName = string.Empty;
            string domain = Environment.UserDomainName;
            string arguments = "-xml testResults.xml";
            SecureString password = AsSecureString("Value");

            Process p = Process.Start(currentProcessName, arguments, userName, password, domain);
            Assert.NotNull(p);
            Assert.Equal(currentProcessName, p.StartInfo.FileName);
            Assert.Equal(arguments, p.StartInfo.Arguments);
            Assert.Equal(userName, p.StartInfo.UserName);
            Assert.Same(password, p.StartInfo.Password);
            Assert.Equal(domain, p.StartInfo.Domain);
            p.Kill();
            password.Dispose();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Starting process with authentication not supported on Unix
        public void Process_StartWithDuplicatePassword()
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "exe",
                UserName = "dummyUser",
                PasswordInClearText = "Value",
                Password = AsSecureString("Value"),
                UseShellExecute = false
            };

            var process = new Process() { StartInfo = startInfo };
            Assert.Throws<ArgumentException>(null, () => process.Start());
        }

        private string GetCurrentProcessName()
        {
            return $"{Process.GetCurrentProcess().ProcessName}.exe";
        }

        private SecureString AsSecureString(string str)
        {
            SecureString secureString = new SecureString();

            foreach (var ch in str)
            {
                secureString.AppendChar(ch);
            }

            return secureString;
        }
    }
}
