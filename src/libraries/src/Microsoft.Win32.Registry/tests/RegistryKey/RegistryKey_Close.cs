// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_Close : RegistryTestsBase
    {
        [Fact]
        public void CloseRegistryKeyTest()
        {
            TestRegistryKey.Close();

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() => TestRegistryKey.DeleteSubKey(TestRegistryKeyName));
            Assert.Throws<ObjectDisposedException>(() => TestRegistryKey.CreateSubKey(TestRegistryKeyName));
            Assert.Throws<ObjectDisposedException>(() => TestRegistryKey.SetValue(string.Empty, "String"));
        }
    }
}
