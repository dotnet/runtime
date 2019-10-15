// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKeyTree_str : RegistryKeyDeleteSubKeyTreeTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKeyTree(null));

            // Should throw if target subkey is system subkey and name is empty
            AssertExtensions.Throws<ArgumentException>(null, () => Registry.CurrentUser.DeleteSubKeyTree(string.Empty));

            // Should throw because subkey doesn't exists
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.DeleteSubKeyTree(name));

            // Should throw because RegistryKey is readonly
            using (var rk = TestRegistryKey.OpenSubKey(string.Empty, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.DeleteSubKeyTree(name);
            });
        }

        [Fact]
        public void SelfDeleteTest()
        {
            using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
            {
                rk.CreateSubKey(TestRegistryKeyName);
                rk.DeleteSubKeyTree("");
            }

            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }

        [Fact]
        public void DeleteSubKeyTreeTest()
        {
            // Creating new SubKey and deleting it
            TestRegistryKey.CreateSubKey(TestRegistryKeyName);
            Assert.NotNull(TestRegistryKey.OpenSubKey(TestRegistryKeyName));

            TestRegistryKey.DeleteSubKeyTree(TestRegistryKeyName);
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }

        [Fact]
        public void DeleteSubKeyTreeTest2()
        {
            // [] Add in multiple subkeys and then delete the root key
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();

            using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
            {
                foreach (var subKeyName in subKeyNames)
                {
                    var rk2 = rk.CreateSubKey(subKeyName);
                    Assert.NotNull(rk2);
                    Assert.NotNull(rk2.CreateSubKey("Test"));
                }

                Assert.Equal(subKeyNames, rk.GetSubKeyNames());
            }

            TestRegistryKey.DeleteSubKeyTree(TestRegistryKeyName);
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKeyTree_KeyExists_KeyDeleted(string expected, string subKeyName) =>
            Verify_DeleteSubKeyTree_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteSubKeyTree(subKeyName));


        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void Verify_DeleteSubKeyTree_KeyDoesNotExists_Throws(string expected, string subKeyName) =>
            Verify_DeleteSubKeyTree_KeyDoesNotExists_Throws(expected, () => TestRegistryKey.DeleteSubKeyTree(subKeyName));
    }
}
