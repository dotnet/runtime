// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
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

        public static IEnumerable<object[]> GetMutexSpecificParameters() =>
            from initiallyOwned in new[] { false, true }
            from rights in new[] { MutexRights.FullControl, MutexRights.Synchronize, MutexRights.Modify, MutexRights.Modify | MutexRights.Synchronize }
            from accessControl in new[] { AccessControlType.Allow, AccessControlType.Deny }
            select new object[] { initiallyOwned, rights, accessControl };

        [Theory]
        [MemberData(nameof(GetMutexSpecificParameters))]
        public void Mutex_Create_SpecificParameters(bool initiallyOwned, MutexRights rights, AccessControlType accessControl)
        {
            MutexSecurity security = GetMutexSecurity(WellKnownSidType.BuiltinUsersSid, rights, accessControl);
            CreateAndVerifyMutex(initiallyOwned, GetRandomName(), security, expectedCreatedNew: true).Dispose();
        }

        [Fact]
        public void Mutex_OpenExisting()
        {
            string name = GetRandomName();
            MutexSecurity expectedSecurity = GetMutexSecurity(WellKnownSidType.BuiltinUsersSid, MutexRights.FullControl, AccessControlType.Allow);
            using Mutex mutexNew = CreateAndVerifyMutex(initiallyOwned: true, name, expectedSecurity, expectedCreatedNew: true);

            using Mutex mutexExisting = MutexAcl.OpenExisting(name, MutexRights.FullControl);

            VerifyHandles(mutexNew, mutexExisting);
            MutexSecurity actualSecurity = mutexExisting.GetAccessControl();
            VerifyMutexSecurity(expectedSecurity, actualSecurity);
        }

        [Fact]
        public void Mutex_TryOpenExisting()
        {
            string name = GetRandomName();
            MutexSecurity expectedSecurity = GetMutexSecurity(WellKnownSidType.BuiltinUsersSid, MutexRights.FullControl, AccessControlType.Allow);
            using Mutex mutexNew = CreateAndVerifyMutex(initiallyOwned: true, name, expectedSecurity, expectedCreatedNew: true);

            Assert.True(MutexAcl.TryOpenExisting(name, MutexRights.FullControl, out Mutex mutexExisting));
            Assert.NotNull(mutexExisting);

            VerifyHandles(mutexNew, mutexExisting);
            MutexSecurity actualSecurity = mutexExisting.GetAccessControl();
            VerifyMutexSecurity(expectedSecurity, actualSecurity);

            mutexExisting.Dispose();
        }

        [Fact]
        public void Mutex_OpenExisting_NameNotFound()
        {
            string name = "ThisShouldNotExist";
            Assert.Throws<WaitHandleCannotBeOpenedException>(() =>
            {
                MutexAcl.OpenExisting(name, MutexRights.FullControl).Dispose();
            });

            Assert.False(MutexAcl.TryOpenExisting(name, MutexRights.FullControl, out _));
        }

        [Fact]
        public void Mutex_OpenExisting_NameInvalid()
        {
            string name = '\0'.ToString();
            Assert.Throws<WaitHandleCannotBeOpenedException>(() =>
            {
                MutexAcl.OpenExisting(name, MutexRights.FullControl).Dispose();
            });

            Assert.False(MutexAcl.TryOpenExisting(name, MutexRights.FullControl, out _));
        }


        [Fact]
        public void Mutex_OpenExisting_PathNotFound()
        {
            string name = @"global\foo";
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                MutexAcl.OpenExisting(name, MutexRights.FullControl).Dispose();
            });

            Assert.False(MutexAcl.TryOpenExisting(name, MutexRights.FullControl, out _));
        }

        [Fact]
        public void Mutex_OpenExisting_BadPathName()
        {
            string name = @"\\?\Path";
            Assert.Throws<System.IO.IOException>(() =>
            {
                MutexAcl.OpenExisting(name, MutexRights.FullControl).Dispose();
            });
            Assert.Throws<System.IO.IOException>(() =>
            {
                MutexAcl.TryOpenExisting(name, MutexRights.FullControl, out _);
            });
        }

        [Fact]
        public void Mutex_OpenExisting_NullName()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MutexAcl.OpenExisting(null, MutexRights.FullControl).Dispose();
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                MutexAcl.TryOpenExisting(null, MutexRights.FullControl, out _);
            });
        }

        [Fact]
        public void Mutex_OpenExisting_EmptyName()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                MutexAcl.OpenExisting(string.Empty, MutexRights.FullControl).Dispose();
            });

            Assert.Throws<ArgumentException>(() =>
            {
                MutexAcl.TryOpenExisting(string.Empty, MutexRights.FullControl, out _);
            });
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

        private void VerifyHandles(Mutex expected, Mutex actual)
        {
            Assert.NotNull(expected.SafeWaitHandle);
            Assert.NotNull(actual.SafeWaitHandle);

            Assert.False(expected.SafeWaitHandle.IsClosed);
            Assert.False(actual.SafeWaitHandle.IsClosed);

            Assert.False(expected.SafeWaitHandle.IsInvalid);
            Assert.False(actual.SafeWaitHandle.IsInvalid);
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
