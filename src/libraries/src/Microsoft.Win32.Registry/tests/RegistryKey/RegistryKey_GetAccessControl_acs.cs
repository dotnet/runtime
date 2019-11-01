// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.AccessControl;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetAccessControl_acs : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Close();
                TestRegistryKey.GetAccessControl(new AccessControlSections());
            });
        }

        [Fact]
        public void GetAccessControlTest()
        {
            var registrySecurity = TestRegistryKey.GetAccessControl(new AccessControlSections());
            Assert.NotNull(registrySecurity);
        }
    }
}
