// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public abstract class RegistryKeyDeleteSubKeyTreeTestsBase : RegistryTestsBase
    {
        protected void Verify_DeleteSubKeyTree_KeyExists_KeyDeleted(Action deleteSubKeyTree)
        {
            CreateTestRegistrySubKey();

            deleteSubKeyTree();
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
        }

        protected void Verify_DeleteSubKeyTree_KeyDoesNotExists_Throws(Action deleteSubKeyTree)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistrySubKeyName));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            Assert.Throws<ArgumentException>(() => deleteSubKeyTree());
        }

        protected void Verify_DeleteSubKeyTree_KeyDoesNotExists_DoesNotThrow(Action deleteSubKeyTree)
        {
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistrySubKeyName));
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            deleteSubKeyTree();
        }
    }
}
