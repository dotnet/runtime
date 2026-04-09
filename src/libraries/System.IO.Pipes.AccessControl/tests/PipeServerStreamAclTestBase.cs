// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class PipeServerStreamAclTestBase
    {
        protected const PipeDirection DefaultPipeDirection = PipeDirection.In;
        protected const HandleInheritability DefaultInheritability = HandleInheritability.None;
        protected const int DefaultBufferSize = 1;

        // As it is documented in the source definition of the PipeAccessRights enum, we do not have a 0 value on purpose (can't grant nor deny "nothing").
        // So ReadWrite will be used in these unit tests the sole minimum additional granted right, considering that AnonymousPipeServerStreams can only
        // get created with either ReadWrite or FullControl.
        protected const PipeAccessRights DefaultAccessRight = PipeAccessRights.ReadWrite;

        // PipeAccessRights.Synchronize is not included in this arary because it is handled in a special way inside the PipeAccessRuleInstance constructor when creating the access mask: If Deny is specified, Synchronize gets removed from the rights.
        // So this right's behavior is verified separately in the System.IO.Pipes.Tests.PipeTest_AclExtensions.PipeSecurity_VerifySynchronizeMasks unit test.
        protected static readonly PipeAccessRights[] s_mostRights = new[]
        {
            PipeAccessRights.ReadData,
            PipeAccessRights.WriteData,
            PipeAccessRights.CreateNewInstance,
            PipeAccessRights.ReadExtendedAttributes,
            PipeAccessRights.WriteExtendedAttributes,
            PipeAccessRights.ReadAttributes,
            PipeAccessRights.WriteAttributes,
            PipeAccessRights.Write,
            PipeAccessRights.Delete,
            PipeAccessRights.ReadPermissions,
            PipeAccessRights.Read,
            PipeAccessRights.ReadWrite,
            PipeAccessRights.ChangePermissions,
            PipeAccessRights.TakeOwnership,
            PipeAccessRights.FullControl,
            PipeAccessRights.AccessSystemSecurity
        };

        protected static readonly PipeAccessRights[] s_bitWisePipeAccessRights = new[]
        {
            PipeAccessRights.ChangePermissions | PipeAccessRights.ReadPermissions,
            PipeAccessRights.ReadExtendedAttributes | PipeAccessRights.WriteExtendedAttributes
        };

        protected static IEnumerable<PipeAccessRights> s_combinedPipeAccessRights = s_mostRights.Concat(s_bitWisePipeAccessRights);

        protected PipeSecurity GetBasicPipeSecurity()
        {
            return GetPipeSecurity(
                WellKnownSidType.BuiltinUsersSid,
                DefaultAccessRight,
                AccessControlType.Allow);
        }

        protected PipeSecurity GetPipeSecurity(WellKnownSidType sid, PipeAccessRights rights, AccessControlType accessControl)
        {
            var security = new PipeSecurity();
            SecurityIdentifier identity = new SecurityIdentifier(sid, null);
            var accessRule = new PipeAccessRule(identity, rights, accessControl);
            security.AddAccessRule(accessRule);
            return security;
        }

        protected void VerifyPipeSecurity(PipeSecurity expectedSecurity, PipeSecurity actualSecurity)
        {
            Assert.Equal(typeof(PipeAccessRights), expectedSecurity.AccessRightType);
            Assert.Equal(typeof(PipeAccessRights), actualSecurity.AccessRightType);

            List<PipeAccessRule> expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<PipeAccessRule>().ToList();

            List<PipeAccessRule> actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<PipeAccessRule>().ToList();

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

        protected bool AreAccessRulesEqual(PipeAccessRule expectedRule, PipeAccessRule actualRule)
        {
            return
                expectedRule.AccessControlType == actualRule.AccessControlType &&
                expectedRule.PipeAccessRights  == actualRule.PipeAccessRights &&
                expectedRule.InheritanceFlags  == actualRule.InheritanceFlags &&
                expectedRule.PropagationFlags  == actualRule.PropagationFlags;
        }

        protected string GetRandomName()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static IEnumerable<object[]> Create_MostAccessRights_MemberData() =>
            from rights in s_mostRights
            select new object[] { rights };

        public static IEnumerable<object[]> Create_InvalidPipeDirection_MemberData() =>
            from direction in new[]
            {
                (PipeDirection)(int.MinValue),
                (PipeDirection)0,
                (PipeDirection)4,
                (PipeDirection)(int.MaxValue)
            }
            select new object[] { direction };

        public static IEnumerable<object[]> Create_InvalidInheritability_MemberData() =>
            from inheritability in new[]
            {
                (HandleInheritability)int.MinValue,
                (HandleInheritability)(-1),
                (HandleInheritability)2,
                (HandleInheritability)int.MaxValue
            }
            select new object[] { inheritability };

        public static IEnumerable<object[]> Create_InvalidBufferSize_MemberData() =>
            from bufferSize in new[] { int.MinValue, -1 }
            select new object[] { bufferSize };
    }
}
