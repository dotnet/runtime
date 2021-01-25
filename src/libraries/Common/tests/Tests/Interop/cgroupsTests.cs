// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace Common.Tests
{
    public class cgroupsTests : FileCleanupTestBase
    {
        [Fact]
        public void ValidateFindCGroupVersion()
        {
            Assert.InRange((int)Interop.cgroups.s_cgroupVersion, 0, 2);
        }

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
        [InlineData("/sys/fs/cgroup/cpu/my_cgroup", "/docker/1234", "/sys/fs/cgroup/cpu", "/docker/1234/my_cgroup")]
        [InlineData("/sys/fs/cgroup/cpu/my_cgroup", "/", "/sys/fs/cgroup/cpu", "/my_cgroup")]
        public void ValidateFindCGroupPath(string expectedResult, string hierarchyRoot, string hierarchyMount, string cgroupPathRelativeToMount)
        {
            Assert.Equal(expectedResult, Interop.cgroups.FindCGroupPath(hierarchyRoot, hierarchyMount, cgroupPathRelativeToMount));
        }

        [Theory]
        [InlineData(true, 2, "0 0 0:0 / /foo ignore ignore - cgroup2 cgroup2 ignore", "ignore", "/", "/foo")]
        [InlineData(true, 2, "0 0 0:0 / /foo ignore ignore - cgroup2 cgroup2 ignore", "memory", "/", "/foo")]
        [InlineData(true, 2, "0 0 0:0 / /foo ignore ignore - cgroup2 cgroup2 ignore", "cpu", "/", "/foo")]
        [InlineData(true, 2, "0 0 0:0 / /foo ignore - cgroup2 cgroup2 ignore", "cpu", "/", "/foo")]
        [InlineData(true, 2, "0 0 0:0 / /foo ignore ignore ignore - cgroup2 cgroup2 ignore", "cpu", "/", "/foo")]
        [InlineData(true, 2, "0 0 0:0 / /foo-with-dashes ignore ignore - cgroup2 cgroup2 ignore", "ignore", "/", "/foo-with-dashes")]
        [InlineData(true, 1, "0 0 0:0 / /foo ignore ignore - cgroup cgroup memory", "memory", "/", "/foo")]
        [InlineData(true, 1, "0 0 0:0 / /foo-with-dashes ignore ignore - cgroup cgroup memory", "memory", "/", "/foo-with-dashes")]
        [InlineData(true, 1, "0 0 0:0 / /foo ignore ignore - cgroup cgroup cpu,memory", "memory", "/", "/foo")]
        [InlineData(true, 1, "0 0 0:0 / /foo ignore ignore - cgroup cgroup memory,cpu", "memory", "/", "/foo")]
        public void ParseValidateMountInfo(bool expectedFound, int cgroupVersion, string procSelfMountInfoText, string subsystem, string expectedRoot, string expectedMount)
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, procSelfMountInfoText);

            Assert.Equal(expectedFound, Interop.cgroups.TryFindHierarchyMount((Interop.cgroups.CGroupVersion) cgroupVersion,
                                                                              path, subsystem, out string root, out string mount));
            if (expectedFound)
            {
                Assert.Equal(expectedRoot, root);
                Assert.Equal(expectedMount, mount);
            }
        }

        [Theory]
        [InlineData(true, 2, "0::/foo", "ignore", "/foo")]
        [InlineData(true, 2, "0::/bar", "ignore", "/bar")]
        [InlineData(true, 2, "0::frob", "ignore", "frob")]
        [InlineData(false, 1, "1::frob", "ignore", "ignore")]
        [InlineData(true, 1, "1:foo:bar", "foo", "bar")]
        [InlineData(true, 1, "0::baz\n1:foo:bar", "foo", "bar")]
        [InlineData(true, 1, "2:foo:bar", "foo", "bar")]
        [InlineData(false, 1, "2:foo:bar", "bar", "ignore")]
        [InlineData(true, 1, "1:foo:bar\n2:eggs:spam", "foo", "bar")]
        [InlineData(true, 1, "1:foo:bar\n2:eggs:spam", "eggs", "spam")]
        [InlineData(true, 1, "2:eggs:spam\n0:foo:bar", "eggs", "spam")]
        public void ParseValidateProcCGroup(bool expectedFound, int cgroupVersion, string procSelfCgroupText, string subsystem, string expectedMountPath)
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, procSelfCgroupText);

            Assert.Equal(expectedFound, Interop.cgroups.TryFindCGroupPathForSubsystem((Interop.cgroups.CGroupVersion) cgroupVersion,
                                                                                      path, subsystem, out string mountPath));
            if (expectedFound)
            {
                Assert.Equal(expectedMountPath, mountPath);
            }
        }
    }
}
