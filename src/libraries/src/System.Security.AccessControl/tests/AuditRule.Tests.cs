// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public abstract class AuditRule_Tests
    {
        public abstract AuditRule Constructor(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags AuditFlags);

        public static IEnumerable<object[]> AuditRule_TestData()
        {
            yield return new object[] { "BA", 1, true, 1, 1, 1 };
            yield return new object[] { "BA", 1, false, 3, 3, 3 };
            yield return new object[] { "BA", -1, false, 3, 3, 3 };
            yield return new object[] { "BA", 0, false, 3, 3, 3 };
            yield return new object[] { "BA", 1, true, 2, 1, 1 };
            yield return new object[] { "BA", 1, true, 1, 0, 1 };
            yield return new object[] { "BA", 1, true, 1, 2, 1 };
        }

        [Fact]
        public virtual void AuditRule_Constructor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("identity", () => Constructor(null, 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 0, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), -1, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 2147483647, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1073741823, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritanceFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)4, (PropagationFlags)1, (AuditFlags)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("propagationFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)4, (AuditFlags)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("AuditFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)3));
        }

        [Theory]
        [MemberData(nameof(AuditRule_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "FileSystemAuditRule is not supported on UAP")]
        public void AuditRule_Constructor(string sid, int accessMask, bool isInherited, int inheritanceFlags, int propagationFlags, int AuditFlags)
        {
            IdentityReference identityReference = new SecurityIdentifier(sid);
            AuditRule AuditRule = Constructor(identityReference, accessMask, isInherited, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, (AuditFlags)AuditFlags);

            Assert.Equal(identityReference, AuditRule.IdentityReference);
            Assert.Equal((InheritanceFlags)inheritanceFlags, AuditRule.InheritanceFlags);
            Assert.Equal((PropagationFlags)propagationFlags, AuditRule.PropagationFlags);
            Assert.Equal((AuditFlags)AuditFlags, AuditRule.AuditFlags);
        }
    }

    public class FileSystemAuditRule_Tests : AuditRule_Tests
    {
        public override AuditRule Constructor(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags AuditFlags)
        {
            return new FileSystemAuditRule(identityReference, FileSystemRights.Read, inheritanceFlags, propagationFlags, AuditFlags);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "FileSystemAuditRule is not supported on UAP")]
        public override void AuditRule_Constructor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("identity", () => Constructor(null, 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritanceFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)4, (PropagationFlags)1, (AuditFlags)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("propagationFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)4, (AuditFlags)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("auditFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AuditFlags)4));
        }
    }
}
