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
    public class EventWaitHandleAclTests : AclTests
    {
        [Fact]
        public void EventWaitHandle_Create_NullSecurity()
        {
            CreateAndVerifyEventWaitHandle(
                initialState: true,
                mode: EventResetMode.AutoReset,
                name: GetRandomName(),
                expectedSecurity: null,
                expectedCreatedNew: true).Dispose();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EventWaitHandle_Create_NameMultipleNew(string name)
        {
            EventWaitHandleSecurity security = GetBasicEventWaitHandleSecurity();

            using EventWaitHandle handle1 = CreateAndVerifyEventWaitHandle(
                initialState: true,
                mode: EventResetMode.AutoReset,
                name,
                security,
                expectedCreatedNew: true);

            using EventWaitHandle handle2 = CreateAndVerifyEventWaitHandle(
                initialState: true,
                mode: EventResetMode.AutoReset,
                name,
                security,
                expectedCreatedNew: true);
        }

        [Fact]
        public void EventWaitHandle_Create_CreateNewExisting()
        {
            string name = GetRandomName();
            EventWaitHandleSecurity security = GetBasicEventWaitHandleSecurity();

            using EventWaitHandle handle1 = CreateAndVerifyEventWaitHandle(
                initialState: true,
                mode: EventResetMode.AutoReset,
                name,
                security,
                expectedCreatedNew: true);

            using EventWaitHandle handle2 = CreateAndVerifyEventWaitHandle(
                initialState: true,
                mode: EventResetMode.AutoReset,
                name,
                security,
                expectedCreatedNew: false);
        }

        [Fact]
        public void EventWaitHandle_Create_GlobalPrefixNameNotFound()
        {
            string prefixedName = @"GLOBAL\" + GetRandomName();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                CreateEventWaitHandle(
                    initialState: true,
                    mode: EventResetMode.AutoReset,
                    prefixedName,
                    expectedSecurity: GetBasicEventWaitHandleSecurity(),
                    expectedCreatedNew: true).Dispose();
            });
        }

        // The documentation says MAX_PATH is the length limit for name, but it won't throw any errors:
        // https://docs.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-createeventexw
        // The .NET Core constructors for EventWaitHandle do not throw on name longer than MAX_LENGTH, so the extension method should match the behavior:
        // https://source.dot.net/#System.Private.CoreLib/shared/System/Threading/EventWaitHandle.Windows.cs,20
        // The .NET Framework constructor throws:
        // https://referencesource.microsoft.com/#mscorlib/system/threading/eventwaithandle.cs,59
        [Fact]
        public void EventWaitHandle_Create_BeyondMaxPathLength()
        {
            // GetRandomName prevents name collision when two tests run at the same time
            string name = GetRandomName() + new string('x', Interop.Kernel32.MAX_PATH);

            EventWaitHandleSecurity security = GetBasicEventWaitHandleSecurity();
            EventResetMode mode = EventResetMode.AutoReset;

            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    CreateEventWaitHandle(
                        initialState: true,
                        mode,
                        name,
                        security,
                        expectedCreatedNew: true).Dispose();
                });
            }
            else
            {
                using EventWaitHandle created = CreateAndVerifyEventWaitHandle(
                        initialState: true,
                        mode,
                        name,
                        security,
                        expectedCreatedNew: true);

                using EventWaitHandle openedByName = EventWaitHandle.OpenExisting(name);
                Assert.NotNull(openedByName);
            }
        }

        [Theory]
        [InlineData((EventResetMode)int.MinValue)]
        [InlineData((EventResetMode)(-1))]
        [InlineData((EventResetMode)2)]
        [InlineData((EventResetMode)int.MaxValue)]
        public void EventWaitHandle_Create_InvalidMode(EventResetMode mode)
        {
            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    CreateEventWaitHandle(
                        initialState: true,
                        mode,
                        GetRandomName(),
                        GetBasicEventWaitHandleSecurity(),
                        expectedCreatedNew: true).Dispose();
                });
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>("mode", () =>
                {
                    CreateEventWaitHandle(
                        initialState: true,
                        mode,
                        GetRandomName(),
                        GetBasicEventWaitHandleSecurity(),
                        expectedCreatedNew: true).Dispose();
                });
            }
        }

        public static IEnumerable<object[]> EventWaitHandle_Create_SpecificParameters_MemberData() =>
            from initialState in new[] { true, false }
            from mode in new[] { EventResetMode.AutoReset, EventResetMode.ManualReset }
            from rights in new[] { EventWaitHandleRights.FullControl, EventWaitHandleRights.Synchronize, EventWaitHandleRights.Modify, EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize }
            from accessControl in new[] { AccessControlType.Allow, AccessControlType.Deny }
            select new object[] { initialState, mode, rights, accessControl };

        [Theory]
        [MemberData(nameof(EventWaitHandle_Create_SpecificParameters_MemberData))]
        public void EventWaitHandle_Create_SpecificParameters(bool initialState, EventResetMode mode, EventWaitHandleRights rights, AccessControlType accessControl)
        {
            EventWaitHandleSecurity security = GetEventWaitHandleSecurity(WellKnownSidType.BuiltinUsersSid, rights, accessControl);
            CreateAndVerifyEventWaitHandle(
                initialState,
                mode,
                GetRandomName(),
                security,
                expectedCreatedNew: true).Dispose();

        }

        [Fact]
        public void EventWaitHandle_OpenExisting()
        {
            string name = GetRandomName();
            EventWaitHandleSecurity expectedSecurity = GetEventWaitHandleSecurity(WellKnownSidType.BuiltinUsersSid, EventWaitHandleRights.FullControl, AccessControlType.Allow);
            using EventWaitHandle EventWaitHandleNew = CreateAndVerifyEventWaitHandle(initialState: true, EventResetMode.AutoReset, name, expectedSecurity, expectedCreatedNew: true);

            using EventWaitHandle EventWaitHandleExisting = EventWaitHandleAcl.OpenExisting(name, EventWaitHandleRights.FullControl);

            VerifyHandles(EventWaitHandleNew, EventWaitHandleExisting);
            EventWaitHandleSecurity actualSecurity = EventWaitHandleExisting.GetAccessControl();
            VerifyEventWaitHandleSecurity(expectedSecurity, actualSecurity);
        }

        [Fact]
        public void EventWaitHandle_TryOpenExisting()
        {
            string name = GetRandomName();
            EventWaitHandleSecurity expectedSecurity = GetEventWaitHandleSecurity(WellKnownSidType.BuiltinUsersSid, EventWaitHandleRights.FullControl, AccessControlType.Allow);
            using EventWaitHandle EventWaitHandleNew = CreateAndVerifyEventWaitHandle(initialState: true, EventResetMode.AutoReset, name, expectedSecurity, expectedCreatedNew: true);

            Assert.True(EventWaitHandleAcl.TryOpenExisting(name, EventWaitHandleRights.FullControl, out EventWaitHandle EventWaitHandleExisting));
            Assert.NotNull(EventWaitHandleExisting);

            VerifyHandles(EventWaitHandleNew, EventWaitHandleExisting);
            EventWaitHandleSecurity actualSecurity = EventWaitHandleExisting.GetAccessControl();
            VerifyEventWaitHandleSecurity(expectedSecurity, actualSecurity);

            EventWaitHandleExisting.Dispose();
        }

        [Fact]
        public void EventWaitHandle_OpenExisting_NameNotFound()
        {
            string name = "ThisShouldNotExist";
            Assert.Throws<WaitHandleCannotBeOpenedException>(() =>
            {
                EventWaitHandleAcl.OpenExisting(name, EventWaitHandleRights.FullControl).Dispose();
            });

            Assert.False(EventWaitHandleAcl.TryOpenExisting(name, EventWaitHandleRights.FullControl, out _));
        }

        [Fact]
        public void EventWaitHandle_OpenExisting_NameInvalid()
        {
            string name = '\0'.ToString();
            Assert.Throws<WaitHandleCannotBeOpenedException>(() =>
            {
                EventWaitHandleAcl.OpenExisting(name, EventWaitHandleRights.FullControl).Dispose();
            });

            Assert.False(EventWaitHandleAcl.TryOpenExisting(name, EventWaitHandleRights.FullControl, out _));
        }

        [Fact]
        public void EventWaitHandle_OpenExisting_PathNotFound()
        {
            string name = @"global\foo";
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                EventWaitHandleAcl.OpenExisting(name, EventWaitHandleRights.FullControl).Dispose();
            });

            Assert.False(EventWaitHandleAcl.TryOpenExisting(name, EventWaitHandleRights.FullControl, out _));
        }

        [Fact]
        public void EventWaitHandle_OpenExisting_BadPathName()
        {
            string name = @"\\?\Path";
            Assert.Throws<System.IO.IOException>(() =>
            {
                EventWaitHandleAcl.OpenExisting(name, EventWaitHandleRights.FullControl).Dispose();
            });

            Assert.Throws<System.IO.IOException>(() =>
            {
                EventWaitHandleAcl.TryOpenExisting(name, EventWaitHandleRights.FullControl, out _);
            });
        }

        [Fact]
        public void EventWaitHandle_OpenExisting_NullName()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                EventWaitHandleAcl.OpenExisting(null, EventWaitHandleRights.FullControl).Dispose();
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                EventWaitHandleAcl.TryOpenExisting(null, EventWaitHandleRights.FullControl, out _);
            });
        }

        [Fact]
        public void EventWaitHandle_OpenExisting_EmptyName()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                EventWaitHandleAcl.OpenExisting(string.Empty, EventWaitHandleRights.FullControl).Dispose();
            });

            Assert.Throws<ArgumentException>(() =>
            {
                EventWaitHandleAcl.TryOpenExisting(string.Empty, EventWaitHandleRights.FullControl, out _);
            });
        }

        private EventWaitHandleSecurity GetBasicEventWaitHandleSecurity()
        {
            return GetEventWaitHandleSecurity(
                WellKnownSidType.BuiltinUsersSid,
                EventWaitHandleRights.FullControl,
                AccessControlType.Allow);
        }

        private EventWaitHandleSecurity GetEventWaitHandleSecurity(WellKnownSidType sid, EventWaitHandleRights rights, AccessControlType accessControl)
        {
            EventWaitHandleSecurity security = new EventWaitHandleSecurity();
            SecurityIdentifier identity = new SecurityIdentifier(sid, null);
            EventWaitHandleAccessRule accessRule = new EventWaitHandleAccessRule(identity, rights, accessControl);
            security.AddAccessRule(accessRule);
            return security;
        }

        private EventWaitHandle CreateEventWaitHandle(bool initialState, EventResetMode mode, string name, EventWaitHandleSecurity expectedSecurity, bool expectedCreatedNew)
        {
            EventWaitHandle handle = EventWaitHandleAcl.Create(initialState, mode, name, out bool createdNew, expectedSecurity);
            Assert.NotNull(handle);
            Assert.Equal(expectedCreatedNew, createdNew);
            return handle;
        }

        private EventWaitHandle CreateAndVerifyEventWaitHandle(bool initialState, EventResetMode mode, string name, EventWaitHandleSecurity expectedSecurity, bool expectedCreatedNew)
        {
            EventWaitHandle eventHandle = CreateEventWaitHandle(initialState, mode, name, expectedSecurity, expectedCreatedNew);

            if (expectedSecurity != null)
            {
                EventWaitHandleSecurity actualSecurity = eventHandle.GetAccessControl();
                VerifyEventWaitHandleSecurity(expectedSecurity, actualSecurity);
            }

            return eventHandle;
        }

        private void VerifyHandles(EventWaitHandle expected, EventWaitHandle actual)
        {
            Assert.NotNull(expected.SafeWaitHandle);
            Assert.NotNull(actual.SafeWaitHandle);

            Assert.False(expected.SafeWaitHandle.IsClosed);
            Assert.False(actual.SafeWaitHandle.IsClosed);

            Assert.False(expected.SafeWaitHandle.IsInvalid);
            Assert.False(actual.SafeWaitHandle.IsInvalid);
        }

        private void VerifyEventWaitHandleSecurity(EventWaitHandleSecurity expectedSecurity, EventWaitHandleSecurity actualSecurity)
        {
            Assert.Equal(typeof(EventWaitHandleRights), expectedSecurity.AccessRightType);
            Assert.Equal(typeof(EventWaitHandleRights), actualSecurity.AccessRightType);

            List<EventWaitHandleAccessRule> expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<EventWaitHandleAccessRule>().ToList();

            List<EventWaitHandleAccessRule> actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<EventWaitHandleAccessRule>().ToList();

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

        private bool AreAccessRulesEqual(EventWaitHandleAccessRule expectedRule, EventWaitHandleAccessRule actualRule)
        {
            return
                expectedRule.AccessControlType     == actualRule.AccessControlType &&
                expectedRule.EventWaitHandleRights == actualRule.EventWaitHandleRights &&
                expectedRule.InheritanceFlags      == actualRule.InheritanceFlags &&
                expectedRule.PropagationFlags      == actualRule.PropagationFlags;
        }
    }
}
