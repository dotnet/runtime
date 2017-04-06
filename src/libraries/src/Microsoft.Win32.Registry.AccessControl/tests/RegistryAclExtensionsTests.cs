// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32
{
    public class RegistryAclExtensionsTests
    {
        [Fact]
        public void GetAccessControl_NullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RegistryAclExtensions.GetAccessControl(null));
            Assert.Throws<ArgumentNullException>(() => RegistryAclExtensions.GetAccessControl(null, System.Security.AccessControl.AccessControlSections.All));
        }

        [Fact]
        public void SetAccessControl_NullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RegistryAclExtensions.SetAccessControl(null, new System.Security.AccessControl.RegistrySecurity()));
        }
    }
}
