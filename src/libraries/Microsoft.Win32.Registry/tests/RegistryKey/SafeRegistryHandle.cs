// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class SafeRegistryHandleTests : TestSubKey
    {
        private const string TestKey = "BCL_TEST_44";

        public SafeRegistryHandleTests()
            : base(TestKey)
        {
        }

        [Fact]
        public void NegativeTests()
        {
            // null handle
            Assert.Throws<ArgumentNullException>(() => RegistryKey.FromHandle(handle: null, view: RegistryView.Default));

            // invalid view
            Assert.Throws<ArgumentException>(() => RegistryKey.FromHandle(_testRegistryKey.Handle, (RegistryView)(-1)));
            Assert.Throws<ArgumentException>(() => RegistryKey.FromHandle(_testRegistryKey.Handle, (RegistryView)3));

            // get handle of disposed RegistryKey
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                return _testRegistryKey.Handle;
            });

        }

        [Fact]
        public void TestDeletedRegistryKey()
        {
            const string subKeyName = "TestKeyThatWillBeDeleted";
            //Try getting registrykey from handle for key that has been deleted.
            Assert.Throws<IOException>(() =>
            {
                RegistryKey rk = _testRegistryKey.CreateSubKey(subKeyName);
                SafeRegistryHandle handle = rk.Handle;
                _testRegistryKey.DeleteSubKey(subKeyName);
                rk = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk.CreateSubKey("TestThrows");
            });

            //Try getting handle on deleted key.
            Assert.Throws<IOException>(() =>
            {
                RegistryKey rk = _testRegistryKey.CreateSubKey(subKeyName);
                _testRegistryKey.DeleteSubKey(subKeyName);
                SafeRegistryHandle handle = rk.Handle;
                rk = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk.CreateSubKey("TestThrows");
            });
        }
    }
}
