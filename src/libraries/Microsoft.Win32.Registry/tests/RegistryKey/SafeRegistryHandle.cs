// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class SafeRegistryHandleTests : IDisposable
    {
        [Fact]
        public void TestNullHandle()
        {
            //null handle
            Action a = () =>
            {
                SafeRegistryHandle handle = null;
                RegistryKey rk2 = RegistryKey.FromHandle(handle, RegistryView.Default);
            };

            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void TestInvalidView()
        {
            //View is -1
            Action a = () =>
            {
                RegistryView view = (RegistryView)(-1);
                RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);
                rk = rk.CreateSubKey("TestKeyThatExists");
                SafeRegistryHandle handle = rk.Handle;
                RegistryKey rk2 = RegistryKey.FromHandle(handle, view);
            };

            Assert.Throws<ArgumentException>(() => { a(); });

            //View is 3
            Action a1 = () =>
            {
                RegistryView view = (RegistryView)3;
                RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);
                rk = rk.CreateSubKey("TestKeyThatExists");
                SafeRegistryHandle handle = rk.Handle;
                RegistryKey rk2 = RegistryKey.FromHandle(handle, view);
            };

            Assert.Throws<ArgumentException>(() => { a1(); });
        }

        [Fact]
        public void TestDisposedRegistryKey()
        {
            //Try getting handle on disposed key.
            Action a1 = () =>
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);
                rk = rk.CreateSubKey("TestKeyThatExists");
                rk.Dispose();
                SafeRegistryHandle handle = rk.Handle;
            };

            Assert.Throws<ObjectDisposedException>(() => { a1(); });
        }

        [Fact]
        public void TestDeletedRegistryKey()
        {
            //Try getting registrykey from handle for key that has been deleted.
            Action a1 = () =>
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);
                RegistryKey rk2 = rk.CreateSubKey("TestKeyThatWillBeDeleted");
                SafeRegistryHandle handle = rk2.Handle;
                rk.DeleteSubKey("TestKeyThatWillBeDeleted");
                RegistryKey rk3 = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk3.CreateSubKey("TestThrows");
            };

            Assert.Throws<IOException>(() => { a1(); });

            //Try getting handle on deleted key.
            Action a2 = () =>
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);
                RegistryKey rk2 = rk.CreateSubKey("TestKeyThatWillBeDeleted");
                rk.DeleteSubKey("TestKeyThatWillBeDeleted");
                SafeRegistryHandle handle = rk2.Handle;
                RegistryKey rk3 = RegistryKey.FromHandle(handle, RegistryView.Default);
                rk3.CreateSubKey("TestThrows");
            };

            Assert.Throws<IOException>(() => { a2(); });
        }

        public void Dispose()
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);
            if (rk.OpenSubKey("TestKeyThatExists") != null)
                rk.DeleteSubKeyTree("TestKeyThatExists");
            if (rk.OpenSubKey("TestKeyThatWillBeDeleted") != null)
                rk.DeleteSubKeyTree("TestKeyThatWillBeDeleted");
            if (rk.OpenSubKey("TestThrows") != null)
                rk.DeleteSubKeyTree("TestThrows");
        }
    }
}