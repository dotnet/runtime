// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_Get_Name : RegistryTestsBase
    {
        private const string CurrentUserKeyName = "HKEY_CURRENT_USER";

        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                return TestRegistryKey.Name;
            });
        }

        [Fact]
        public static void TestBaseKeyName()
        {
            Assert.Equal(CurrentUserKeyName, Registry.CurrentUser.Name);
        }

        [Fact]
        public void TestSubKeyName()
        {
            string expectedName = string.Format("HKEY_CURRENT_USER\\{0}", TestRegistryKeyName);
            Assert.Equal(expectedName, TestRegistryKey.Name);
        }
    }
}
