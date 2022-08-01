// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using Xunit;
using Xunit.Sdk;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        const UnixFileMode ExecutablePermissions = UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite |
                                                   UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                                   UnixFileMode.GroupRead | UnixFileMode.GroupExecute;

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
            if (OperatingSystem.IsWindows())
            {
                Assert.NotEqual(0, value);
            }
            else
            {
                Assert.Equal(0, value);
            }
        }

        private void AssertNonZeroAllZeroDarwin(long value)
        {
            if (OperatingSystem.IsMacOS())
            {
                Assert.Equal(0, value);
            }
            else
            {
                Assert.NotEqual(0, value);
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(-2)]
        [InlineData((long)int.MaxValue + 1)]
        public void TestWaitForExitValidation(long milliseconds)
        {
            CreateDefaultProcess();
            Assert.Throws<ArgumentOutOfRangeException>("timeout", () => _process.WaitForExit(TimeSpan.FromMilliseconds(milliseconds)));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior varies on Windows and Unix
        public void TestBasePriorityOnWindows()
        {
            CreateDefaultProcess();

            ProcessPriorityClass originalPriority = _process.PriorityClass;
            Assert.Equal(Process.GetCurrentProcess().PriorityClass, originalPriority);

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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void TestExited_SynchronizingObject(bool invokeRequired)
        {
            var mres = new ManualResetEventSlim();
            Task beginInvokeTask = null;

            Process p = CreateProcessLong();
            p.SynchronizingObject = new DelegateSynchronizeInvoke()
            {
                InvokeRequiredDelegate = () => invokeRequired,
                BeginInvokeDelegate = (d, args) =>
                {
                    Assert.Null(beginInvokeTask);
                    beginInvokeTask = new Task(() => d.DynamicInvoke(args));
                    beginInvokeTask.Start(TaskScheduler.Default);
                    return beginInvokeTask;
                }
            };
            p.EnableRaisingEvents = true;
            p.Exited += delegate { mres.Set(); };
            StartSleepKillWait(p);

            Assert.True(mres.Wait(WaitInMS));

            if (invokeRequired)
            {
                Assert.NotNull(beginInvokeTask);
                Assert.True(beginInvokeTask.Wait(WaitInMS));
            }
            else
            {
                Assert.Null(beginInvokeTask);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void ProcessStart_TryExitCommandAsFileName_ThrowsWin32Exception()
        {
            Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = false, FileName = "exit", Arguments = "42" }));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void ProcessStart_UseShellExecuteFalse_FilenameIsUrl_ThrowsWin32Exception()
        {
            Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = false, FileName = "https://www.github.com/corefx" }));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void ProcessStart_TryOpenFolder_UseShellExecuteIsFalse_ThrowsWin32Exception()
        {
            Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = false, FileName = Path.GetTempPath() }));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void TestStartWithBadWorkingDirectory()
        {
            string program;
            string workingDirectory;
            if (PlatformDetection.IsWindows)
            {
                program = "powershell.exe";
                workingDirectory = @"C:\does-not-exist";
            }
            else
            {
                program = "uname";
                workingDirectory = "/does-not-exist";
            }

            if (IsProgramInstalled(program))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = program,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory
                };

                Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(psi));
                Assert.NotEqual(0, e.NativeErrorCode);
                Assert.Contains(program, e.Message);
                Assert.Contains(workingDirectory, e.Message);
            }
            else
            {
                Console.WriteLine($"Program {program} is not installed on this machine.");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasWindowsShell))]
        [OuterLoop("Launches File Explorer")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void ProcessStart_UseShellExecute_OnWindows_OpenMissingFile_Throws()
        {
            string fileToOpen = Path.Combine(Environment.CurrentDirectory, "_no_such_file.TXT");
            AssertExtensions.ThrowsContains<Win32Exception>(
                () => Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }),
                fileToOpen);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.HasWindowsShell))]
        [InlineData(true)]
        [InlineData(false)]
        [OuterLoop("Launches File Explorer")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void ProcessStart_UseShellExecute_OnWindows_DoesNotThrow(bool isFolder)
        {
            string fileToOpen;
            if (isFolder)
            {
                fileToOpen = Environment.CurrentDirectory;
            }
            else
            {
                fileToOpen = GetTestFilePath() + ".txt";
                File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_UseShellExecute_OnWindows_DoesNotThrow)}");
            }

            using (var px = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }))
            {
                if (isFolder)
                {
                    Assert.Null(px);
                }
                else
                {
                    if (px != null) // sometimes process is null
                    {
                        try
                        {
                            Assert.Equal("notepad", px.ProcessName.ToLower());
                        }
                        finally
                        {
                            px.Kill();
                            Assert.True(px.WaitForExit(WaitInMS));
                            px.WaitForExit(); // wait for event handlers to complete
                        }
                    }
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsServerCore),
            nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotWindowsIoTCore),
            nameof(PlatformDetection.IsNotAppSandbox))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34685", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [InlineData(true), InlineData(false)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void ProcessStart_UseShellExecute_Executes(bool filenameAsUrl)
        {
            string filename = WriteScriptFile(TestDirectory, GetTestFileName(), returnValue: 42);

            if (filenameAsUrl)
            {
                filename = new Uri(filename).ToString();
            }

            using (var process = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = filename }))
            {
                process.WaitForExit();
                Assert.Equal(42, process.ExitCode);
            }
        }

        private static bool UseShellExecuteExecuteOrderIsRunnablePlatform =>
            PlatformDetection.IsNotWindowsServerCore &&
            PlatformDetection.IsNotWindowsNanoServer &&
            PlatformDetection.IsNotWindowsIoTCore &&
            RemoteExecutor.IsSupported;

        private static bool IsNotAppSandbox => PlatformDetection.IsNotAppSandbox;

        [ConditionalFact(nameof(UseShellExecuteExecuteOrderIsRunnablePlatform), nameof(IsNotAppSandbox))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34685", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void ProcessStart_UseShellExecute_ExecuteOrder()
        {
            // Create a directory that we will use as PATH
            string path = Path.Combine(TestDirectory, "Path");
            Directory.CreateDirectory(path);
            // Create a directory that will be our working directory
            string wd = Path.Combine(TestDirectory, "WorkingDirectory");
            Directory.CreateDirectory(wd);

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["PATH"] = path;
            options.StartInfo.WorkingDirectory = wd;
            RemoteExecutor.Invoke(pathDirectory =>
            {
                // Create two identically named scripts, one in the working directory and one on PATH.
                const int workingDirReturnValue = 1;
                const int pathDirReturnValue = 2;
                string pathScriptFile = WriteScriptFile(pathDirectory,                 "script", returnValue: pathDirReturnValue);
                string wdScriptFile = WriteScriptFile(Directory.GetCurrentDirectory(), "script", returnValue: workingDirReturnValue);
                string scriptFilename = Path.GetFileName(pathScriptFile);
                Assert.Equal(scriptFilename, Path.GetFileName(wdScriptFile));

                // Execute the script and verify we prefer the one in the working directory.
                using (var process = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = scriptFilename }))
                {
                    process.WaitForExit();
                    Assert.Equal(workingDirReturnValue, process.ExitCode);
                }

                // Remove the script in the working directory and verify we now use the one on PATH.
                File.Delete(scriptFilename);
                using (var process = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = scriptFilename }))
                {
                    process.WaitForExit();
                    Assert.Equal(pathDirReturnValue, process.ExitCode);
                }

                return RemoteExecutor.SuccessExitCode;
            }, path, options).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsServerCore),
            nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotWindowsIoTCore),
            nameof(PlatformDetection.IsNotAppSandbox))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34685", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void ProcessStart_UseShellExecute_WorkingDirectory()
        {
            // Create a directory that will ProcessStartInfo.WorkingDirectory
            // and add a script.
            string wd = Path.Combine(TestDirectory, "WorkingDirectory");
            Directory.CreateDirectory(wd);
            string filename = Path.GetFileName(WriteScriptFile(wd, GetTestFileName(), returnValue: 42));

            // Verify UseShellExecute finds the script in the WorkingDirectory.
            Assert.False(Path.IsPathRooted(filename));
            using (var process = Process.Start(new ProcessStartInfo { UseShellExecute = true,
                                                                      FileName = filename,
                                                                      WorkingDirectory = wd }))
            {
                process.WaitForExit();
                Assert.Equal(42, process.ExitCode);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestExitCode()
        {
            {
                Process p = CreateProcessPortable(RemotelyInvokable.Dummy);
                p.Start();
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(RemoteExecutor.SuccessExitCode, p.ExitCode);
            }

            {
                Process p = CreateProcessLong();
                StartSleepKillWait(p);
                Assert.NotEqual(0, p.ExitCode);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestExitTime()
        {
            // Try twice, since it's possible that the system clock could be adjusted backwards between when we snapshot it
            // and when the process ends, but vanishingly unlikely that would happen twice.
            DateTime timeBeforeProcessStart = DateTime.MaxValue;
            Process p = null;

            for (int i = 0; i <= 1; i++)
            {
                // ExitTime resolution on some platforms is less accurate than our DateTime.UtcNow resolution, so
                // we subtract ms from the begin time to account for it.
                timeBeforeProcessStart = DateTime.UtcNow.AddMilliseconds(-25);
                p = CreateProcessLong();
                p.Start();
                Assert.Throws<InvalidOperationException>(() => p.ExitTime);
                p.Kill();
                Assert.True(p.WaitForExit(WaitInMS));

                if (p.ExitTime.ToUniversalTime() >= timeBeforeProcessStart)
                    break;
            }

            Assert.InRange(p.ExitTime.ToUniversalTime(), timeBeforeProcessStart, DateTime.MaxValue);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void StartTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.StartTime);
        }

        [Fact]
        public void GetCurrentProcess_Id_EqualsCurrentProcessId()
        {
            using Process current = Process.GetCurrentProcess();
            Assert.Equal(Environment.ProcessId, current.Id);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestId()
        {
            CreateDefaultProcess();

            Assert.NotEqual(Environment.ProcessId, _process.Id);

            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(_process.Id, Interop.GetProcessId(_process.SafeHandle));
            }
            else
            {
                IEnumerable<int> testProcessIds = Process.GetProcessesByName(RemoteExecutor.HostRunnerName).Select(p => p.Id);
                Assert.Contains(_process.Id, testProcessIds);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestHasExited()
        {
            {
                Process p = CreateProcessPortable(RemotelyInvokable.Dummy);
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestToString_OnRunningProcess()
        {
            Process p = CreateDefaultProcess();
            var name = p.ProcessName;
            Assert.Equal($"System.Diagnostics.Process ({name})", p.ToString());

            KillWait(p);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestToString_OnExitedProcess()
        {
            Process p = CreateDefaultProcess();
            KillWait(p);

            // Ensure ToString does not throw an exception, but still returns
            // a representation of the object.
            Assert.Contains("System.Diagnostics.Process", p.ToString());
        }

        [Fact]
        public void HasExited_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.HasExited);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
        public void Kill_NotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.Kill());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void TestMainModule()
        {
            Process p = Process.GetCurrentProcess();

            Assert.InRange(p.Modules.Count, 1, int.MaxValue);
            Assert.Equal(RemoteExecutor.HostRunnerName, p.MainModule.ModuleName);
            Assert.EndsWith(RemoteExecutor.HostRunnerName, p.MainModule.FileName);
            Assert.Equal(string.Format("System.Diagnostics.ProcessModule ({0})", RemoteExecutor.HostRunnerName), p.MainModule.ToString());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestMaxWorkingSet()
        {
            CreateDefaultProcess();

            using (Process p = Process.GetCurrentProcess())
            {
                Assert.InRange((long)p.MaxWorkingSet, 1, long.MaxValue);
                Assert.InRange((long)p.MinWorkingSet, 0, long.MaxValue);
            }

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD()) {
                return; // doesn't support getting/setting working set for other processes
            }

            long curValue = (long)_process.MaxWorkingSet;
            Assert.InRange(curValue, 0, long.MaxValue);

            if (OperatingSystem.IsWindows())
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
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.FreeBSD | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Getting MaxWorkingSet is not supported on OSX, BSD, iOS, MacCatalyst, and tvOS.")]
        public void MaxWorkingSet_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MaxWorkingSet);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.FreeBSD)]
        public void MaxValueWorkingSet_GetSetMacos_ThrowsPlatformSupportedException()
        {
            var process = new Process();
            Assert.Throws<PlatformNotSupportedException>(() => process.MaxWorkingSet);
            Assert.Throws<PlatformNotSupportedException>(() => process.MaxWorkingSet = (IntPtr)1);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestMinWorkingSet()
        {
            CreateDefaultProcess();

            using (Process p = Process.GetCurrentProcess())
            {
                Assert.InRange((long)p.MaxWorkingSet, 1, long.MaxValue);
                Assert.InRange((long)p.MinWorkingSet, 0, long.MaxValue);
            }

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD()) {
                return; // doesn't support getting/setting working set for other processes
            }

            long curValue = (long)_process.MinWorkingSet;
            Assert.InRange(curValue, 0, long.MaxValue);

            if (OperatingSystem.IsWindows())
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
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.FreeBSD | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Getting MinWorkingSet is not supported on OSX, BSD, iOS, MacCatalyst, and tvOS.")]
        public void MinWorkingSet_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MinWorkingSet);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.FreeBSD)]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestPeakVirtualMemorySize64()
        {
            CreateDefaultProcess();

            AssertNonZeroAllZeroDarwin(_process.PeakVirtualMemorySize64);
        }

        [Fact]
        public void PeakVirtualMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PeakVirtualMemorySize64);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestPeakWorkingSet64()
        {
            CreateDefaultProcess();

            AssertNonZeroAllZeroDarwin(_process.PeakWorkingSet64);
        }

        [Fact]
        public void PeakWorkingSet64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PeakWorkingSet64);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestPrivateMemorySize64()
        {
            CreateDefaultProcess();

            AssertNonZeroAllZeroDarwin(_process.PrivateMemorySize64);
        }

        [Fact]
        public void PrivateMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PrivateMemorySize64);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49107", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public void TestVirtualMemorySize64()
        {
            CreateDefaultProcess();

            Assert.InRange(_process.VirtualMemorySize64, 1, long.MaxValue);
        }

        [Fact]
        public void VirtualMemorySize64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.VirtualMemorySize64);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestWorkingSet64()
        {
            CreateDefaultProcess();

            if (OperatingSystem.IsMacOS())
            {
                // resident memory can be 0 on OSX.
                Assert.InRange(_process.WorkingSet64, 0, long.MaxValue);
                return;
            }

            Assert.InRange(_process.WorkingSet64, 1, long.MaxValue);
        }

        [Fact]
        public void WorkingSet64_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.WorkingSet64);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestProcessorTime()
        {
            CreateDefaultProcess();

            Assert.InRange(_process.UserProcessorTime.TotalSeconds, 0, long.MaxValue);
            Assert.InRange(_process.PrivilegedProcessorTime.TotalSeconds, 0, long.MaxValue);

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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TotalProcessorTime_PerformLoop_TotalProcessorTimeValid()
        {
            CreateDefaultProcess();

            DateTime startTime = DateTime.UtcNow;
            TimeSpan processorTimeBeforeSpin = Process.GetCurrentProcess().TotalProcessorTime;

            // Perform loop to occupy cpu, takes less than a second.
            int i = int.MaxValue / 16;
            while (i > 0)
            {
                i--;
            }

            TimeSpan processorTimeAfterSpin = Process.GetCurrentProcess().TotalProcessorTime;
            DateTime endTime = DateTime.UtcNow;

            double timeDiff = (endTime - startTime).TotalMilliseconds;
            double cpuTimeDiff = (processorTimeAfterSpin - processorTimeBeforeSpin).TotalMilliseconds;

            double cpuUsage = cpuTimeDiff / (timeDiff * Environment.ProcessorCount);

            Assert.InRange(cpuUsage, 0, 1);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void UserProcessorTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.UserProcessorTime);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void PriviledgedProcessorTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.PrivilegedProcessorTime);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void TotalProcessorTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.TotalProcessorTime);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestProcessStartTime()
        {
            TimeSpan allowedWindow = TimeSpan.FromSeconds(3);

            for (int i = 0; i < 2; i++)
            {
                Process p = CreateProcessPortable(RemotelyInvokable.ReadLine);

                Assert.Throws<InvalidOperationException>(() => p.StartTime);

                DateTime testStartTime = DateTime.Now;
                p.StartInfo.RedirectStandardInput = true;
                p.Start();
                Assert.Equal(p.StartTime, p.StartTime);
                DateTime processStartTime = p.StartTime;
                using (StreamWriter writer = p.StandardInput)
                {
                    writer.WriteLine("start");
                }

                Assert.True(p.WaitForExit(WaitInMS));
                DateTime testEndTime = DateTime.Now;

                bool hasTimeChanged = testEndTime < testStartTime;
                if (i != 0 || !hasTimeChanged)
                {
                    Assert.InRange(processStartTime, testStartTime - allowedWindow, testEndTime + allowedWindow);
                    break;
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ProcessStartTime_Deterministic_Across_Instances()
        {
            CreateDefaultProcess();
            for (int i = 0; i < 10; ++i)
            {
                using (var p = Process.GetProcessById(_process.Id))
                {
                    Assert.Equal(_process.StartTime, p.StartTime);
                }
            }
        }

        [Fact]
        public void ExitTime_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.ExitTime);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.FreeBSD, "getting/setting affinity not supported on OSX and BSD")]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported)), PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior varies on Windows and Unix
        public void TestPriorityClassWindows()
        {
            CreateDefaultProcess();

            ProcessPriorityClass priorityClass = _process.PriorityClass;
            try
            {
                _process.PriorityClass = ProcessPriorityClass.High;
                Assert.Equal(ProcessPriorityClass.High, _process.PriorityClass);

                _process.PriorityClass = ProcessPriorityClass.Normal;
                Assert.Equal(ProcessPriorityClass.Normal, _process.PriorityClass);
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestProcessName()
        {
            CreateDefaultProcess();

            // Process.ProcessName drops the extension when it's exe.
            string processName = RemoteExecutor.HostRunner.EndsWith(".exe") ?_process.ProcessName : Path.GetFileNameWithoutExtension(_process.ProcessName);
            Assert.Equal(Path.GetFileNameWithoutExtension(RemoteExecutor.HostRunner), processName, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ProcessName_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.ProcessName);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestSafeHandle()
        {
            CreateDefaultProcess();

            Assert.False(_process.SafeHandle.IsInvalid);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void Handle_CreateEvent_BlocksUntilProcessCompleted(bool useSafeHandle)
        {
            using (RemoteInvokeHandle h = RemoteExecutor.Invoke(() => Console.ReadLine(), new RemoteInvokeOptions { StartInfo = new ProcessStartInfo() { RedirectStandardInput = true } }))
            using (var mre = new ManualResetEvent(false))
            {
                mre.SetSafeWaitHandle(new SafeWaitHandle(useSafeHandle ? h.Process.SafeHandle.DangerousGetHandle() : h.Process.Handle, ownsHandle: false));

                Assert.False(mre.WaitOne(millisecondsTimeout: 0), "Event should not yet have been set.");

                h.Process.StandardInput.WriteLine(); // allow child to complete

                Assert.True(mre.WaitOne(RemoteExecutor.FailWaitTimeoutMilliseconds), "Event should have been set.");
            }
        }

        [Fact]
        public void SafeHandle_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.SafeHandle);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

            int currentProcessId = Environment.ProcessId;

            Assert.Equal(currentProcessId, current.Id);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestGetProcessById()
        {
            CreateDefaultProcess();

            Process p = Process.GetProcessById(_process.Id);
            Assert.Equal(_process.Id, p.Id);
            Assert.Equal(_process.ProcessName, p.ProcessName);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetProcessById_KilledProcess_ThrowsArgumentException()
        {
            Process process = CreateDefaultProcess();
            var handle = process.SafeHandle;
            int processId = process.Id;
            process.Kill();
            process.WaitForExit(WaitInMS);
            Assert.Throws<ArgumentException>(() => Process.GetProcessById(processId));
            GC.KeepAlive(handle);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
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
            AssertExtensions.Throws<ArgumentException>("machineName", () => Process.GetProcesses(""));
        }

        [Fact]
        public void GetProcesses_InvalidMachineName_ThrowsInvalidOperationException()
        {
            Type exceptionType = PlatformDetection.IsWindows ? typeof(InvalidOperationException) : typeof(PlatformNotSupportedException);
            Assert.Throws(exceptionType, () => Process.GetProcesses(Guid.NewGuid().ToString()));
        }

        [Fact]
        public void GetProcesses_RemoteMachinePath_ReturnsExpected()
        {
            string computerDomain = null;
            try
            {
                computerDomain = Domain.GetComputerDomain().Name;
            }
            catch
            {
                // Ignore all exceptions - this test is not testing GetComputerDomain.
                // This path is taken when the executing machine is not domain-joined or DirectoryServices are unavailable.
                return;
            }

            try
            {
                Process[] processes = Process.GetProcesses(Environment.MachineName + "." + computerDomain);
                Assert.NotEmpty(processes);
            }
            catch (InvalidOperationException)
            {
                // As we can't detect reliably if performance counters are enabled
                // we let possible InvalidOperationExceptions pass silently.
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void GetProcessesByName_NullEmpty_ReturnsAllProcesses(string name)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(name);

            int expectedCount = (PlatformDetection.IsMobile) ? 1 : 2;

            Assert.Contains(processes, process => process.ProcessName == currentProcess.ProcessName);
            Assert.InRange(processes.Length, expectedCount, int.MaxValue); // should contain current process and some number of additional processes
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void GetProcessesByName_ProcessName_ReturnsExpected()
        {
            // Get the current process using its name
            Process currentProcess = Process.GetCurrentProcess();
            Assert.NotNull(currentProcess.ProcessName);
            Assert.NotEmpty(currentProcess.ProcessName);

            foreach (string processName in new[] { currentProcess.ProcessName, currentProcess.ProcessName.ToLowerInvariant(), currentProcess.ProcessName.ToUpperInvariant() })
            {
                Process[] processes = Process.GetProcessesByName(processName);
                try
                {
                    Assert.NotEmpty(processes);
                }
                catch (NotEmptyException)
                {
                    throw new TrueException(PrintProcesses(currentProcess), false);
                }

                Assert.All(processes, process => Assert.Equal(currentProcess.ProcessName, process.ProcessName));
                Assert.All(processes, process => Assert.Equal(".", process.MachineName));
            }

            // Outputs a list of active processes in case of failure: https://github.com/dotnet/runtime/issues/28874
            string PrintProcesses(Process currentProcess)
            {
                StringBuilder builder = new StringBuilder();
                foreach (Process process in Process.GetProcesses())
                {
                    builder.Append($"Pid: '{process.Id}' Name: '{process.ProcessName}'");
                    try
                    {
                        builder.Append($" Main module: '{process.MainModule.FileName}'");
                    }
                    catch
                    {
                        // We cannot obtain main module of all processes
                    }
                    builder.AppendLine();
                }

                builder.Append($"Current process id: {currentProcess.Id} Process name: '{currentProcess.ProcessName}'");
                return builder.ToString();
            }
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void GetProcessesByName_ProcessNameMachineName_ReturnsExpected(string machineName)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName, machineName);
            Assert.NotEmpty(processes);

            Assert.All(processes, process => Assert.Equal(machineName, process.MachineName));
        }

        [Theory]
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void GetProcessesByName_NoSuchProcess_ReturnsEmpty()
        {
            string processName = Guid.NewGuid().ToString("N");
            Assert.Empty(Process.GetProcessesByName(processName));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void GetProcessesByName_NullMachineName_ThrowsArgumentNullException()
        {
            Process currentProcess = Process.GetCurrentProcess();
            AssertExtensions.Throws<ArgumentNullException>("machineName", () => Process.GetProcessesByName(currentProcess.ProcessName, null));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void GetProcessesByName_EmptyMachineName_ThrowsArgumentException()
        {
            Process currentProcess = Process.GetCurrentProcess();
            AssertExtensions.Throws<ArgumentException>("machineName", () => Process.GetProcessesByName(currentProcess.ProcessName, ""));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Behavior differs on Windows and Unix
        public void TestProcessOnRemoteMachineWindows()
        {
            Process currentProcess = Process.GetCurrentProcess();

            void TestRemoteProcess(Process remoteProcess)
            {
                Assert.Equal(currentProcess.Id, remoteProcess.Id);
                Assert.Equal(currentProcess.BasePriority, remoteProcess.BasePriority);
                Assert.Equal(currentProcess.EnableRaisingEvents, remoteProcess.EnableRaisingEvents);
                Assert.Equal("127.0.0.1", remoteProcess.MachineName);
                // This property throws exception only on remote processes.
                Assert.Throws<NotSupportedException>(() => remoteProcess.MainModule);
            }

            try
            {
                TestRemoteProcess(Process.GetProcessById(currentProcess.Id, "127.0.0.1"));
                TestRemoteProcess(Process.GetProcessesByName(currentProcess.ProcessName, "127.0.0.1").Where(p => p.Id == currentProcess.Id).Single());
            }
            catch (InvalidOperationException)
            {
                // As we can't detect reliably if performance counters are enabled
                // we let possible InvalidOperationExceptions pass silently.
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartInfo_GetFileName_ReturnsExpected()
        {
            Process process = CreateProcessLong();
            process.Start();

            Assert.Equal(RemoteExecutor.HostRunner, process.StartInfo.FileName);

            process.Kill();
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartInfo_SetOnRunningProcess_ThrowsInvalidOperationException()
        {
            Process process = CreateProcessLong();
            process.Start();

            // .NET Core fixes a bug where Process.StartInfo for a unrelated process would
            // return information about the current process, not the unrelated process.
            // See https://github.com/dotnet/runtime/issues/14329.
            Assert.Throws<InvalidOperationException>(() => process.StartInfo = new ProcessStartInfo());

            process.Kill();
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void StartInfo_SetGet_ReturnsExpected()
        {
            var process = new Process() { StartInfo = new ProcessStartInfo(RemoteExecutor.HostRunner) };
            Assert.Equal(RemoteExecutor.HostRunner, process.StartInfo.FileName);
        }

        [Fact]
        public void StartInfo_SetNull_ThrowsArgumentNullException()
        {
            var process = new Process();
            Assert.Throws<ArgumentNullException>(() => process.StartInfo = null);
        }

        [Fact]
        public void StartInfo_GetOnRunningProcess_ThrowsInvalidOperationException()
        {
            Process process = Process.GetCurrentProcess();

            // .NET Core fixes a bug where Process.StartInfo for an unrelated process would
            // return information about the current process, not the unrelated process.
            // See https://github.com/dotnet/runtime/issues/14329.
            Assert.Throws<InvalidOperationException>(() => process.StartInfo);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
        [InlineData("\"\" b \"\"", ",b,")]
        [InlineData("\"\"\"\" b c", "\",b,c")]
        [InlineData("c\"\"\"\" b \"\"\\", "c\",b,\\")]
        [InlineData("\"\"c \"\"b\"\" d\"\\", "c,b,d\\")]
        [InlineData("\"\"a\"\" b d", "a,b,d")]
        [InlineData("b d \"\"a\"\" ", "b,d,a")]
        [InlineData("\\\"\\\"a\\\"\\\" b d", "\"\"a\"\",b,d")]
        [InlineData("b d \\\"\\\"a\\\"\\\"", "b,d,\"\"a\"\"")]
        public void TestArgumentParsing(string inputArguments, string expectedArgv)
        {
            var options = new RemoteInvokeOptions
            {
                Start = true,
                StartInfo = new ProcessStartInfo { RedirectStandardOutput = true }
            };

            using (RemoteInvokeHandle handle = RemoteExecutor.InvokeRaw((Func<string, string, string, int>)RemotelyInvokable.ConcatThreeArguments, inputArguments, options))
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on iOS and tvOS.")]
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
            Assert.Contains(path, e.Message);
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Start_RedirectStandardOutput_StartAgain_DoesntThrow()
        {
            using (Process process = CreateProcess(() =>
            {
                Console.WriteLine("hello world");
                return RemoteExecutor.SuccessExitCode;
            }))
            {
                process.StartInfo.RedirectStandardOutput = true;

                Assert.True(process.Start());
                process.BeginOutputReadLine();

                Assert.True(process.Start());

                Assert.True(process.WaitForExit(WaitInMS));
                process.WaitForExit(); // ensure event handlers have completed
            }
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
        public void StandardInput_Disposed_ThrowsObjectDisposedException()
        {
            var process = new Process();
            process.StartInfo.FileName = "Nothing";
            process.Dispose();

            Assert.Throws<ObjectDisposedException>(() => process.StandardInput);
        }

        [Fact]
        public void StandardError_Disposed_ThrowsObjectDisposedException()
        {
            var process = new Process();
            process.StartInfo.FileName = "Nothing";
            process.Dispose();

            Assert.Throws<ObjectDisposedException>(() => process.StandardError);
        }

        [Fact]
        public void StandardOutput_Disposed_ThrowsObjectDisposedException()
        {
            var process = new Process();
            process.StartInfo.FileName = "Nothing";
            process.Dispose();

            Assert.Throws<ObjectDisposedException>(() => process.StandardOutput);
        }

        [Fact]
        public void CancelOutputRead_Disposed_ThrowsObjectDisposedException()
        {
            var process = new Process();
            process.StartInfo.FileName = "Nothing";
            process.Dispose();

            Assert.Throws<ObjectDisposedException>(() => process.CancelOutputRead());
        }

        [Fact]
        public void CancelErrorRead_Disposed_ThrowsObjectDisposedException()
        {
            var process = new Process();
            process.StartInfo.FileName = "Nothing";
            process.Dispose();

            Assert.Throws<ObjectDisposedException>(() => process.CancelErrorRead());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Windows)]  // Expected process HandleCounts differs on OSX
        public void TestHandleCount()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.InRange(p.HandleCount, 1, int.MaxValue);
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

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Windows)]  // Expected process HandleCounts differs on OSX
        public void HandleCountChanges()
        {
            RemoteExecutor.Invoke(() =>
            {
                RetryHelper.Execute(() =>
                {
                    using (Process p = Process.GetCurrentProcess())
                    {
                        // Warm up code paths
                        p.Refresh();
                        using (var tmpFile = File.Open(GetTestFilePath(), FileMode.OpenOrCreate))
                        {
                            // Get the initial handle count
                            p.Refresh();
                            int handleCountAtStart = p.HandleCount;
                            int handleCountAfterOpens;

                            // Open a bunch of files and get a new handle count, then close the files
                            var files = new List<FileStream>();
                            try
                            {
                                files.AddRange(Enumerable.Range(0, 50).Select(_ => File.Open(GetTestFilePath(), FileMode.OpenOrCreate)));
                                p.Refresh();
                                handleCountAfterOpens = p.HandleCount;
                            }
                            finally
                            {
                                files.ForEach(f => f.Dispose());
                            }

                            // Get the handle count after closing all the files
                            p.Refresh();
                            int handleCountAtEnd = p.HandleCount;

                            Assert.InRange(handleCountAfterOpens, handleCountAtStart + 1, int.MaxValue);
                            Assert.InRange(handleCountAtEnd, handleCountAtStart, handleCountAfterOpens - 1);
                        }
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void HandleCount_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.HandleCount);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)] // MainWindowHandle is not supported on Unix.
        public void MainWindowHandle_NoWindow_ReturnsEmptyHandle()
        {
            CreateDefaultProcess();

            Assert.Equal(IntPtr.Zero, _process.MainWindowHandle);
            Assert.Equal(_process.MainWindowHandle, _process.MainWindowHandle);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void MainWindowHandle_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.MainWindowHandle);
        }

        [ConditionalFact(typeof(PlatformDetection),
            nameof(PlatformDetection.IsNotWindowsNanoServer), // it needs Notepad
            nameof(PlatformDetection.IsNotWindowsServerCore))] // explained in https://github.com/dotnet/runtime/pull/44972
        [OuterLoop("Pops UI")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void MainWindowHandle_GetWithGui_ShouldRefresh_Windows()
        {
            const string ExePath = "notepad.exe";
            Assert.True(IsProgramInstalled(ExePath), "Notepad is not installed");

            using (Process process = Process.Start(ExePath))
            {
                try
                {
                    for (int attempt = 0; attempt < 50; ++attempt)
                    {
                        process.Refresh();
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.NotEqual(IntPtr.Zero, process.MainWindowHandle);
                }
                finally
                {
                    process.Kill();
                    Assert.True(process.WaitForExit(WaitInMS));
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection),
            nameof(PlatformDetection.IsNotWindowsNanoServer), // it needs Notepad
            nameof(PlatformDetection.IsNotWindowsServerCore))] // explained in https://github.com/dotnet/runtime/pull/44972
        [OuterLoop("Pops UI")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void MainWindowTitle_GetWithGui_ShouldRefresh_Windows()
        {
            const string ExePath = "notepad.exe";
            Assert.True(IsProgramInstalled(ExePath), "Notepad is not installed");

            using (Process process = Process.Start(new ProcessStartInfo(ExePath)))
            {
                try
                {
                    for (int attempt = 0; attempt < 50; ++attempt)
                    {
                        process.Refresh();
                        if (process.MainWindowTitle != string.Empty)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.NotEqual(string.Empty, process.MainWindowTitle);
                }
                finally
                {
                    process.Kill();
                    Assert.True(process.WaitForExit(WaitInMS));
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void RefreshResetsAllRefreshableFields()
        {
            // testing Process.Responding using a real unresponsive process would be very hard to do properly
            // instead of this, we just test the implementation to ensure that #36768 is not coming back
            var process = new Process();

            VerifyPrivateFieldsValues(process, shouldHaveDefaultValues: true);

            SetPrivateFieldsToNonDefaultValues(process);

            VerifyPrivateFieldsValues(process, shouldHaveDefaultValues: false);

            process.Refresh();

            VerifyPrivateFieldsValues(process, shouldHaveDefaultValues: true);

            static void VerifyPrivateFieldsValues(Process process, bool shouldHaveDefaultValues)
            {
                Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_exited"));
                Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_haveWorkingSetLimits"));
                Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_haveProcessorAffinity"));
                Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_havePriorityClass"));
                Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_haveExitTime"));
                Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_havePriorityBoostEnabled"));

                Assert.Equal(shouldHaveDefaultValues, null == GetPrivateFieldValue(process, "_processInfo"));
                Assert.Equal(shouldHaveDefaultValues, null == GetPrivateFieldValue(process, "_threads"));
                Assert.Equal(shouldHaveDefaultValues, null == GetPrivateFieldValue(process, "_modules"));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(shouldHaveDefaultValues, null == GetPrivateFieldValue(process, "_mainWindowTitle"));
                    Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_signaled"));
                    Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_haveMainWindow"));
                    Assert.Equal(shouldHaveDefaultValues, !(bool)GetPrivateFieldValue(process, "_haveResponding"));
                }
            }

            static void SetPrivateFieldsToNonDefaultValues(Process process)
            {
                SetPrivateFieldValue(process, "_exited", true);
                SetPrivateFieldValue(process, "_haveWorkingSetLimits", true);
                SetPrivateFieldValue(process, "_haveProcessorAffinity", true);
                SetPrivateFieldValue(process, "_havePriorityClass", true);
                SetPrivateFieldValue(process, "_haveExitTime", true);
                SetPrivateFieldValue(process, "_havePriorityBoostEnabled", true);

                SetPrivateFieldValue(process, "_processInfo", typeof(Process).Assembly.GetType("System.Diagnostics.ProcessInfo").GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, Array.Empty<Type>()).Invoke(null));
                SetPrivateFieldValue(process, "_threads", new ProcessThreadCollection(Array.Empty<ProcessThread>()));
                SetPrivateFieldValue(process, "_modules",  new ProcessModuleCollection(Array.Empty<ProcessModule>()));

                if (OperatingSystem.IsWindows())
                {
                    SetPrivateFieldValue(process, "_signaled", true);
                    SetPrivateFieldValue(process, "_haveMainWindow", true);
                    SetPrivateFieldValue(process, "_mainWindowTitle", "notNull");
                    SetPrivateFieldValue(process, "_haveResponding", true);
                }
            }

            static object GetPrivateFieldValue(Process process, string fieldName) => typeof(Process)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(process);

            static void SetPrivateFieldValue(Process process, string fieldName, object value) => typeof(Process)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(process, value);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CloseMainWindow_NoWindow_ReturnsFalse()
        {
            CreateDefaultProcess();

            Assert.False(_process.CloseMainWindow());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CloseMainWindow_NotStarted_ThrowsInvalidOperationException_Windows()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.CloseMainWindow());
        }

        [Fact]
        // CloseMainWindow is a no-op and always returns false on Unix or UWP.
        public void CloseMainWindow_NotStarted_ReturnsFalse_UWPNonWindows()
        {
            if (PlatformDetection.IsWindows && !PlatformDetection.IsInAppContainer)
                return;

            var process = new Process();
            Assert.False(process.CloseMainWindow());
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestPeakVirtualMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroAllZeroDarwin(_process.PeakVirtualMemorySize);
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestPeakWorkingSet()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroAllZeroDarwin(_process.PeakWorkingSet);
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestPrivateMemorySize()
        {
            CreateDefaultProcess();

#pragma warning disable 0618
            AssertNonZeroAllZeroDarwin(_process.PrivateMemorySize);
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestWorkingSet()
        {
            CreateDefaultProcess();

            if (OperatingSystem.IsMacOS())
            {
                // resident memory can be 0 on OSX.
#pragma warning disable 0618
                Assert.InRange(_process.WorkingSet, 0, int.MaxValue);
#pragma warning restore 0618
                return;
            }

#pragma warning disable 0618
            Assert.InRange(_process.WorkingSet, 1, int.MaxValue);
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

        [OuterLoop("May take many seconds the first time it's run")]
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
            string name = "xcopy.exe";
            string userName = string.Empty;
            string domain = "thisDomain";
            SecureString password = AsSecureString("Value");

            using (Process p = Process.Start(name, userName, password, domain)) // This writes junk to the Console but with this overload, we can't prevent that.
            {
                Assert.NotNull(p);
                Assert.Equal(name, p.StartInfo.FileName);
                Assert.Equal(userName, p.StartInfo.UserName);
                Assert.Same(password, p.StartInfo.Password);
                Assert.Equal(domain, p.StartInfo.Domain);
                Assert.True(p.WaitForExit(WaitInMS));
            }
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
            using (Process p = Process.Start(currentProcessName, arguments, userName, password, domain))
            {
                Assert.NotNull(p);
                Assert.Equal(currentProcessName, p.StartInfo.FileName);
                Assert.Equal(arguments, p.StartInfo.Arguments);
                Assert.Equal(userName, p.StartInfo.UserName);
                Assert.Same(password, p.StartInfo.Password);
                Assert.Equal(domain, p.StartInfo.Domain);
                p.Kill();
            }
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
            AssertExtensions.Throws<ArgumentException>(null, () => process.Start());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestLongProcessIsWorking()
        {
            // Sanity check for CreateProcessLong
            Process p = CreateProcessLong();
            p.Start();
            Thread.Sleep(500);
            Assert.False(p.HasExited);
            p.Kill();
            p.WaitForExit();
            Assert.True(p.HasExited);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/29330", TestPlatforms.OSX)]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52852", TestPlatforms.MacCatalyst)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/53095", TestPlatforms.Android)]
        public void LongProcessNamesAreSupported()
        {
            string sleepPath;
            if (OperatingSystem.IsLinux())
            {
                // On some distros sleep is implemented using a script/symlink, which causes this test to fail.
                // Instead of using sleep directly, we wrap it with a script.
                sleepPath = GetTestFilePath();
                File.WriteAllText(sleepPath, $"#!/bin/sh\nsleep 600\n"); // sleep 10 min.
                File.SetUnixFileMode(sleepPath, ExecutablePermissions);
            }
            else
            {
                sleepPath = GetProgramPath("sleep");
                if (sleepPath == null)
                {
                    return;
                }
            }

            const string LongProcessName = "123456789012345678901234567890";
            string sleepCommandPathFileName = Path.Combine(TestDirectory, LongProcessName);
            File.Copy(sleepPath, sleepCommandPathFileName);

            using (Process px = Process.Start(sleepCommandPathFileName, "600"))
            {
                Process[] runningProcesses = Process.GetProcesses();
                try
                {
                    Assert.Contains(runningProcesses, p => p.ProcessName == LongProcessName);
                }
                finally
                {
                    px.Kill();
                    px.WaitForExit();
                }
            }
        }

        [Fact]
        public void Start_HasStandardInputEncodingNonRedirected_ThrowsInvalidOperationException()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "FileName",
                    RedirectStandardInput = false,
                    StandardInputEncoding = Encoding.UTF8
                }
            };

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Start_StandardInputEncodingPropagatesToStreamWriter()
        {
            var process = CreateProcessPortable(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardInput = true;
            var encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: true);
            process.StartInfo.StandardInputEncoding = encoding;
            process.Start();

            Assert.Same(encoding, process.StandardInput.Encoding);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void StartProcessWithArgumentList()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName());
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            testProcess.StartInfo = psi;

            try
            {
                testProcess.Start();
                Assert.Equal(string.Empty, testProcess.StartInfo.Arguments);
            }
            finally
            {
                testProcess.Kill();

                Assert.True(testProcess.WaitForExit(WaitInMS));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ArgumentListArgumentNullThrowsOnStart()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName());
            psi.ArgumentList.Add(null);

            Process testProcess = CreateProcess();
            testProcess.StartInfo = psi;

            AssertExtensions.Throws<ArgumentNullException>("item", () => testProcess.Start());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void StartProcessWithSameArgumentList()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName());
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            Process secondTestProcess = CreateProcess();
            testProcess.StartInfo = psi;
            try
            {
                testProcess.Start();
                Assert.Equal(string.Empty, testProcess.StartInfo.Arguments);
                secondTestProcess.StartInfo = psi;
                secondTestProcess.Start();
                Assert.Equal(string.Empty, secondTestProcess.StartInfo.Arguments);
            }
            finally
            {
                testProcess.Kill();

                Assert.True(testProcess.WaitForExit(WaitInMS));

                secondTestProcess.Kill();

                Assert.True(testProcess.WaitForExit(WaitInMS));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void BothArgumentCtorAndArgumentListSet()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName(), "arg3");
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            testProcess.StartInfo = psi;
            Assert.Throws<InvalidOperationException>(() => testProcess.Start());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void BothArgumentSetAndArgumentListSet()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName());
            psi.Arguments = "arg3";
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            testProcess.StartInfo = psi;
            Assert.Throws<InvalidOperationException>(() => testProcess.Start());
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void Kill_EntireProcessTree_True_ProcessNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.Kill(entireProcessTree: true));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Currently, remote processes are only supported on Windows. If that changes, adjust accordingly.
        public void Kill_EntireProcessTree_True_CalledByNonLocalProcess_ThrowsInvalidOperationException()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process process;

            try
            {
                process = Process.GetProcessById(currentProcess.Id, "127.0.0.1");
            }
            catch (InvalidOperationException)
            {
                // As we can't detect reliably if performance counters are enabled,
                // we silently abort on InvalidOperationExceptions since this test
                // can only run if the attempt to get the process succeeded.
                return;
            }

            Assert.Throws<NotSupportedException>(() => process.Kill(entireProcessTree: true));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
        public void Kill_EntireProcessTree_True_CalledOnCallingProcess_ThrowsInvalidOperationException()
        {
            var process = Process.GetCurrentProcess();
            Assert.Throws<InvalidOperationException>(() => process.Kill(entireProcessTree: true));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Kill_EntireProcessTree_True_CalledOnTreeContainingCallingProcess_ThrowsInvalidOperationException()
        {
            Process containingProcess = CreateProcess(() =>
            {
                Process parentProcess = CreateProcess(() => RunProcessAttemptingToKillEntireTreeOnParent());

                parentProcess.Start();
                parentProcess.WaitForExit();

                return parentProcess.ExitCode;

            });

            containingProcess.Start();
            containingProcess.WaitForExit();

            if (containingProcess.ExitCode != 10)
                Assert.True(false, "attempt to terminate a process tree containing the calling process did not throw the expected exception");

            int RunProcessAttemptingToKillEntireTreeOnParent()
            {
                Process process = CreateProcess(parentProcessIdString =>
                {
                    Process parentProcess = Process.GetProcessById(int.Parse(parentProcessIdString));

                    bool caught = false;
                    try
                    {
                        parentProcess.Kill(entireProcessTree: true);
                    }
                    catch (InvalidOperationException)
                    {
                        caught = true;
                    }
                    return caught ? 10 : 20;
                }, Process.GetCurrentProcess().Id.ToString());

                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void Kill_ExitedChildProcess_DoesNotThrow(bool killTree)
        {
            Process process = CreateProcess();
            process.Start();

            Assert.True(process.WaitForExit(Helpers.PassingTestTimeoutMilliseconds), $"Process {process.Id} did not finish in {Helpers.PassingTestTimeoutMilliseconds}.");

            process.Kill(killTree);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task Kill_EntireProcessTree_False_OnlyRootProcessTerminated()
        {
            IReadOnlyList<Process> tree = CreateProcessTree();

            try
            {
                Process parentProcess = tree.First();

                parentProcess.Kill(entireProcessTree: false);

                await Helpers.RetryWithBackoff(() =>
                {
                    var actual = tree.Select(p => p.HasExited).ToList();
                    Assert.Equal(new[] { true, false, false }, actual);
                });
            }
            finally
            {
                foreach (Process process in tree)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Test cleanup code, so ignore any exceptions.
                    }
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task Kill_EntireProcessTree_True_EntireTreeTerminated()
        {
            IReadOnlyList<Process> tree = CreateProcessTree();

            try
            {
                Process parentProcess = tree.First();

                parentProcess.Kill(entireProcessTree: true);

                await Helpers.RetryWithBackoff(() =>
                {
                    var actual = tree.Select(p => p.HasExited).ToList();
                    Assert.Equal(new[] { true, true, true }, actual);
                });
            }
            finally
            {
                foreach (Process process in tree)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Test cleanup code, so ignore any exceptions.
                    }
                }
            }
        }

        [Fact]
        public void Start_ThrowsArgumentNullExceptionForNullFileName()
        {
            Assert.Throws<ArgumentNullException>("fileName", () => Process.Start(null, Enumerable.Repeat("notNull", 1)));
        }

        [Fact]
        public void Start_ThrowsArgumentNullExceptionForNullArgumentsList()
        {
            IEnumerable<string> @null = null;
            Assert.Throws<ArgumentNullException>("arguments", () => Process.Start("notNull", @null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // cmd.exe is available only on Windows
        public void Start_PassesArgumentsList_WhichGetsEscaped()
        {
            string folderNameWithSpaces = "folder name with spaces"; // this needs escaping
            string fullPath = Path.Combine(TestDirectory, folderNameWithSpaces);
            string[] arguments = new string[] { "/c", "mkdir", fullPath };

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath);
            }

            using (Process mkdir = Process.Start("cmd.exe", arguments))
            {
                Assert.Equal(arguments, mkdir.StartInfo.ArgumentList);

                mkdir.WaitForExit(WaitInMS);

                Assert.True(Directory.Exists(fullPath));
                Directory.Delete(fullPath);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindowsAndNotElevated))]
        public void NonElevatedUser_QueryProcessNameOfSystemProcess()
        {
            const string Services = "services";

            string currentProcessUser = Helpers.GetProcessUserName(Process.GetCurrentProcess());
            Assert.NotNull(currentProcessUser);

            Process? systemOwnedServices = null;

            foreach (var p in Process.GetProcessesByName(Services))
            {
                // returns the username of the owner of the process or null if the username can't be queried.
                // for services.exe, this will be null.
                string? servicesUser = Helpers.GetProcessUserName(p); 

                // this isn't really verifying that services.exe is owned by SYSTEM, but we are sure it is not owned by the current user.
                if (servicesUser != currentProcessUser)
                {
                    systemOwnedServices = p;
                    break;
                }
            }

            Assert.NotNull(systemOwnedServices);
            Assert.Equal(Services, systemOwnedServices.ProcessName);

            systemOwnedServices = Process.GetProcessById(systemOwnedServices.Id);
            Assert.Equal(Services, systemOwnedServices.ProcessName);
        }

        private IReadOnlyList<Process> CreateProcessTree()
        {
            (Process Value, string Message) rootResult = ListenForAnonymousPipeMessage(rootPipeHandleString =>
            {
                Process root = CreateProcess(rhs =>
                {
                    (Process Value, string Message) child1Result = ListenForAnonymousPipeMessage(child1PipeHandleString =>
                    {
                        Process child1 = CreateProcess(c1hs =>
                        {
                            Process child2 = CreateProcess(() => WaitForever());
                            child2.Start();

                            SendMessage(child2.Id.ToString(), c1hs);

                            return WaitForever();
                        }, child1PipeHandleString, autoDispose: false);

                        child1.Start();

                        return child1;
                    });

                    var child1ProcessId = child1Result.Value.Id;
                    var child2ProcessId = child1Result.Message;
                    SendMessage($"{child1ProcessId};{child2ProcessId}", rhs);

                    return WaitForever();
                }, rootPipeHandleString, autoDispose: false);

                root.Start();

                return root;
            });

            IEnumerable<Process> childProcesses = rootResult.Message
                .Split(';')
                .Select(x => int.Parse(x))
                .Select(pid => Process.GetProcessById(pid));

            return new[] { rootResult.Value }
                .Concat(childProcesses)
                .ToList();

            int WaitForever()
            {
                Thread.Sleep(Timeout.Infinite);

                // never reaches here -- but necessary to satisfy method's signature
                return RemoteExecutor.SuccessExitCode;
            }

            void SendMessage(string message, string handleAsString)
            {
                using (var client = new AnonymousPipeClientStream(PipeDirection.Out, handleAsString))
                {
                    using (var sw = new StreamWriter(client))
                    {
                        sw.WriteLine(message);
                    }
                }
            }

            (T Value, string Message) ListenForAnonymousPipeMessage<T>(Func<string, T> action)
            {
                using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
                {
                    string handleAsString = pipeServer.GetClientHandleAsString();

                    T result = action(handleAsString);

                    pipeServer.DisposeLocalCopyOfClientHandle();

                    using (var sr = new StreamReader(pipeServer))
                    {
                        return (result, sr.ReadLine());
                    }
                }
            }
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
