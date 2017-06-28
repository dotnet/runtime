// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.DirectoryServices;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl
{
    public class DirectoryObjectSecurityTests
    {
        [Fact]
        public void ObjectInitialization_DefaultConstructor_Success()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            Assert.True(customObjectSecurity.IsDS);
            Assert.True(customObjectSecurity.IsContainer);
        }

        [Fact]
        public void ObjectInitialization_CommonSecurityDescriptor_Success()
        {
            var descriptor = new CommonSecurityDescriptor(false, false, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            Assert.False(customObjectSecurity.IsDS);
            Assert.False(customObjectSecurity.IsContainer);
        }

        [Fact]
        public void ObjectInitialization_InvalidSecurityDescriptor()
        {
            AssertExtensions.Throws<ArgumentNullException>("securityDescriptor", () => new CustomDirectoryObjectSecurity(null));
        }

        [Fact]
        public void GetAccessRules_InvalidTargetType()
        {
            var activeDirectorySecurity = new ActiveDirectorySecurity();
            AssertExtensions.Throws<ArgumentException>("targetType", () =>
               activeDirectorySecurity
               .GetAccessRules(false, false, typeof(System.Security.Principal.GenericPrincipal)));
        }

        [Fact]
        public void GetAccessRules_TargetType_SecurityIdentifier_ReturnsValidObject()
        {
            var descriptor = new CommonSecurityDescriptor(false, false, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAccessRules(false, false, typeof(System.Security.Principal.SecurityIdentifier));

            Assert.NotNull(ruleCollection);
            Assert.Empty(ruleCollection);
        }

        [Fact]
        public void GetAccessRules_TargetType_NTAccount_ReturnsValidObject()
        {
            var descriptor = new CommonSecurityDescriptor(false, false, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAccessRules(false, false, typeof(System.Security.Principal.NTAccount));

            Assert.NotNull(ruleCollection);
            Assert.Empty(ruleCollection);
        }

        [Fact]
        public void GetAuditRules_ReturnsValidObject()
        {
            var descriptor = new CommonSecurityDescriptor(false, false, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAuditRules(true, false, typeof(System.Security.Principal.NTAccount));

            Assert.NotNull(ruleCollection);
        }


        [Fact]
        public void RemoveAuditRuleAll_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAuditRuleAll(null));
        }

        [Fact]
        public void RemoveAuditRuleSpecific_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAuditRuleSpecific(null));
        }

        [Fact]
        public void RemoveAuditRule_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAuditRule(null));
        }

        [Fact]
        public void SetAuditRule_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.SetAuditRule(null));
        }
        [Fact]
        public void AddAuditRule_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.AddAuditRule(null));
        }

        [Fact]
        public void RemoveAccessRuleSpecific_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAccessRuleSpecific(null));
        }

        [Fact]
        public void RemoveAccessRuleAll_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAccessRuleAll(null));
        }

        [Fact]
        public void RemoveAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAccessRule(null));
        }

        [Fact]
        public void ResetAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.ResetAccessRule(null));
        }

        [Fact]
        public void SetAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.SetAccessRule(null));
        }

        [Fact]
        public void AddAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.AddAccessRule(null));
        }

        private class CustomDirectoryObjectSecurity : DirectoryObjectSecurity
        {
            public CustomDirectoryObjectSecurity()
            {
            }
            public CustomDirectoryObjectSecurity(CommonSecurityDescriptor securityDescriptor) : base(securityDescriptor)
            {
            }

            public override Type AccessRightType => throw new NotImplementedException();

            public override Type AccessRuleType => throw new NotImplementedException();

            public override Type AuditRuleType => throw new NotImplementedException();

            public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            {
                throw new NotImplementedException();
            }
            public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            {
                throw new NotImplementedException();
            }
            public new void RemoveAuditRuleAll(ObjectAuditRule rule)
            {
                base.RemoveAuditRuleAll(rule);
            }
            public new void RemoveAuditRuleSpecific(ObjectAuditRule rule)
            {
                base.RemoveAuditRuleSpecific(rule);
            }
            public new bool RemoveAuditRule(ObjectAuditRule rule)
            {
                return base.RemoveAuditRule(rule);
            }
            public new void SetAuditRule(ObjectAuditRule rule)
            {
                base.SetAuditRule(rule);
            }
            public new void AddAuditRule(ObjectAuditRule rule)
            {
                base.AddAuditRule(rule);
            }
            public new void RemoveAccessRuleSpecific(ObjectAccessRule rule)
            {
                base.RemoveAccessRuleSpecific(rule);
            }
            public new void RemoveAccessRuleAll(ObjectAccessRule rule)
            {
                base.RemoveAccessRuleAll(rule);
            }
            public new bool RemoveAccessRule(ObjectAccessRule rule)
            {
                return base.RemoveAccessRule(rule);
            }
            public new void ResetAccessRule(ObjectAccessRule rule)
            {
                base.ResetAccessRule(rule);
            }
            public new void SetAccessRule(ObjectAccessRule rule)
            {
                base.SetAccessRule(rule);
            }
            public new void AddAccessRule(ObjectAccessRule rule)
            {
                base.AddAccessRule(rule);
            }
            public new bool IsDS
            {
                get
                {
                    return base.IsDS;
                }
            }
            public new bool IsContainer
            {
                get
                {
                    return base.IsContainer;
                }
            }
        }
    }
}
