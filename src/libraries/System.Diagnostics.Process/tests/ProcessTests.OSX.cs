// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Security;
using Xunit;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public unsafe void TestTotalProcessorTimeMacOs()
        {
            var rUsage = Interop.libproc.proc_pid_rusage(Environment.ProcessId);
            var timeBase = new Interop.libSystem.mach_timebase_info_data_t();
            Interop.libSystem.mach_timebase_info(&timeBase);

            var nativeUserNs = rUsage.ri_user_time * timeBase.numer / timeBase.denom;
            var nativeSystemNs = rUsage.ri_system_time * timeBase.numer / timeBase.denom;
            var nativeTotalNs = nativeSystemNs + nativeUserNs;

            var nativeUserTime = TimeSpan.FromMicroseconds(nativeUserNs / 1000);
            var nativeSystemTime = TimeSpan.FromMicroseconds(nativeSystemNs / 1000);
            var nativeTotalTime = TimeSpan.FromMicroseconds(nativeTotalNs / 1000);

            var process = Process.GetCurrentProcess();
            var managedUserTime = process.UserProcessorTime;
            var managedSystemTime = process.PrivilegedProcessorTime;
            var managedTotalTime = process.TotalProcessorTime;

            AssertTime(managedUserTime, nativeUserTime, "user");
            AssertTime(managedSystemTime, nativeSystemTime, "system");
            AssertTime(managedTotalTime, nativeTotalTime, "total");

            void AssertTime(TimeSpan managed, TimeSpan native, string label)
            {
                Assert.True(
                    managed >= native,
                    $"Time '{label}' returned by managed API ({managed}) should be greated or equal to the time returned by native API ({native}).");
            }
        }
    }
}
