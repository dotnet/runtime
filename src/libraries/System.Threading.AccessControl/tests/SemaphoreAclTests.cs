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
    public class SemaphoreAclTests : AclTests
    {
        private const int DefaultInitialCount = 0;
        private const int DefaultMaximumCount = 1;

        [Fact]
        public void Semaphore_Create_NullSecurity()
        {
            CreateAndVerifySemaphore(
                DefaultInitialCount,
                DefaultMaximumCount,
                name: GetRandomName(),
                expectedSecurity: null,
                expectedCreatedNew: true).Dispose();
        }

        [Theory]
        [InlineData(-1, 1)]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        public void Semaphore_Create_InvalidCounts(int initialCount, int maximumCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                CreateAndVerifySemaphore(
                    initialCount,
                    maximumCount,
                    name: GetRandomName(),
                    expectedSecurity: GetBasicSemaphoreSecurity(),
                    expectedCreatedNew: true).Dispose();
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Semaphore_Create_NameMultipleNew(string name)
        {
            SemaphoreSecurity security = GetBasicSemaphoreSecurity();
            bool expectedCreatedNew = true;

            using Semaphore semaphore1 = CreateAndVerifySemaphore(
                DefaultInitialCount,
                DefaultMaximumCount,
                name,
                security,
                expectedCreatedNew);

            using Semaphore semaphore2 = CreateAndVerifySemaphore(
                DefaultInitialCount,
                DefaultMaximumCount,
                name,
                security,
                expectedCreatedNew);
        }

        [Fact]
        public void Semaphore_Create_CreateNewExisting()
        {
            string name = GetRandomName();
            SemaphoreSecurity security = GetBasicSemaphoreSecurity();

            using Semaphore SemaphoreNew = CreateAndVerifySemaphore(
                DefaultInitialCount,
                DefaultMaximumCount,
                name,
                security,
                expectedCreatedNew: true);

            using Semaphore SemaphoreExisting = CreateAndVerifySemaphore(
                DefaultInitialCount,
                DefaultMaximumCount,
                name,
                security,
                expectedCreatedNew: false);
        }

        [Fact]
        public void Semaphore_Create_BeyondMaxPathLength()
        {
            // GetRandomName prevents name collision when two tests run at the same time
            string name = GetRandomName() + new string('x', Interop.Kernel32.MAX_PATH);

            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    CreateAndVerifySemaphore(
                        DefaultInitialCount,
                        DefaultMaximumCount,
                        name,
                        GetBasicSemaphoreSecurity(),
                        expectedCreatedNew: true).Dispose();
                });
            }
            else
            {
                using Semaphore created = CreateAndVerifySemaphore(
                    DefaultInitialCount,
                    DefaultMaximumCount,
                    name,
                    GetBasicSemaphoreSecurity(),
                    expectedCreatedNew: true);

                using Semaphore openedByName = Semaphore.OpenExisting(name);
                Assert.NotNull(openedByName);
            }
        }

        [Theory]
        [InlineData(SemaphoreRights.FullControl, AccessControlType.Allow)]
        [InlineData(SemaphoreRights.FullControl, AccessControlType.Deny)]
        [InlineData(SemaphoreRights.Synchronize, AccessControlType.Allow)]
        [InlineData(SemaphoreRights.Synchronize, AccessControlType.Deny)]
        [InlineData(SemaphoreRights.Modify, AccessControlType.Allow)]
        [InlineData(SemaphoreRights.Modify, AccessControlType.Deny)]
        [InlineData(SemaphoreRights.Modify | SemaphoreRights.Synchronize, AccessControlType.Allow)]
        [InlineData(SemaphoreRights.Modify | SemaphoreRights.Synchronize, AccessControlType.Deny)]
        public void Semaphore_Create_SpecificSecurity(SemaphoreRights rights, AccessControlType accessControl)
        {
            SemaphoreSecurity security = GetSemaphoreSecurity(WellKnownSidType.BuiltinUsersSid, rights, accessControl);

            CreateAndVerifySemaphore(
                DefaultInitialCount,
                DefaultMaximumCount,
                GetRandomName(),
                security,
                expectedCreatedNew: true).Dispose();

        }

        [Fact]
        public void Semaphore_OpenExisting()
        {
            string name = GetRandomName();
            SemaphoreSecurity expectedSecurity = GetSemaphoreSecurity(WellKnownSidType.BuiltinUsersSid, SemaphoreRights.FullControl, AccessControlType.Allow);
            using Semaphore semaphoreNew = CreateAndVerifySemaphore(initialCount: 1, maximumCount: 2, name, expectedSecurity, expectedCreatedNew: true);

            using Semaphore semaphoreExisting = SemaphoreAcl.OpenExisting(name, SemaphoreRights.FullControl);

            VerifyHandles(semaphoreNew, semaphoreExisting);
            SemaphoreSecurity actualSecurity = semaphoreExisting.GetAccessControl();
            VerifySemaphoreSecurity(expectedSecurity, actualSecurity);
        }

        [Fact]
        public void Semaphore_TryOpenExisting()
        {
            string name = GetRandomName();
            SemaphoreSecurity expectedSecurity = GetSemaphoreSecurity(WellKnownSidType.BuiltinUsersSid, SemaphoreRights.FullControl, AccessControlType.Allow);
            using Semaphore semaphoreNew = CreateAndVerifySemaphore(initialCount: 1, maximumCount: 2, name, expectedSecurity, expectedCreatedNew: true);

            Assert.True(SemaphoreAcl.TryOpenExisting(name, SemaphoreRights.FullControl, out Semaphore semaphoreExisting));
            Assert.NotNull(semaphoreExisting);

            VerifyHandles(semaphoreNew, semaphoreExisting);
            SemaphoreSecurity actualSecurity = semaphoreExisting.GetAccessControl();
            VerifySemaphoreSecurity(expectedSecurity, actualSecurity);

            semaphoreExisting.Dispose();
        }

        [Fact]
        public void Semaphore_OpenExisting_NameNotFound()
        {
            string name = "ThisShouldNotExist";
            Assert.Throws<WaitHandleCannotBeOpenedException>(() =>
            {
                SemaphoreAcl.OpenExisting(name, SemaphoreRights.FullControl).Dispose();
            });

            Assert.False(SemaphoreAcl.TryOpenExisting(name, SemaphoreRights.FullControl, out _));
        }

        [Fact]
        public void Semaphore_OpenExisting_NameInvalid()
        {
            string name = '\0'.ToString();
            Assert.Throws<WaitHandleCannotBeOpenedException>(() =>
            {
                SemaphoreAcl.OpenExisting(name, SemaphoreRights.FullControl).Dispose();
            });

            Assert.False(SemaphoreAcl.TryOpenExisting(name, SemaphoreRights.FullControl, out _));
        }

        [Fact]
        public void Semaphore_OpenExisting_PathNotFound()
        {
            string name = @"global\foo";
            Assert.Throws<IOException>(() =>
            {
                SemaphoreAcl.OpenExisting(name, SemaphoreRights.FullControl).Dispose();
            });

            Assert.False(SemaphoreAcl.TryOpenExisting(name, SemaphoreRights.FullControl, out _));
        }

        [Fact]
        public void Semaphore_OpenExisting_BadPathName()
        {
            string name = @"\\?\Path";
            Assert.Throws<IOException>(() =>
            {
                SemaphoreAcl.OpenExisting(name, SemaphoreRights.FullControl).Dispose();
            });

            Assert.Throws<IOException>(() =>
            {
                SemaphoreAcl.TryOpenExisting(name, SemaphoreRights.FullControl, out _);
            });
        }

        [Fact]
        public void Semaphore_OpenExisting_NullName()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SemaphoreAcl.OpenExisting(null, SemaphoreRights.FullControl).Dispose();
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                SemaphoreAcl.TryOpenExisting(null, SemaphoreRights.FullControl, out _);
            });
        }

        [Fact]
        public void Semaphore_OpenExisting_EmptyName()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                SemaphoreAcl.OpenExisting(string.Empty, SemaphoreRights.FullControl).Dispose();
            });

            Assert.Throws<ArgumentException>(() =>
            {
                SemaphoreAcl.TryOpenExisting(string.Empty, SemaphoreRights.FullControl, out _);
            });
        }

        private SemaphoreSecurity GetBasicSemaphoreSecurity()
        {
            return GetSemaphoreSecurity(
                WellKnownSidType.BuiltinUsersSid,
                SemaphoreRights.FullControl,
                AccessControlType.Allow);
        }

        private SemaphoreSecurity GetSemaphoreSecurity(WellKnownSidType sid, SemaphoreRights rights, AccessControlType accessControl)
        {
            SemaphoreSecurity security = new SemaphoreSecurity();
            SecurityIdentifier identity = new SecurityIdentifier(sid, null);
            SemaphoreAccessRule accessRule = new SemaphoreAccessRule(identity, rights, accessControl);
            security.AddAccessRule(accessRule);
            return security;
        }

        private Semaphore CreateAndVerifySemaphore(int initialCount, int maximumCount, string name, SemaphoreSecurity expectedSecurity, bool expectedCreatedNew)
        {
            Semaphore Semaphore = SemaphoreAcl.Create(initialCount, maximumCount, name, out bool createdNew, expectedSecurity);
            Assert.NotNull(Semaphore);
            Assert.Equal(createdNew, expectedCreatedNew);

            if (expectedSecurity != null)
            {
                SemaphoreSecurity actualSecurity = Semaphore.GetAccessControl();
                VerifySemaphoreSecurity(expectedSecurity, actualSecurity);
            }

            return Semaphore;
        }

        private void VerifyHandles(Semaphore expected, Semaphore actual)
        {
            Assert.NotNull(expected.SafeWaitHandle);
            Assert.NotNull(actual.SafeWaitHandle);

            Assert.False(expected.SafeWaitHandle.IsClosed);
            Assert.False(actual.SafeWaitHandle.IsClosed);

            Assert.False(expected.SafeWaitHandle.IsInvalid);
            Assert.False(actual.SafeWaitHandle.IsInvalid);
        }

        private void VerifySemaphoreSecurity(SemaphoreSecurity expectedSecurity, SemaphoreSecurity actualSecurity)
        {
            Assert.Equal(typeof(SemaphoreRights), expectedSecurity.AccessRightType);
            Assert.Equal(typeof(SemaphoreRights), actualSecurity.AccessRightType);

            List<SemaphoreAccessRule> expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<SemaphoreAccessRule>().ToList();

            List<SemaphoreAccessRule> actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<SemaphoreAccessRule>().ToList();

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

        private bool AreAccessRulesEqual(SemaphoreAccessRule expectedRule, SemaphoreAccessRule actualRule)
        {
            return
                expectedRule.AccessControlType == actualRule.AccessControlType &&
                expectedRule.SemaphoreRights   == actualRule.SemaphoreRights &&
                expectedRule.InheritanceFlags  == actualRule.InheritanceFlags &&
                expectedRule.PropagationFlags  == actualRule.PropagationFlags;
        }
    }
}
