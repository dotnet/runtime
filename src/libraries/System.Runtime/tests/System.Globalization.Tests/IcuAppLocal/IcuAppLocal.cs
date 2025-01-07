// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class IcuAppLocalTests
    {
        private static bool SupportsIcuPackageDownload => PlatformDetection.IsNotHybridGlobalizationOnApplePlatform && RemoteExecutor.IsSupported &&
                                                          ((PlatformDetection.IsWindows && !PlatformDetection.IsArmProcess) ||
                                                           (PlatformDetection.IsLinux && (PlatformDetection.IsX64Process || PlatformDetection.IsArm64Process) &&
                                                           !PlatformDetection.IsAlpine && !PlatformDetection.IsLinuxBionic));


        [ConditionalFact(nameof(SupportsIcuPackageDownload))]
        public void TestIcuAppLocal()
        {
            // We define this switch dynamically during the runtime using RemoteExecutor.
            // The reason is, if we enable ICU app-local here, this test will compile and run
            // on all supported OSs even the ICU NuGet package not have native bits support such OSs.
            // Note, it doesn't matter if we have test case conditioned to not run on such OSs, because
            // the test has to start running first before filtering the test cases and the globalization
            // code will run and fail fast at that time.

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", "68.2.0.9");

            RemoteExecutor.Invoke(() =>
            {
                // Start with calling Globalization to force the initialization.
                CultureInfo ci = CultureInfo.GetCultureInfo("en-US");

                Type? interopGlobalization = Type.GetType("Interop+Globalization, System.Private.CoreLib");
                Assert.NotNull(interopGlobalization);

                MethodInfo? methodInfo = interopGlobalization.GetMethod("GetICUVersion", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(methodInfo);

                // Assert the ICU version 0x44020009 is 68.2.0.9
                Assert.Equal(0x44020009, (int)methodInfo.Invoke(null, null));

                // Now call globalization API to ensure the binding working without any problem.
                Assert.Equal(-1, ci.CompareInfo.Compare("sample\u0000", "Sample\u0000", CompareOptions.IgnoreSymbols));
            }, new RemoteInvokeOptions { CheckExitCode = false, StartInfo = psi }).Dispose();
        }
    }
}
