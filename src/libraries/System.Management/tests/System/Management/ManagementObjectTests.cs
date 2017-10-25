// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management;
using Xunit;

namespace System.Management.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WMI not supported via UAP")]
    public class ManagementObjectTests
    {
        private static string s_systemDriveId = Environment.GetEnvironmentVariable("SystemDrive");

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Get_Win32_LogicalDisk()
        {
            using (ManagementObject obj = new ManagementObject($"Win32_LogicalDisk.DeviceID=\"{s_systemDriveId}\""))
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetRelated_For_Win32_LogicalDisk()
        {
            using (ManagementObject obj = new ManagementObject($"Win32_LogicalDisk.DeviceID=\"{s_systemDriveId}\""))
            using (ManagementObjectCollection relatedCollection = obj.GetRelated())
            {
                Assert.True(relatedCollection.Count > 0);
                foreach (ManagementObject related in relatedCollection)
                    Assert.False(string.IsNullOrWhiteSpace(related.ClassPath.NamespacePath));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Set_Property_Win32_ComputerSystem()
        {
            using (ManagementObject obj = new ManagementObject($"Win32_ComputerSystem.Name=\"{Environment.MachineName}\""))
            {
                obj.Get();
                obj.SetPropertyValue("Workgroup", "WmiTests");
            }
        }
    }
}
