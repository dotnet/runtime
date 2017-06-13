// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public abstract class AccessRule_Tests
    {
        public abstract AccessRule Constructor(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType accessControlType);

        public static IEnumerable<object[]> AccessRule_TestData()
        {
            yield return new object[] { "S-1-5-32-544", 1, true, 1, 1, 0 };
            yield return new object[] { "S-1-5-32-544", 1, true, 2, 1, 0 };
            yield return new object[] { "S-1-5-32-544", 1, true, 1, 0, 0 };
            yield return new object[] { "S-1-5-32-544", 1, true, 1, 2, 0 };
            yield return new object[] { "S-1-5-32-544", 1, true, 3, 2, 0 };
            yield return new object[] { "S-1-5-32-544", 1, true, 1, 3, 0 };
        }

        [Fact]
        public virtual void AccessRule_Constructor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("identity", () => Constructor(null, 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 0, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), -1, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 2147483647, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessMask", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1073741823, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritanceFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)4, (PropagationFlags)1, (AccessControlType)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("propagationFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)4, (AccessControlType)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("accessControlType", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)3));
        }

        [Theory]
        [MemberData(nameof(AccessRule_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "FileSystemAccessRule is not supported on UAP")]
        public void AccessRule_Constructor(string sid, int accessMask, bool isInherited, int inheritanceFlags, int propagationFlags, int accessControlType)
        {
            IdentityReference identityReference = new SecurityIdentifier(sid);
            AccessRule accessRule = Constructor(identityReference, accessMask, isInherited, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, (AccessControlType)accessControlType);

            Assert.Equal(identityReference, accessRule.IdentityReference);
            Assert.Equal((InheritanceFlags)inheritanceFlags, accessRule.InheritanceFlags);
            Assert.Equal((PropagationFlags)propagationFlags, accessRule.PropagationFlags);
            Assert.Equal((AccessControlType)accessControlType, accessRule.AccessControlType);
        }
    }

    public class FileSystemAccessRule_Tests : AccessRule_Tests
    {
        public override AccessRule Constructor(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType accessControlType)
        {
            return new FileSystemAccessRule(identityReference, FileSystemRights.Read, inheritanceFlags, propagationFlags, accessControlType);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "FileSystemAccessRule is not supported on UAP")]
        public override void AccessRule_Constructor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("identity", () => Constructor(null, 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritanceFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)4, (PropagationFlags)1, (AccessControlType)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("propagationFlags", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)4, (AccessControlType)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("type", () => Constructor(new SecurityIdentifier("S-1-5-32-544"), 1, true, (InheritanceFlags)1, (PropagationFlags)1, (AccessControlType)2));
        }
    }
}
