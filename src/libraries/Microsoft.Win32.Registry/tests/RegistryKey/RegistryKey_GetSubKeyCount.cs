// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetSubKeyCount : RegistryTestsBase
    {
        [Fact]
        public void ShouldThrowIfDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                return TestRegistryKey.SubKeyCount;
            });
        }

        [Fact]
        public void ShouldThrowIfRegistryKeyDeleted()
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestRegistryKeyName);
            Assert.Throws<IOException>(() => TestRegistryKey.SubKeyCount);
        }

        [Fact]
        public void SubKeyCountTest()
        {
            // [] Creating new SubKeys and get count

            Assert.Equal(expected: 0, actual: TestRegistryKey.SubKeyCount);

            using RegistryKey subkey = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
            Assert.NotNull(subkey);
            Assert.Equal(expected: 1, actual: TestRegistryKey.SubKeyCount);

            TestRegistryKey.DeleteSubKey(TestRegistryKeyName);
            Assert.Equal(expected: 0, actual: TestRegistryKey.SubKeyCount);
        }

        [Fact]
        public void SubKeyCountTest2()
        {
            // [] Add multiple keys and test for SubKeyCount
            string[] testSubKeys = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();
            foreach (var subKey in testSubKeys)
            {
                TestRegistryKey.CreateSubKey(subKey).Dispose();
            }

            Assert.Equal(testSubKeys.Length, TestRegistryKey.SubKeyCount);
        }
    }
}
