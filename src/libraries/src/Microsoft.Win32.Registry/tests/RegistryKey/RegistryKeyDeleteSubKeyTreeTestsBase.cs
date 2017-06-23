// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public abstract class RegistryKeyDeleteSubKeyTreeTestsBase : RegistryTestsBase
    {
        protected void Verify_DeleteSubKeyTree_KeyExists_KeyDeleted(string expected, Action deleteSubKeyTree)
        {
            CreateTestRegistrySubKey(expected);

            deleteSubKeyTree();
            Assert.Null(TestRegistryKey.OpenSubKey(expected));
        }

        protected void Verify_DeleteSubKeyTree_KeyDoesNotExists_Throws(string expected, Action deleteSubKeyTree)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(expected));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            AssertExtensions.Throws<ArgumentException>(null, () => deleteSubKeyTree());
        }

        protected void Verify_DeleteSubKeyTree_KeyDoesNotExists_DoesNotThrow(string expected, Action deleteSubKeyTree)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(expected));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            deleteSubKeyTree();
        }
    }
}
