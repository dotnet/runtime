// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public abstract class RegistryKeyOpenSubKeyTestsBase : RegistryTestsBase
    {
        protected void Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(Func<RegistryKey> openSubKey)
        {
            CreateTestRegistrySubKey();

            using (RegistryKey key = openSubKey())
            {
                Assert.NotNull(key);
                Assert.Equal(1, TestRegistryKey.SubKeyCount);
                Assert.Equal(TestRegistrySubKeyFullName, key.Name);
            }
        }

        protected void Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(Func<RegistryKey> openSubKey)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistrySubKeyName));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            Assert.Null(openSubKey());
        }
    }
}
