// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class OSMinVersionTest
    {
        [ConditionalFact]
        [OSMinVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public void RunTest_Win8DoesNotRunOnWin7()
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                Environment.OSVersion.Version.ToString().StartsWith("6.1"),
                "Test should not be running on Win7 or Win2008R2.");
        }

        [ConditionalTheory]
        [OSMinVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        [InlineData(1)]
        public void RunTheory_Win8DoesNotRunOnWin7(int arg)
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                Environment.OSVersion.Version.ToString().StartsWith("6.1"),
                "Test should not be running on Win7 or Win2008R2.");
        }

        [ConditionalFact]
        [OSMinVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RunTest_Win10_RS4()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            Assert.NotNull(versionKey);
            var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
            Assert.NotNull(currentVersion);
            Assert.True(17134 <= int.Parse(currentVersion));
        }

        [ConditionalFact]
        [OSMinVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2)]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RunTest_Win10_19H2()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            Assert.NotNull(versionKey);
            var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
            Assert.NotNull(currentVersion);
            Assert.True(18363 <= int.Parse(currentVersion));
        }
    }

    [OSMinVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
    public class OSMinVersionClassTest
    {
        [ConditionalFact]
        public void TestSkipClass_Win8DoesNotRunOnWin7()
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                Environment.OSVersion.Version.ToString().StartsWith("6.1"),
                "Test should not be running on Win7 or Win2008R2.");
        }
    }
}
