using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class PipeServerStreamAclTestBase
    {
        protected PipeSecurity GetBasicPipeSecurity()
        {
            return GetPipeSecurity(
                WellKnownSidType.BuiltinUsersSid,
                PipeAccessRights.FullControl,
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
    }
}
