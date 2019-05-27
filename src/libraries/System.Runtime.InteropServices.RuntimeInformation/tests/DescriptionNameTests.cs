// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public class DescriptionNameTests
    {
        [Fact]
        public void DumpRuntimeInformationToConsole()
        {
            // Not really a test, but useful to dump a variety of information to the test log to help
            // debug environmental issues, in particular in CI

            string dvs = PlatformDetection.GetDistroVersionString();
            string osd = RuntimeInformation.OSDescription.Trim();
            string osv = Environment.OSVersion.ToString();
            string osa = RuntimeInformation.OSArchitecture.ToString();
            Console.WriteLine($"### OS: Distro={dvs} Description={osd} Version={osv} Arch={osa}");

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

            string cgroupsLocation = Interop.cgroups.s_cgroupMemoryPath;
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
                sb.AppendFormat($"###\tArchitecture: {RuntimeInformation.ProcessArchitecture.ToString()}").AppendLine();
                foreach (string prop in new string[]
                {
                        #pragma warning disable 0618 // some of these Int32-returning properties are marked obsolete
                        nameof(p.BasePriority),
                        nameof(p.HandleCount),
                        nameof(p.Id),
                        nameof(p.MachineName),
                        nameof(p.MainModule),
                        nameof(p.MainWindowHandle),
                        nameof(p.MainWindowTitle),
                        nameof(p.MaxWorkingSet),
                        nameof(p.MinWorkingSet),
                        nameof(p.NonpagedSystemMemorySize),
                        nameof(p.NonpagedSystemMemorySize64),
                        nameof(p.PagedMemorySize),
                        nameof(p.PagedMemorySize64),
                        nameof(p.PagedSystemMemorySize),
                        nameof(p.PagedSystemMemorySize64),
                        nameof(p.PeakPagedMemorySize),
                        nameof(p.PeakPagedMemorySize64),
                        nameof(p.PeakVirtualMemorySize),
                        nameof(p.PeakVirtualMemorySize64),
                        nameof(p.PeakWorkingSet),
                        nameof(p.PeakWorkingSet64),
                        nameof(p.PriorityBoostEnabled),
                        nameof(p.PriorityClass),
                        nameof(p.PrivateMemorySize),
                        nameof(p.PrivateMemorySize64),
                        nameof(p.PrivilegedProcessorTime),
                        nameof(p.ProcessName),
                        nameof(p.ProcessorAffinity),
                        nameof(p.Responding),
                        nameof(p.SessionId),
                        nameof(p.StartTime),
                        nameof(p.TotalProcessorTime),
                        nameof(p.UserProcessorTime),
                        nameof(p.VirtualMemorySize),
                        nameof(p.VirtualMemorySize64),
                        nameof(p.WorkingSet),
                        nameof(p.WorkingSet64),
                        #pragma warning restore 0618
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
                // Dump several procfs files
                foreach (string path in new string[] { "/proc/self/mountinfo", "/proc/self/cgroup", "/proc/self/limits" })
                {
                    Console.WriteLine($"### CONTENTS OF \"{path}\":");
                    try
                    {
                        using (Process cat = Process.Start("cat", path))
                        {
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
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp)]
        public void VerifyRuntimeNameOnNetCoreApp()
        {
            Assert.True(RuntimeInformation.FrameworkDescription.StartsWith(".NET Core"), RuntimeInformation.FrameworkDescription);
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
