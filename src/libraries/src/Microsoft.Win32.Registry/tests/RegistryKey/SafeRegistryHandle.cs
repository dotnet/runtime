// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Assert.Throws<ArgumentException>(() => RegistryKey.FromHandle(TestRegistryKey.Handle, (RegistryView)(-1)));
            Assert.Throws<ArgumentException>(() => RegistryKey.FromHandle(TestRegistryKey.Handle, (RegistryView)3));

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
                RegistryKey rk = TestRegistryKey.CreateSubKey(subKeyName);
                SafeRegistryHandle handle = rk.Handle;
                TestRegistryKey.DeleteSubKey(subKeyName);
                rk = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk.CreateSubKey("TestThrows");
            });

            //Try getting handle on deleted key.
            Assert.Throws<IOException>(() =>
            {
                RegistryKey rk = TestRegistryKey.CreateSubKey(subKeyName);
                TestRegistryKey.DeleteSubKey(subKeyName);
                SafeRegistryHandle handle = rk.Handle;
                rk = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk.CreateSubKey("TestThrows");
            });
        }
    }
}
