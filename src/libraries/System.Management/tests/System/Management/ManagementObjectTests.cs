// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Sdk;

namespace System.Management.Tests
{
    public class ManagementObjectTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindowsNanoServer))]
        public void PlatformNotSupportedException_On_Nano()
        {
            // The underlying delegate usage can cause some cases to have the PNSE as the inner exception but there is a best effort
            // to throw PNSE for such case.
            Assert.Throws<PlatformNotSupportedException>(() => new ManagementObject($"Win32_LogicalDisk.DeviceID=\"{WmiTestHelper.SystemDriveId}\""));
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34689", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Get_Win32_LogicalDisk()
        {
            using (ManagementObject obj = new ManagementObject($"Win32_LogicalDisk.DeviceID=\"{WmiTestHelper.SystemDriveId}\""))
            {
                obj.Get();
                Assert.True(obj.Properties.Count > 0);
                Assert.True(ulong.Parse(obj["Size"].ToString()) > 0);
                var classPath = obj.ClassPath.Path;
                Assert.Equal($@"\\{Environment.MachineName}\root\cimv2:Win32_LogicalDisk", classPath);

                var clone = obj.Clone();
                Assert.False(ReferenceEquals(clone, obj));
                ((ManagementObject)clone).Dispose();
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [OuterLoop]
        public void GetRelated_For_Win32_LogicalDisk()
        {
            using (ManagementObject obj = new ManagementObject($"Win32_LogicalDisk.DeviceID=\"{WmiTestHelper.SystemDriveId}\""))
            using (ManagementObjectCollection relatedCollection = obj.GetRelated())
            {
                Assert.True(relatedCollection.Count > 0);
                foreach (ManagementObject related in relatedCollection)
                    Assert.False(string.IsNullOrWhiteSpace(related.ClassPath.NamespacePath));
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34689", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Set_Property_Win32_ComputerSystem()
        {
            using (ManagementObject obj = new ManagementObject($"Win32_ComputerSystem.Name=\"{Environment.MachineName}\""))
            {
                obj.Get();
                obj.SetPropertyValue("Workgroup", "WmiTests");
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [OuterLoop]
        public void Invoke_Instance_And_Static_Method_Win32_Process()
        {
            if (PlatformDetection.IsWindows10Version22000OrGreater)
            {
                // https://github.com/dotnet/runtime/issues/70414
                throw new SkipTestException("Unstable on Windows 11");
            }
            // Retries are sometimes necessary as underlying API call can return
            // ERROR_NOT_READY or occasionally ERROR_INVALID_BLOCK or ERROR_NOT_ENOUGH_MEMORY
            RetryHelper.Execute(() =>
            {
                var processClass = new ManagementClass("Win32_Process");
                object[] methodArgs = { "notepad.exe", null, null, 0 };

                object resultObj = processClass.InvokeMethod("Create", methodArgs);

                var resultCode = (uint)resultObj;
                Assert.Equal(0u, resultCode);

                var processId = (uint)methodArgs[3];
                Assert.True(0u != processId, $"Unexpected process ID: {processId}");

                using (Process targetProcess = Process.GetProcessById((int)processId))
                using (var process = new ManagementObject($"Win32_Process.Handle=\"{processId}\""))
                {
                    Assert.False(targetProcess.HasExited);

                    resultObj = process.InvokeMethod("Terminate", new object[] { 0 });
                    resultCode = (uint)resultObj;
                    Assert.Equal(0u, resultCode);

                    Assert.True(targetProcess.HasExited);
                }
            }, maxAttempts: 10, retryWhen: e => e is XunitException);
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [OuterLoop]
        public void Serialize_ManagementException()
        {
            try
            {
                new ManagementObject("Win32_LogicalDisk.DeviceID=\"InvalidDeviceId\"").Get();
            }
            catch (ManagementException e)
            {
                using var ms = new MemoryStream();
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, e);
                ms.Position = 0;

                var exception = (ManagementException)formatter.Deserialize(ms);

                Assert.Equal(e.ErrorCode, exception.ErrorCode);

                // On .NET Framework the `ErrorInformation` underlying field is serialized
                if (PlatformDetection.IsNetFramework)
                {
                    Assert.Equal(e.ErrorInformation, exception.ErrorInformation);
                    return;
                }

                Assert.Null(exception.ErrorInformation);
            }
        }
    }
}
