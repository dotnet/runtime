// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using Xunit;

namespace System.Tests
{
#pragma warning disable 618
    public static class SecurityAttributeTests
    {
        [Fact]
        public static void InstantiateSecurityAttributes()
        {
            var t = new SecurityTreatAsSafeAttribute();
            var u = new SecuritySafeCriticalAttribute();
            var v = new SecurityTransparentAttribute();
            var w = new SecurityCriticalAttribute();
            var x = new SuppressUnmanagedCodeSecurityAttribute();
            var y = new UnverifiableCodeAttribute();
            var z = new AllowPartiallyTrustedCallersAttribute();
        }

        [Fact]
        public static void SecurityCriticalAttribute_Test()
        {
            var att = new SecurityCriticalAttribute(SecurityCriticalScope.Everything);

            Assert.Equal(SecurityCriticalScope.Everything, att.Scope);
        }

        [Fact]
        public static void AllowPartiallyTrustedCallersAttribute_Test()
        {
            var att = new AllowPartiallyTrustedCallersAttribute();
            att.PartialTrustVisibilityLevel = PartialTrustVisibilityLevel.NotVisibleByDefault;

            Assert.Equal(PartialTrustVisibilityLevel.NotVisibleByDefault, att.PartialTrustVisibilityLevel);
        }

        [Fact]
        public static void SecurityRulesAttribute_Test()
        {
            var att = new SecurityRulesAttribute(SecurityRuleSet.Level1);
            Assert.Equal(SecurityRuleSet.Level1, att.RuleSet);
            Assert.False(att.SkipVerificationInFullTrust);

            att.SkipVerificationInFullTrust = true;
            Assert.True(att.SkipVerificationInFullTrust);
        }
    }
#pragma warning restore 618
}
