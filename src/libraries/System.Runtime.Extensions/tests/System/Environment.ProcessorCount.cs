// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public class EnvironmentProcessorCount
    {
        private const string ProcessorCountEnvVar = "DOTNET_PROCESSOR_COUNT";

        [Fact]
        public void ProcessorCount_IsPositive()
        {
            Assert.InRange(Environment.ProcessorCount, 1, int.MaxValue);
        }

        private static unsafe int ParseProcessorCount(string settingValue)
        {
            const uint MAX_PROCESSOR_COUNT = 0xffff;

            if (string.IsNullOrEmpty(settingValue))
                return 0;

            // Mimic handling the setting's value in coreclr's GetCurrentProcessCpuCount
            fixed (char *ptr = settingValue)
            {
                char *endptr;
                int value = (int)wcstoul(ptr, &endptr, 10);

                if (0 < value && value <= MAX_PROCESSOR_COUNT)
                    return value;
            }

            return 0;
        }

        private static int GetTotalProcessorCount()
        {
            // Assume a single CPU group
            GetSystemInfo(out SYSTEM_INFO sysInfo);
            return (int)sysInfo.dwNumberOfProcessors;
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Uses P/Invokes to get processor information
        [Fact]
        public void ProcessorCount_Windows_MatchesGetSystemInfo()
        {
            string procCountConfig = Environment.GetEnvironmentVariable(ProcessorCountEnvVar);
            int expectedCount = ParseProcessorCount(procCountConfig);

            // Assume no process affinity or CPU quota set
            if (expectedCount == 0)
                expectedCount = GetTotalProcessorCount();

            Assert.Equal(expectedCount, Environment.ProcessorCount);
        }

        public static int GetProcessorCount() => Environment.ProcessorCount;

        [PlatformSpecific(TestPlatforms.Windows)]
        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52910", TestRuntimes.Mono)]
        [InlineData(8000, 0, null)]
        [InlineData(8000, 2000, null)]
        [InlineData(8000, 0, "1")]
        [InlineData(2000, 0, null)]
        [InlineData(2000, 0, " 17 ")]
        [InlineData(0, 0, "3")]
        public static unsafe void ProcessorCount_Windows_RespectsJobCpuRateAndConfigurationSetting(
            ushort maxRate, ushort minRate, string procCountConfig)
        {
            IntPtr hJob = IntPtr.Zero;
            PROCESS_INFORMATION processInfo = default;
            string savedProcCountConfig = Environment.GetEnvironmentVariable(ProcessorCountEnvVar);

            try
            {
                hJob = CreateJobObject(IntPtr.Zero, null);
                JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuRateControl = default;

                if (maxRate != 0)
                {
                    // Setting JobObjectCpuRateControlInformation requires Windows 8 or later
                    if (!PlatformDetection.IsWindows8xOrLater)
                        return;

                    if (minRate == 0)
                    {
                        cpuRateControl.ControlFlags =
                            JobObjectCpuRateControlFlags.JOB_OBJECT_CPU_RATE_CONTROL_ENABLE |
                            JobObjectCpuRateControlFlags.JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP;
                        cpuRateControl.CpuRate = maxRate;
                    }
                    else
                    {
                        // Setting min and max rates requires Windows 10 or later
                        if (!PlatformDetection.IsWindows10OrLater)
                            return;

                        cpuRateControl.ControlFlags =
                            JobObjectCpuRateControlFlags.JOB_OBJECT_CPU_RATE_CONTROL_ENABLE |
                            JobObjectCpuRateControlFlags.JOB_OBJECT_CPU_RATE_CONTROL_MIN_MAX_RATE;
                        cpuRateControl.MinRate = minRate;
                        cpuRateControl.MaxRate = maxRate;
                    }

                    if (!SetInformationJobObject(
                        hJob, JOBOBJECTINFOCLASS.JobObjectCpuRateControlInformation, (IntPtr)(&cpuRateControl), (uint)Marshal.SizeOf(cpuRateControl)))
                        throw new Win32Exception();
                }

                ProcessStartInfo startInfo;

                using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(GetProcessorCount, new RemoteInvokeOptions { Start = false }))
                {
                    startInfo = handle.Process.StartInfo;
                    handle.Process.Dispose();
                    handle.Process = null;
                }

                STARTUPINFO startupInfo = new() { cb = (uint)Marshal.SizeOf<STARTUPINFO>() };
                Environment.SetEnvironmentVariable(ProcessorCountEnvVar, procCountConfig);

                if (!CreateProcess(
                    startInfo.FileName, $"\"{startInfo.FileName}\" {startInfo.Arguments}",
                    IntPtr.Zero, IntPtr.Zero, false, CREATE_SUSPENDED,
                    IntPtr.Zero, null, ref startupInfo, out processInfo))
                    throw new Win32Exception();

                if (!AssignProcessToJobObject(hJob, processInfo.hProcess))
                    throw new Win32Exception();

                uint result = ResumeThread(processInfo.hThread);
                if (result == RESUME_THREAD_FAILED)
                    throw new Win32Exception();

                const uint WaitTime = 3 * 60 * 1000; // Three minutes
                result = WaitForSingleObject(processInfo.hProcess, WaitTime);

                if (result == WAIT_FAILED)
                    throw new Win32Exception();

                if (result != WAIT_OBJECT_0)
                    throw new Exception("Error waiting for the child process");

                if (!GetExitCodeProcess(processInfo.hProcess, out uint exitCode))
                    throw new Win32Exception();

                int expectedCount = ParseProcessorCount(procCountConfig);

                if (expectedCount == 0)
                {
                    int totalProcCount = GetTotalProcessorCount();

                    if (maxRate == 0)
                        expectedCount = totalProcCount;
                    else
                        expectedCount = (maxRate * totalProcCount + 9999) / 10000;
                }

                Assert.Equal(expectedCount, (int)exitCode);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ProcessorCountEnvVar, savedProcCountConfig);

                if (processInfo.hProcess != IntPtr.Zero)
                {
                    TerminateProcess(processInfo.hProcess, unchecked((uint)(-1)));
                    CloseHandle(processInfo.hProcess);
                }

                if (processInfo.hThread != IntPtr.Zero)
                    CloseHandle(processInfo.hThread);

                if (hJob != IntPtr.Zero)
                    CloseHandle(hJob);
            }
        }

        [DllImport("msvcrt.dll")]
        private static extern unsafe uint wcstoul(char *strSource, char **endptr, int @base);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [Flags]
        private enum JobObjectCpuRateControlFlags : uint
        {
            JOB_OBJECT_CPU_RATE_CONTROL_ENABLE = 0x1,
            JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP = 0x4,
            JOB_OBJECT_CPU_RATE_CONTROL_MIN_MAX_RATE = 0x10,
        }

        private enum JOBOBJECTINFOCLASS
        {
            JobObjectCpuRateControlInformation = 15,
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
        {
            [FieldOffset(0)]
            public JobObjectCpuRateControlFlags ControlFlags;
            [FieldOffset(4)]
            public uint CpuRate;
            [FieldOffset(4)]
            public uint Weight;
            [FieldOffset(4)]
            public ushort MinRate;
            [FieldOffset(6)]
            public ushort MaxRate;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(
            IntPtr hJob, JOBOBJECTINFOCLASS JobObjectInformationClass,
            IntPtr lpJobObjectInformation, uint cbJobObjectInformationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryInformationJobObject(
            IntPtr hJob, JOBOBJECTINFOCLASS JobObjectInformationClass,
            IntPtr lpJobObjectInformation, uint cbJobObjectInformationLength, IntPtr lpReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        private const uint CREATE_SUSPENDED = 0x00000004;

        private struct STARTUPINFO
        {
            public uint cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public ushort wShowWindow;
            public ushort cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           IntPtr lpProcessAttributes,
           IntPtr lpThreadAttributes,
           bool bInheritHandles,
           uint dwCreationFlags,
           IntPtr lpEnvironment,
           string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        private const uint RESUME_THREAD_FAILED = unchecked((uint)(-1));

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        private const uint WAIT_OBJECT_0 = 0;
        private const uint WAIT_FAILED = unchecked((uint)(-1));

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
