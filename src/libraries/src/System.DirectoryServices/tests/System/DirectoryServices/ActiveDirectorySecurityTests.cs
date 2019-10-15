// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "DirectoryObjectSecurity is not supported.")]
    public class ActiveDirectorySecurityTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var security = new ActiveDirectorySecurity();
            Assert.Equal(typeof(ActiveDirectoryRights), security.AccessRightType);
            Assert.Equal(typeof(ActiveDirectoryAccessRule), security.AccessRuleType);
            Assert.Equal(typeof(ActiveDirectoryAuditRule), security.AuditRuleType);
        }
    }
}
