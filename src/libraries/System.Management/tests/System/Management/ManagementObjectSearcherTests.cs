// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Management.Tests
{
    public class ManagementObjectSearcherTests
    {
        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        public void Dynamic_Instances()
        {
            using (var searcher = new ManagementObjectSearcher())
            {
                const string targetClass = "Win32_Process";
                var selectQuery = new SelectQuery(targetClass);
                searcher.Query = selectQuery;
                ManagementObjectCollection instances = searcher.Get();
                Assert.True(instances.Count > 0);
                foreach (ManagementObject instance in instances)
                    Assert.Equal(targetClass, instance.Path.ClassName);
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [OuterLoop]
        public void Related_Instances()
        {
            using (var searcher = new ManagementObjectSearcher())
            {
                var relatedObjectQuery = new RelatedObjectQuery($"Win32_LogicalDisk.DeviceID='{WmiTestHelper.SystemDriveId}'");
                searcher.Query = relatedObjectQuery;
                ManagementObjectCollection instances = searcher.Get();
                Assert.True(instances.Count > 0);

                string[] expectedAssociatedInstanceClasses =
                {
                    "Win32_Directory", "Win32_DiskPartition", "Win32_ComputerSystem", "Win32_QuotaSetting", "Win32_SystemAccount", "Win32_Group"
                };

                foreach (ManagementObject instance in instances)
                    Assert.True(expectedAssociatedInstanceClasses.Contains(instance.Path.ClassName), $"Unexpected instance of class {instance.Path.ClassName}");
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [OuterLoop]
        public void Relationship_Classes()
        {
            using (var searcher = new ManagementObjectSearcher())
            {
                var relationshipQuery = new RelationshipQuery($"Win32_LogicalDisk.DeviceID='{WmiTestHelper.SystemDriveId}'");
                searcher.Query = relationshipQuery;
                ManagementObjectCollection instances = searcher.Get();
                Assert.True(instances.Count > 0);

                string[] expectedReferenceClasses =
                {
                    "Win32_LogicalDiskRootDirectory", "Win32_LogicalDiskToPartition", "Win32_SystemDevices", "Win32_VolumeQuotaSetting", "Win32_DiskQuota"
                };

                foreach (ManagementObject instance in instances)
                    Assert.True(expectedReferenceClasses.Contains(instance.Path.ClassName), $"Unexpected instance of class {instance.Path.ClassName}");
            }
        }
    }
}
