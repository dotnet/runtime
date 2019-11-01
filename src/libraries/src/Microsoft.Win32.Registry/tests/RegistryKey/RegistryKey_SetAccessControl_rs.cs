// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.AccessControl;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_SetAccessControl_rs : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed registrySecurity is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.SetAccessControl(null));

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Close();
                TestRegistryKey.SetAccessControl(new RegistrySecurity());
            });
        }

        [Fact]
        public void SetAccessControlTest()
        {
            TestRegistryKey.SetAccessControl(TestRegistryKey.GetAccessControl());
        }
    }
}
