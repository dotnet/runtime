// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
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
                using RegistryKey created = rk.CreateSubKey(TestRegistryKeyName);
                rk.DeleteSubKeyTree("");
            }

            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }
        
        [Fact]
        public void SelfDeleteWithValuesTest()
        {
            using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
            {
                rk.SetValue("VAL", "Dummy", RegistryValueKind.String);
                rk.SetDefaultValue("Default");
                using RegistryKey created = rk.CreateSubKey(TestRegistryKeyName);
                created.SetValue("Value", 42, RegistryValueKind.DWord);
                rk.DeleteSubKeyTree("");
            }

            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }
        
        [Fact]
        public void SelfDeleteWithValuesTest_AnotherHandlePresent()
        {
            using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
            {
                rk.SetValue("VAL", "Dummy", RegistryValueKind.String);
                rk.SetDefaultValue("Default");
                using RegistryKey created = rk.CreateSubKey(TestRegistryKeyName);
                created.SetValue("Value", 42, RegistryValueKind.DWord);

                using var rk2 = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
                rk.DeleteSubKeyTree("");
            }

            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }

        [Fact]
        public void DeleteSubKeyTreeTest()
        {
            // Creating new SubKey and deleting it
            using RegistryKey created = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
            using RegistryKey opened = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
            Assert.NotNull(opened);

            TestRegistryKey.DeleteSubKeyTree(TestRegistryKeyName);
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }

        [Fact]
        public void DeleteSubKeyTreeTest2()
        {
            // [] Add in multiple subkeys and then delete the root key
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();

            using (RegistryKey rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
            {
                foreach (var subKeyName in subKeyNames)
                {
                    using RegistryKey rk2 = rk.CreateSubKey(subKeyName);
                    Assert.NotNull(rk2);

                    using RegistryKey rk3 = rk2.CreateSubKey("Test");
                    Assert.NotNull(rk3);
                }

                Assert.Equal(subKeyNames, rk.GetSubKeyNames());
            }

            TestRegistryKey.DeleteSubKeyTree(TestRegistryKeyName);
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }
        
        [Fact]
        public void DeleteSubKeyTreeTest3()
        {
            // [] Add in multiple subkeys and then delete the root key
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();

            using (RegistryKey rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName))
            {
                foreach (var subKeyName in subKeyNames)
                {
                    using RegistryKey rk2 = rk.CreateSubKey(subKeyName);
                    Assert.NotNull(rk2);

                    using RegistryKey rk3 = rk2.CreateSubKey("Test");
                    Assert.NotNull(rk3);
                }

                Assert.Equal(subKeyNames, rk.GetSubKeyNames());

                // Add multiple values to the key being deleted
                foreach (int i in Enumerable.Range(1, 9))
                {
                    rk.SetValue("STRVAL_" + i, i.ToString(), RegistryValueKind.String);
                    rk.SetValue("INTVAL_" + i, i, RegistryValueKind.DWord);
                }
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
        public void DeleteSubKeyTree_KeyDoesNotExists_Throws(string expected, string subKeyName) =>
            Verify_DeleteSubKeyTree_KeyDoesNotExists_Throws(expected, () => TestRegistryKey.DeleteSubKeyTree(subKeyName));
    }
}
