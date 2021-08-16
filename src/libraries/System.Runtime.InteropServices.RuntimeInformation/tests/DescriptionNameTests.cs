// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public class DescriptionNameTests
    {
        // When running both inner and outer loop together, dump only once
        private static bool s_dumpedRuntimeInfo = false;

        private static readonly bool s_isInHelix = Environment.GetEnvironmentVariables().Keys.Cast<string>().Where(key => key.StartsWith("HELIX")).Any();

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "throws PNSE when binariesLocation is not an empty string.")]
        public void DumpRuntimeInformationToConsole()
        {
            if (s_dumpedRuntimeInfo || !s_isInHelix)
                return;

            s_dumpedRuntimeInfo = true;

            // Not really a test, but useful to dump a variety of information to the test log to help
            // debug environmental issues, in particular in CI

            string dvs = PlatformDetection.GetDistroVersionString();
            string osd = RuntimeInformation.OSDescription.Trim();
            string osv = Environment.OSVersion.ToString();
            string osa = RuntimeInformation.OSArchitecture.ToString();
            string rid = RuntimeInformation.RuntimeIdentifier;
            Console.WriteLine($"### OS: Distro={dvs} Description={osd} Version={osv} Arch={osa} Rid={rid}");

            string lcr = PlatformDetection.LibcRelease;
            string lcv = PlatformDetection.LibcVersion;
            Console.WriteLine($"### LIBC: Release={lcr} Version={lcv}");

            Console.WriteLine($"### FRAMEWORK: Version={Environment.Version} Description={RuntimeInformation.FrameworkDescription.Trim()}");

            string binariesLocation = Path.GetDirectoryName(typeof(object).Assembly.Location);
            string binariesLocationFormat = PlatformDetection.IsInAppContainer ? "Unknown" : new DriveInfo(binariesLocation).DriveFormat;
            Console.WriteLine($"### BINARIES: {binariesLocation} (drive format {binariesLocationFormat})");

            string tempPathLocation = Path.GetTempPath();
            string tempPathLocationFormat = PlatformDetection.IsInAppContainer ? "Unknown" : new DriveInfo(tempPathLocation).DriveFormat;
            Console.WriteLine($"### TEMP PATH: {tempPathLocation} (drive format {tempPathLocationFormat})");

            Console.WriteLine($"### CURRENT DIRECTORY: {Environment.CurrentDirectory}");

            Console.WriteLine($"### CGROUPS VERSION: {Interop.cgroups.s_cgroupVersion}");
            string cgroupsLocation = Interop.cgroups.s_cgroupMemoryLimitPath;
            if (cgroupsLocation != null)
            {
                Console.WriteLine($"### CGROUPS MEMORY: {cgroupsLocation}");
            }

            Console.WriteLine($"### ENVIRONMENT VARIABLES");
            foreach (DictionaryEntry envvar in Environment.GetEnvironmentVariables())
            {
                Console.WriteLine($"###\t{envvar.Key}: {envvar.Value}");
            }

            using (Process p = Process.GetCurrentProcess())
            {
                var sb = new StringBuilder();
                sb.AppendLine("### PROCESS INFORMATION:");
                sb.AppendLine($"###\tArchitecture: {RuntimeInformation.ProcessArchitecture}");
                foreach (string prop in new string[]
                {
                        nameof(p.BasePriority),
                        nameof(p.HandleCount),
                        nameof(p.Id),
                        nameof(p.MachineName),
                        nameof(p.MainModule),
                        nameof(p.MainWindowHandle),
                        nameof(p.MainWindowTitle),
                        nameof(p.MaxWorkingSet),
                        nameof(p.MinWorkingSet),
                        nameof(p.NonpagedSystemMemorySize64),
                        nameof(p.PagedMemorySize64),
                        nameof(p.PagedSystemMemorySize64),
                        nameof(p.PeakPagedMemorySize64),
                        nameof(p.PeakVirtualMemorySize64),
                        nameof(p.PeakWorkingSet64),
                        nameof(p.PriorityBoostEnabled),
                        nameof(p.PriorityClass),
                        nameof(p.PrivateMemorySize64),
                        nameof(p.PrivilegedProcessorTime),
                        nameof(p.ProcessName),
                        nameof(p.ProcessorAffinity),
                        nameof(p.Responding),
                        nameof(p.SessionId),
                        nameof(p.StartTime),
                        nameof(p.TotalProcessorTime),
                        nameof(p.UserProcessorTime),
                        nameof(p.VirtualMemorySize64),
                        nameof(p.WorkingSet64),
                })
                {
                    sb.Append($"###\t{prop}: ");
                    try
                    {
                        sb.Append(p.GetType().GetProperty(prop).GetValue(p));
                    }
                    catch (Exception e)
                    {
                        sb.Append($"(Exception: {e.Message})");
                    }
                    sb.AppendLine();
                }
                Console.WriteLine(sb.ToString());
            }

            if (osd.Contains("Linux"))
            {
                // Dump several procfs files and /etc/os-release
                foreach (string path in new string[] {
                    "/proc/self/mountinfo",
                    "/proc/self/cgroup",
                    "/proc/self/limits",
                    "/etc/os-release",
                    "/etc/sysctl.conf",
                    "/proc/meminfo",
                    "/proc/sys/vm/oom_kill_allocating_task",
                    "/proc/sys/kernel/core_pattern",
                    "/proc/sys/kernel/core_uses_pid",
                    "/proc/sys/kernel/coredump_filter"
                })
                {
                    Console.WriteLine($"### CONTENTS OF \"{path}\":");
                    try
                    {
                        using (Process cat = new Process())
                        {
                            cat.StartInfo.FileName = "cat";
                            cat.StartInfo.Arguments = path;
                            cat.StartInfo.RedirectStandardOutput = true;
                            cat.OutputDataReceived += (sender, e) =>
                            {
                                string trimmed = e.Data?.Trim();
                                if (!string.IsNullOrEmpty(trimmed) && trimmed[0] != '#') // skip comments in files
                                {
                                    Console.WriteLine(e.Data);
                                }
                            };
                            cat.Start();
                            cat.BeginOutputReadLine();
                            cat.WaitForExit();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"###\t(Exception: {e.Message})");
                    }
                }
            }
        }

        [Fact]
        [OuterLoop]
        [SkipOnPlatform(TestPlatforms.Browser, "throws PNSE when binariesLocation is not an empty string.")]
        public void DumpRuntimeInformationToConsoleOuter()
        {
            // Outer loop runs don't run inner loop tests.
            // But we want to log this data for any Helix run.
            DumpRuntimeInformationToConsole();
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp)]
        public void VerifyRuntimeNameOnNetCoreApp()
        {
            Assert.True(RuntimeInformation.FrameworkDescription.StartsWith(".NET"), RuntimeInformation.FrameworkDescription);
            Assert.Same(RuntimeInformation.FrameworkDescription, RuntimeInformation.FrameworkDescription);
        }

        [Fact]
        public void VerifyOSDescription()
        {
            Assert.NotNull(RuntimeInformation.OSDescription);
            Assert.Same(RuntimeInformation.OSDescription, RuntimeInformation.OSDescription);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void VerifyWindowsDescriptionDoesNotContainTrailingWhitespace()
        {
            Assert.False(RuntimeInformation.OSDescription.EndsWith(" "));
        }

        [Fact, PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows name in RuntimeInformation
        public void VerifyWindowsName()
        {
            Assert.Contains("windows", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.Linux)]  // Checks Linux name in RuntimeInformation
        public void VerifyLinuxName()
        {
            Assert.Contains("linux", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.NetBSD)]  // Checks NetBSD name in RuntimeInformation
        public void VerifyNetBSDName()
        {
            Assert.Contains("netbsd", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.FreeBSD)]  // Checks FreeBSD name in RuntimeInformation
        public void VerifyFreeBSDName()
        {
            Assert.Contains("FreeBSD", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.OSX)]  // Checks OSX name in RuntimeInformation
        public void VerifyOSXName()
        {
            Assert.Contains("darwin", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }
    }
}
