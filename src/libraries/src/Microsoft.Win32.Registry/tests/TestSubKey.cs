// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class TestSubKey : IDisposable
    {
        private readonly string TestKey;
        protected readonly RegistryKey _testRegistryKey;

        public TestSubKey(string testKeyName)
        {
            TestKey = testKeyName;

            var rk = Registry.CurrentUser;
            // If subkey already exists then we should fail because it could be used by someone
            Assert.Null(rk.OpenSubKey(TestKey));

            //created the test key. if that failed it will cause many of the test scenarios below fail.
            _testRegistryKey = rk.CreateSubKey(TestKey);
            Assert.NotNull(_testRegistryKey);
        }

        public void Dispose()
        {
            _testRegistryKey.Dispose();

            var rk = Registry.CurrentUser;
            if (rk.OpenSubKey(TestKey) != null)
                rk.DeleteSubKeyTree(TestKey);
        }
    }
}
