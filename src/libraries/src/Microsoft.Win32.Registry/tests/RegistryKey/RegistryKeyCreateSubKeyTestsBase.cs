// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public abstract class RegistryKeyCreateSubKeyTestsBase : RegistryTestsBase
    {
        protected void Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(string expected, Func<RegistryKey> createSubKey)
        {
            CreateTestRegistrySubKey(expected);

            using (RegistryKey key = createSubKey())
            {
                Assert.NotNull(key);
                Assert.Equal(1, TestRegistryKey.SubKeyCount);
                Assert.Equal(TestRegistryKey.Name + @"\" + expected, key.Name);
            }
        }

        protected void Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, Func<RegistryKey> createSubKey)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(expected));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            using (RegistryKey key = createSubKey())
            {
                Assert.NotNull(key);
                Assert.Equal(1, TestRegistryKey.SubKeyCount);
                Assert.Equal(TestRegistryKey.Name + @"\" + expected, key.Name);
            }
        }
    }
}
