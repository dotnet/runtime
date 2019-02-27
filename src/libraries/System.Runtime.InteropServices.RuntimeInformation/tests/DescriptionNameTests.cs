// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public class DescriptionNameTests
    {
        [Fact]
        public void DumpRuntimeInformationToConsole()
        {
            // Not really a test, but useful to dump to the log to
            // sanity check that the test run or CI job
            // was actually run on the OS that it claims to be on
            string dvs = PlatformDetection.GetDistroVersionString();
            string osd = RuntimeInformation.OSDescription.Trim();
            string osv = Environment.OSVersion.ToString();
            string osa = RuntimeInformation.OSArchitecture.ToString();
            string pra = RuntimeInformation.ProcessArchitecture.ToString();
            string frd = RuntimeInformation.FrameworkDescription.Trim();
            string lcr = PlatformDetection.LibcRelease;
            string lcv = PlatformDetection.LibcVersion;

            Console.WriteLine($@"### CONFIGURATION: {dvs} OS={osd} OSVer={osv} OSArch={osa} Arch={pra} Framework={frd} LibcRelease={lcr} LibcVersion={lcv}");

            if (!PlatformDetection.IsNetNative)
            {
                string binariesLocation = Path.GetDirectoryName(typeof(object).Assembly.Location);
                Console.WriteLine("location: " + binariesLocation);
                string binariesLocationFormat = PlatformDetection.IsInAppContainer ? "Unknown" : new DriveInfo(binariesLocation).DriveFormat;
                Console.WriteLine($"### BINARIES: {binariesLocation} (drive format {binariesLocationFormat})");
            }

            string tempPathLocation = Path.GetTempPath();
            string tempPathLocationFormat = PlatformDetection.IsInAppContainer ? "Unknown" : new DriveInfo(tempPathLocation).DriveFormat;
            Console.WriteLine($"### TEMP PATH: {tempPathLocation} (drive format {tempPathLocationFormat})");

            Console.WriteLine($"### CURRENT DIRECTORY: {Environment.CurrentDirectory}");
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp)]
        public void VerifyRuntimeNameOnNetCoreApp()
        {
            Assert.True(RuntimeInformation.FrameworkDescription.StartsWith(".NET Core"), RuntimeInformation.FrameworkDescription);
            Assert.Same(RuntimeInformation.FrameworkDescription, RuntimeInformation.FrameworkDescription);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot)]
        public void VerifyRuntimeNameOnNetNative()
        {
            Assert.True(RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"), RuntimeInformation.FrameworkDescription);
            Assert.Same(RuntimeInformation.FrameworkDescription, RuntimeInformation.FrameworkDescription);
        }

        [Fact]
        public void VerifyOSDescription()
        {
            Assert.NotNull(RuntimeInformation.OSDescription);
            Assert.Same(RuntimeInformation.OSDescription, RuntimeInformation.OSDescription);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
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
