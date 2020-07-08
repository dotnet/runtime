// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.Threading.Tests
{
    public class MutexAclTests : AclTests
    {
        [Fact]
        public void Mutex_Create_NullSecurity()
        {
            CreateAndVerifyMutex(initiallyOwned: true, GetRandomName(), expectedSecurity: null, expectedCreatedNew: true).Dispose();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Mutex_Create_NameMultipleNew(string name)
        {
            MutexSecurity security = GetBasicMutexSecurity();

            using Mutex mutex1 = CreateAndVerifyMutex(initiallyOwned: true, name, security, expectedCreatedNew: true);
            using Mutex mutex2 = CreateAndVerifyMutex(initiallyOwned: true, name, security, expectedCreatedNew: true);
        }

        [Fact]
        public void Mutex_Create_CreateNewExisting()
        {
            string name = GetRandomName();
            MutexSecurity security = GetBasicMutexSecurity();

            using Mutex mutexNew      = CreateAndVerifyMutex(initiallyOwned: true, name, security, expectedCreatedNew: true);
            using Mutex mutexExisting = CreateAndVerifyMutex(initiallyOwned: true, name, security, expectedCreatedNew: false);
        }

        [Fact]
        public void Mutex_Create_BeyondMaxPathLength()
        {
            // GetRandomName prevents name collision when two tests run at the same time
            string name = GetRandomName() + new string('x', Interop.Kernel32.MAX_PATH);

            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    CreateAndVerifyMutex(initiallyOwned: true, name, GetBasicMutexSecurity(), expectedCreatedNew: true).Dispose();
                });
            }
            else
            {
                using Mutex created = CreateAndVerifyMutex(initiallyOwned: true, name, GetBasicMutexSecurity(), expectedCreatedNew: true);
                using Mutex openedByName = Mutex.OpenExisting(name);
                Assert.NotNull(openedByName);
            }
        }

        [Theory]
        [InlineData(true, MutexRights.FullControl, AccessControlType.Allow)]
        [InlineData(true, MutexRights.FullControl, AccessControlType.Deny)]
        [InlineData(true, MutexRights.Synchronize, AccessControlType.Allow)]
        [InlineData(true, MutexRights.Synchronize, AccessControlType.Deny)]
        [InlineData(true, MutexRights.Modify, AccessControlType.Allow)]
        [InlineData(true, MutexRights.Modify, AccessControlType.Deny)]
        [InlineData(true, MutexRights.Modify | MutexRights.Synchronize, AccessControlType.Allow)]
        [InlineData(true, MutexRights.Modify | MutexRights.Synchronize, AccessControlType.Deny)]
        [InlineData(false, MutexRights.FullControl, AccessControlType.Allow)]
        [InlineData(false, MutexRights.FullControl, AccessControlType.Deny)]
        [InlineData(false, MutexRights.Synchronize, AccessControlType.Allow)]
        [InlineData(false, MutexRights.Synchronize, AccessControlType.Deny)]
        [InlineData(false, MutexRights.Modify, AccessControlType.Allow)]
        [InlineData(false, MutexRights.Modify, AccessControlType.Deny)]
        public void Mutex_Create_SpecificParameters(bool initiallyOwned, MutexRights rights, AccessControlType accessControl)
        {
            MutexSecurity security = GetMutexSecurity(WellKnownSidType.BuiltinUsersSid, rights, accessControl);
            CreateAndVerifyMutex(initiallyOwned, GetRandomName(), security, expectedCreatedNew: true).Dispose();

        }

        private MutexSecurity GetBasicMutexSecurity()
        {
            return GetMutexSecurity(
                WellKnownSidType.BuiltinUsersSid,
                MutexRights.FullControl,
                AccessControlType.Allow);
        }

        private MutexSecurity GetMutexSecurity(WellKnownSidType sid, MutexRights rights, AccessControlType accessControl)
        {
            MutexSecurity security = new MutexSecurity();
            SecurityIdentifier identity = new SecurityIdentifier(sid, null);
            MutexAccessRule accessRule = new MutexAccessRule(identity, rights, accessControl);
            security.AddAccessRule(accessRule);
            return security;
        }

        private Mutex CreateAndVerifyMutex(bool initiallyOwned, string name, MutexSecurity expectedSecurity, bool expectedCreatedNew)
        {
            Mutex mutex = MutexAcl.Create(initiallyOwned, name, out bool createdNew, expectedSecurity);
            Assert.NotNull(mutex);
            Assert.Equal(createdNew, expectedCreatedNew);

            if (expectedSecurity != null)
            {
                MutexSecurity actualSecurity = mutex.GetAccessControl();
                VerifyMutexSecurity(expectedSecurity, actualSecurity);
            }

            return mutex;
        }

        private void VerifyMutexSecurity(MutexSecurity expectedSecurity, MutexSecurity actualSecurity)
        {
            Assert.Equal(typeof(MutexRights), expectedSecurity.AccessRightType);
            Assert.Equal(typeof(MutexRights), actualSecurity.AccessRightType);

            List<MutexAccessRule> expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<MutexAccessRule>().ToList();

            List<MutexAccessRule> actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<MutexAccessRule>().ToList();

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

        private bool AreAccessRulesEqual(MutexAccessRule expectedRule, MutexAccessRule actualRule)
        {
            return
                expectedRule.AccessControlType == actualRule.AccessControlType &&
                expectedRule.MutexRights       == actualRule.MutexRights &&
                expectedRule.InheritanceFlags  == actualRule.InheritanceFlags &&
                expectedRule.PropagationFlags  == actualRule.PropagationFlags;
        }
    }
}
