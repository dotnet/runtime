// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetSubKeyCount : RegistryTestsBase
    {
        [Fact]
        public void ShoudThrowIfDisposed()
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
            Assert.NotNull(TestRegistryKey.CreateSubKey(TestRegistryKeyName));
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
                TestRegistryKey.CreateSubKey(subKey);
            }
            
            Assert.Equal(testSubKeys.Length, TestRegistryKey.SubKeyCount);
        }
    }
}