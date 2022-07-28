// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetSubKeyNames : RegistryTestsBase
    {
        [Fact]
        public void ShouldThrowIfDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.GetSubKeyNames();
            });
        }

        [Fact]
        public void ShouldThrowIfRegistryKeyDeleted()
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestRegistryKeyName);
            Assert.Throws<IOException>(() => TestRegistryKey.GetSubKeyNames());
        }

        [Fact]
        public void GetSubKeyNamesTest()
        {
            // [] Creating new SubKeys and get the names
            string[] expectedSubKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();
            foreach (var subKeyName in expectedSubKeyNames)
            {
                TestRegistryKey.CreateSubKey(subKeyName).Dispose();
            }

            Assert.Equal(expectedSubKeyNames, TestRegistryKey.GetSubKeyNames());
        }

        [Fact]
        public void GetSubKeyNamesTest2()
        {
            // [] Check that zero length array is returned for no subkeys
            Assert.Empty(TestRegistryKey.GetSubKeyNames());
        }
    }
}
