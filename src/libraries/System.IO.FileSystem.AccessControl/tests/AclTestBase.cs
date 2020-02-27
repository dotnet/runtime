using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO
{
    public class AclTestBase : FileCleanupTestBase
    {
        protected void VerifyAccessSecurity(CommonObjectSecurity expectedSecurity, CommonObjectSecurity actualSecurity)
        {
            if (expectedSecurity.AccessRightType == typeof(FileSystemRights))
            {
                Assert.Equal(typeof(FileSystemRights), actualSecurity.AccessRightType);
            }
            else if (expectedSecurity.AccessRightType == typeof(MemoryMappedFileRights))
            {
                Assert.Equal(typeof(MemoryMappedFileRights), actualSecurity.AccessRightType);
            }

            List<AccessRule> expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<AccessRule>().ToList();

            List<AccessRule> actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<AccessRule>().ToList();

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

        protected bool AreAccessRulesEqual(AccessRule expectedRule, AccessRule actualRule)
        {
            Type expectedType = expectedRule.GetType();
            Type actualType = actualRule.GetType();

            Assert.Equal(expectedType, actualType);

            bool result = true;

            // A FileSystemAccessRule has one more field to verify
            if (expectedType == typeof(FileSystemAccessRule))
            {
                result = ((FileSystemAccessRule)expectedRule).FileSystemRights == ((FileSystemAccessRule)actualRule).FileSystemRights;
            };

            return result &&
                expectedRule.AccessControlType == actualRule.AccessControlType &&
                expectedRule.InheritanceFlags  == actualRule.InheritanceFlags &&
                expectedRule.PropagationFlags  == actualRule.PropagationFlags;
        }
    }
}
