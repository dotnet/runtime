// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class SafeRegistryHandleTests : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // null handle
            Assert.Throws<ArgumentNullException>(() => RegistryKey.FromHandle(handle: null, view: RegistryView.Default));

            // invalid view
            AssertExtensions.Throws<ArgumentException>("view", () => RegistryKey.FromHandle(TestRegistryKey.Handle, (RegistryView)(-1)));
            AssertExtensions.Throws<ArgumentException>("view", () => RegistryKey.FromHandle(TestRegistryKey.Handle, (RegistryView)3));

            // get handle of disposed RegistryKey
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                return TestRegistryKey.Handle;
            });

        }

        [Fact]
        public void TestDeletedRegistryKey()
        {
            const string subKeyName = "TestKeyThatWillBeDeleted";
            //Try getting registrykey from handle for key that has been deleted.
            Assert.Throws<IOException>(() =>
            {
                using RegistryKey rk = TestRegistryKey.CreateSubKey(subKeyName);
                SafeRegistryHandle handle = rk.Handle;
                TestRegistryKey.DeleteSubKey(subKeyName);

                using RegistryKey rk2 = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk2.CreateSubKey("TestThrows").Dispose();
            });

            //Try getting handle on deleted key.
            Assert.Throws<IOException>(() =>
            {
                using RegistryKey rk = TestRegistryKey.CreateSubKey(subKeyName);
                TestRegistryKey.DeleteSubKey(subKeyName);
                SafeRegistryHandle handle = rk.Handle;

                using RegistryKey rk2 = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk2.CreateSubKey("TestThrows");
            });
        }
    }
}
