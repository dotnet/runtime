// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public abstract class RegistryKeyDeleteSubKeyTestsBase : RegistryTestsBase
    {
        protected void Verify_DeleteSubKey_KeyExists_KeyDeleted(string expected, Action deleteSubKey)
        {
            CreateTestRegistrySubKey(expected);

            deleteSubKey();
            Assert.Null(TestRegistryKey.OpenSubKey(expected));
        }

        protected void Verify_DeleteSubKey_KeyDoesNotExists_Throws(string expected, Action deleteSubKey)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(expected));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            AssertExtensions.Throws<ArgumentException>(null, () => deleteSubKey());
        }

        protected void Verify_DeleteSubKey_KeyDoesNotExists_DoesNotThrow(string expected, Action deleteSubKey)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(expected));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            deleteSubKey();
        }
    }
}
