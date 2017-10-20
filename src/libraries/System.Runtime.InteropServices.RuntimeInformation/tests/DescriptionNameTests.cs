// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            Console.WriteLine($@"{dvs} OS={osd} OSVer={osv} OSArch={osa} Arch={pra} Framework={frd}");
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp)]
        public void VerifyRuntimeDebugNameOnNetCoreApp()
        {
            AssemblyFileVersionAttribute attr = (AssemblyFileVersionAttribute)(typeof(object).GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)));
            string expected = string.Format(".NET Core {0}", attr.Version);
            Assert.Equal(expected, RuntimeInformation.FrameworkDescription);
            Assert.Same(RuntimeInformation.FrameworkDescription, RuntimeInformation.FrameworkDescription);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void VerifyRuntimeDebugNameOnNetFramework()
        {
            AssemblyFileVersionAttribute attr = (AssemblyFileVersionAttribute)(typeof(object).GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)));
            string expected = string.Format(".NET Framework {0}", attr.Version);
            Assert.Equal(expected, RuntimeInformation.FrameworkDescription);
            Assert.Same(RuntimeInformation.FrameworkDescription, RuntimeInformation.FrameworkDescription);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.Netcoreapp)]
        public void VerifyRuntimeDebugNameOnNetCoreUwp()
        {
            AssemblyFileVersionAttribute attr = (AssemblyFileVersionAttribute)(typeof(object).GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)));
            string expected = string.Format(PlatformDetection.IsNetNative ? ".NET Native {0}" : ".NET Core {0}", attr.Version);
            Assert.Equal(expected, RuntimeInformation.FrameworkDescription);
            Assert.Same(RuntimeInformation.FrameworkDescription, RuntimeInformation.FrameworkDescription);
        }

        [Fact]
        public void VerifyOSDescription()
        {
            Assert.NotNull(RuntimeInformation.OSDescription);
            Assert.Same(RuntimeInformation.OSDescription, RuntimeInformation.OSDescription);
        }

        [Fact, PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows debug name in RuntimeInformation
        public void VerifyWindowsDebugName()
        {
            Assert.Contains("windows", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.Linux)]  // Checks Linux debug name in RuntimeInformation
        public void VerifyLinuxDebugName()
        {
            Assert.Contains("linux", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.NetBSD)]  // Checks NetBSD debug name in RuntimeInformation
        public void VerifyNetBSDDebugName()
        {
            Assert.Contains("netbsd", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.FreeBSD)]  // Checks FreeBSD debug name in RuntimeInformation
        public void VerifyFreeBSDDebugName()
        {
            Assert.Contains("FreeBSD", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(TestPlatforms.OSX)]  // Checks OSX debug name in RuntimeInformation
        public void VerifyOSXDebugName()
        {
            Assert.Contains("darwin", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }
    }
}
