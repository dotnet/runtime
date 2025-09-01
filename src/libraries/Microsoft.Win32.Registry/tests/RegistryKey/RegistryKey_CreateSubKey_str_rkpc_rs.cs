// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_CreateSubKey_str_rkpc_rs : RegistryKeyCreateSubKeyTestsBase
    {
        [Fact]
        public void CreateSubKeyWithRegistrySecurity()
        {
            var security = new RegistrySecurity();
            var identifier = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var accessRule = new RegistryAccessRule(identifier, RegistryRights.FullControl, AccessControlType.Allow);
            security.AddAccessRule(accessRule);

            using RegistryKey created = TestRegistryKey.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, security);
            Assert.NotNull(created);
            var actualSecurity = created.GetAccessControl();
            Assert.NotNull(actualSecurity);

            VerifyRegistrySecurity(security, actualSecurity);
        }
        
        [Fact]
        public void CreateSubKeyWithRegistryOptionsRegistrySecurity()
        {
            var security = new RegistrySecurity();
            var identifier = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var accessRule = new RegistryAccessRule(identifier, RegistryRights.FullControl, AccessControlType.Allow);
            security.AddAccessRule(accessRule);

            using RegistryKey created = TestRegistryKey.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.Volatile, security);
            Assert.NotNull(created);
            var actualSecurity = created.GetAccessControl();
            Assert.NotNull(actualSecurity);

            VerifyRegistrySecurity(security, actualSecurity);
        }

        private void VerifyRegistrySecurity(RegistrySecurity expectedSecurity, RegistrySecurity actualSecurity)
        {
            var expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<RegistryAccessRule>().ToList();

            var actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<RegistryAccessRule>().ToList();

            Assert.Equal(expectedAccessRules.Count, actualAccessRules.Count);
            if (expectedAccessRules.Count > 0)
            {
                Assert.All(expectedAccessRules, actualAccessRule =>
                {
                    int count = expectedAccessRules.Count(expectedAccessRule => AreAccessRulesEqual(expectedAccessRule, actualAccessRule));
                    Assert.True(count > 0);
                });
            }
        }

        private bool AreAccessRulesEqual(RegistryAccessRule expectedRule, RegistryAccessRule actualRule)
        {
            return
                expectedRule.AccessControlType == actualRule.AccessControlType &&
                expectedRule.RegistryRights  == actualRule.RegistryRights &&
                expectedRule.InheritanceFlags  == actualRule.InheritanceFlags &&
                expectedRule.PropagationFlags  == actualRule.PropagationFlags;
        }
    }
}
