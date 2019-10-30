// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace Common.Tests
{
    public class cgroupsTests : FileCleanupTestBase
    {
        [Theory]
        [InlineData(true, "0", 0)]
        [InlineData(false, "max", 0)]
        [InlineData(true, "1k", 1024)]
        [InlineData(true, "1K", 1024)]
        public void ValidateTryReadMemoryValue(bool expectedResult, string valueText, ulong expectedValue)
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, valueText);

            Assert.Equal(expectedResult, Interop.cgroups.TryReadMemoryValueFromFile(path, out ulong val));
            if (expectedResult)
            {
                Assert.Equal(expectedValue, val);
            }
        }

        [Theory]
        [InlineData(false, "0 0 0:0 / /foo ignore ignore - overlay overlay ignore", "ignore", 0, "/", "/")]
        [InlineData(true, "0 0 0:0 / /foo ignore ignore - cgroup2 cgroup2 ignore", "ignore", 2, "/", "/foo")]
        [InlineData(true, "0 0 0:0 / /foo ignore ignore - cgroup2 cgroup2 ignore", "memory", 2, "/", "/foo")]
        [InlineData(true, "0 0 0:0 / /foo ignore ignore - cgroup2 cgroup2 ignore", "cpu", 2, "/", "/foo")]
        [InlineData(true, "0 0 0:0 / /foo ignore - cgroup2 cgroup2 ignore", "cpu", 2, "/", "/foo")]
        [InlineData(true, "0 0 0:0 / /foo ignore ignore ignore - cgroup2 cgroup2 ignore", "cpu", 2, "/", "/foo")]
        [InlineData(true, "0 0 0:0 / /foo-with-dashes ignore ignore - cgroup2 cgroup2 ignore", "ignore", 2, "/", "/foo-with-dashes")]
        [InlineData(true, "0 0 0:0 / /foo ignore ignore - cgroup cgroup memory", "memory", 1, "/", "/foo")]
        [InlineData(true, "0 0 0:0 / /foo-with-dashes ignore ignore - cgroup cgroup memory", "memory", 1, "/", "/foo-with-dashes")]
        [InlineData(true, "0 0 0:0 / /foo ignore ignore - cgroup cgroup cpu,memory", "memory", 1, "/", "/foo")]
        [InlineData(true, "0 0 0:0 / /foo ignore ignore - cgroup cgroup memory,cpu", "memory", 1, "/", "/foo")]
        [InlineData(false, "0 0 0:0 / /foo ignore ignore - cgroup cgroup cpu", "memory", 0, "/", "/foo")]
        public void ParseValidateMountInfo(bool expectedFound, string procSelfMountInfoText, string subsystem, int expectedVersion, string expectedRoot, string expectedMount)
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, procSelfMountInfoText);

            Assert.Equal(expectedFound, Interop.cgroups.TryFindHierarchyMount(path, subsystem, out Interop.cgroups.CGroupVersion version, out string root, out string mount));
            if (expectedFound)
            {
                Assert.Equal(expectedVersion, (int)version);
                Assert.Equal(expectedRoot, root);
                Assert.Equal(expectedMount, mount);
            }
        }

        [Theory]
        [InlineData(true, "0::/foo", "ignore", "/foo")]
        [InlineData(true, "0::/bar", "ignore", "/bar")]
        [InlineData(true, "0::frob", "ignore", "frob")]
        [InlineData(false, "1::frob", "ignore", "ignore")]
        [InlineData(true, "1:foo:bar", "foo", "bar")]
        [InlineData(true, "2:foo:bar", "foo", "bar")]
        [InlineData(false, "2:foo:bar", "bar", "ignore")]
        [InlineData(true, "1:foo:bar\n2:eggs:spam", "foo", "bar")]
        [InlineData(true, "1:foo:bar\n2:eggs:spam", "eggs", "spam")]
        public void ParseValidateProcCGroup(bool expectedFound, string procSelfCgroupText, string subsystem, string expectedMountPath)
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, procSelfCgroupText);

            Assert.Equal(expectedFound, Interop.cgroups.TryFindCGroupPathForSubsystem(path, subsystem, out string mountPath));
            if (expectedFound)
            {
                Assert.Equal(expectedMountPath, mountPath);
            }
        }
    }
}
