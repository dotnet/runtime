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
                _testRegistryKey.Dispose();
                return _testRegistryKey.SubKeyCount;
            });
        }

        [Fact]
        public void ShouldThrowIfRegistryKeyDeleted()
        {
            Registry.CurrentUser.DeleteSubKeyTree(_testRegistryKeyName);
            Assert.Throws<IOException>(() => _testRegistryKey.SubKeyCount);
        }

        [Fact]
        public void SubKeyCountTest()
        {
            // [] Creating new SubKeys and get count

            Assert.Equal(expected: 0, actual: _testRegistryKey.SubKeyCount);
            Assert.NotNull(_testRegistryKey.CreateSubKey(_testRegistryKeyName));
            Assert.Equal(expected: 1, actual: _testRegistryKey.SubKeyCount);
            
            _testRegistryKey.DeleteSubKey(_testRegistryKeyName);
            Assert.Equal(expected: 0, actual: _testRegistryKey.SubKeyCount);
        }

        [Fact]
        public void SubKeyCountTest2()
        {
            // [] Add multiple keys and test for SubKeyCount
            string[] testSubKeys = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();
            foreach (var subKey in testSubKeys)
            {
                _testRegistryKey.CreateSubKey(subKey);
            }
            
            Assert.Equal(testSubKeys.Length, _testRegistryKey.SubKeyCount);
        }
    }
}