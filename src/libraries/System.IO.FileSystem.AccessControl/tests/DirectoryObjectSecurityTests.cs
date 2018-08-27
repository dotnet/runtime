// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System.DirectoryServices;
using System.Security.Principal;
using Xunit;
using System.Linq;
using System.Collections.Generic;

namespace System.Security.AccessControl
{
    public class DirectoryObjectSecurityTests
    {
        //https://msdn.microsoft.com/en-us/library/aa394063(v=vs.85).aspx
        private const int ReadWriteAccessMask = 0x01 | 0x02;
        private const int SynchronizeAccessMask = 0x100000;
        private const int ReadAccessMask = 0x01;
        private const int WriteAccessMask = 0x02;
        private const int ReadAttributeAccessMask = 0x80;

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
            AssertExtensions.Throws<ArgumentNullException>("securityDescriptor",
                () => new CustomDirectoryObjectSecurity(null));
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "System.DirectoryServices is not supported on this platform.")]
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
        public void RemoveAuditRuleAll_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAuditRuleReadWrite = new CustomAuditRule
                (
                    Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                    PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AuditFlags.Success
                );
            var customAuditRuleSynchronize = new CustomAuditRule
                (
                    Helpers.s_LocalSystemNTAccount, SynchronizeAccessMask, true, InheritanceFlags.None,
                    PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AuditFlags.Success
                );

            customObjectSecurity.AddAuditRule(customAuditRuleReadWrite);
            customObjectSecurity.AddAuditRule(customAuditRuleSynchronize);
            customObjectSecurity.RemoveAuditRuleAll(customAuditRuleReadWrite);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            List<CustomAuditRule> existingRules = ruleCollection.Cast<CustomAuditRule>().ToList();
            Assert.Empty(existingRules);
        }

        [Fact]
        public void RemoveAuditRuleSpecific_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAuditRuleSpecific(null));
        }

        [Fact]
        public void RemoveAuditRuleSpecific_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);

            var objectTypeGuid = Guid.NewGuid();
            var customAuditRuleReadWrite = new CustomAuditRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AuditFlags.Success
                );

            customObjectSecurity.AddAuditRule(customAuditRuleReadWrite);
            customObjectSecurity.RemoveAuditRuleSpecific(customAuditRuleReadWrite);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            List<CustomAuditRule> existingRules = ruleCollection.Cast<CustomAuditRule>().ToList();
            Assert.False(existingRules.Contains(customAuditRuleReadWrite));
        }


        [Fact]
        public void RemoveAuditRuleSpecific_NoMatchableRuleFound()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAuditRuleReadWrite = new CustomAuditRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AuditFlags.Success
                );

            var customAuditRuleWrite = new CustomAuditRule(
                Helpers.s_LocalSystemNTAccount, WriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AuditFlags.Success
                );

            customObjectSecurity.AddAuditRule(customAuditRuleReadWrite);
            Assert.Contains(customAuditRuleReadWrite, customObjectSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAuditRule>());

            customObjectSecurity.RemoveAuditRuleSpecific(customAuditRuleWrite);
            Assert.Contains(customAuditRuleReadWrite, customObjectSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAuditRule>());
        }

        [Fact]
        public void RemoveAuditRule_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAuditRule(null));
        }

        [Fact]
        public void RemoveAuditRule_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectType = Guid.NewGuid();

            var customAuditRuleWrite = new CustomAuditRule(
                Helpers.s_LocalSystemNTAccount, WriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectType, Guid.NewGuid(), AuditFlags.Success
                );

            var customAuditRuleReadWrite = new CustomAuditRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectType, Guid.NewGuid(), AuditFlags.Success
                );
            customObjectSecurity.AddAuditRule(customAuditRuleReadWrite);
            customObjectSecurity.RemoveAuditRule(customAuditRuleWrite);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.NotNull(ruleCollection);
            List<CustomAuditRule> existingRules = ruleCollection.Cast<CustomAuditRule>().ToList();
            Assert.True(existingRules.Count > 0);
            Assert.True(
                existingRules.Any(
                    x => x.AccessMaskValue == ReadAccessMask &&
                    x.AuditFlags == AuditFlags.Success &&
                    x.IdentityReference == Helpers.s_LocalSystemNTAccount
                    )
                );
        }

        [Fact]
        public void SetAuditRule_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.SetAuditRule(null));
        }

        [Fact]
        public void SetAuditRule_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAuditRuleReadWrite = new CustomAuditRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AuditFlags.Success
                );

            var customAuditRuleRead = new CustomAuditRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AuditFlags.Success
                );

            customObjectSecurity.AddAuditRule(customAuditRuleReadWrite);
            customObjectSecurity.SetAuditRule(customAuditRuleRead);

            var existingRules = customObjectSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAuditRule>().ToList();
            Assert.DoesNotContain(customAuditRuleReadWrite, existingRules);
            Assert.Contains(customAuditRuleRead, existingRules);
        }

        [Fact]
        public void AddAuditRule_InvalidObjectAuditRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.AddAuditRule(null));
        }

        [Fact]
        public void AddAuditRule_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);

            var customAuditRuleRead = new CustomAuditRule(
                new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null).Translate(typeof(NTAccount)), ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, Guid.NewGuid(), Guid.NewGuid(), AuditFlags.Success
                );

            var customAuditRuleReadAttribute = new CustomAuditRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), ReadAttributeAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, Guid.NewGuid(), Guid.NewGuid(), AuditFlags.Success
                );

            customObjectSecurity.AddAuditRule(customAuditRuleRead);
            customObjectSecurity.AddAuditRule(customAuditRuleReadAttribute);
            AuthorizationRuleCollection ruleCollection = customObjectSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.NotNull(ruleCollection);
            List<CustomAuditRule> addedRules = ruleCollection.Cast<CustomAuditRule>().ToList();

            Assert.Contains(customAuditRuleRead, addedRules);
            Assert.Contains(customAuditRuleReadAttribute, addedRules);
        }

        [Fact]
        public void RemoveAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAccessRule(null));
        }

        [Fact]
        public void RemoveRule_AccessControlType_Allow_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();
            
            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            var customAccessRuleWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, WriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            bool result = customObjectSecurity.RemoveAccessRule(customAccessRuleWrite);

            Assert.Equal(true, result);
            AuthorizationRuleCollection ruleCollection = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.NotNull(ruleCollection);

            Assert.Contains(ruleCollection.Cast<CustomAccessRule>(), x =>
                x.IdentityReference == Helpers.s_LocalSystemNTAccount &&
                x.AccessControlType == customAccessRuleReadWrite.AccessControlType &&
                x.AccessMaskValue == ReadAccessMask
            );
        }

        [Fact]
        public void RemoveAccessRule_AccessControlType_Deny_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);

            int readDataAndAttribute = ReadAccessMask | ReadAttributeAccessMask;
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadDataAndAttribute = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, readDataAndAttribute, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            var customAccessRuleRead = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadDataAndAttribute);
            customObjectSecurity.RemoveAccessRule(customAccessRuleRead);

            AuthorizationRuleCollection ruleCollection = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.NotNull(ruleCollection);

            Assert.Contains(ruleCollection.Cast<CustomAccessRule>(), x =>
                x.IdentityReference == Helpers.s_LocalSystemNTAccount &&
                x.AccessControlType == AccessControlType.Deny &&
                x.AccessMaskValue == ReadAttributeAccessMask
            );
        }

        [Fact]
        public void RemoveAccessRuleSpecific_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAccessRuleSpecific(null));
        }

        [Fact]
        public void RemoveAccessRuleSpecific_AccessControlType_Allow_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();
            
            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            customObjectSecurity.RemoveAccessRuleSpecific(customAccessRuleReadWrite);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            List<CustomAccessRule> existingRules = ruleCollection.Cast<CustomAccessRule>().ToList();
            Assert.False(existingRules.Contains(customAccessRuleReadWrite));
        }

        [Fact]
        public void RemoveAccessRuleSpecific_AccessControlType_Deny_NoMatchableRules_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            var customAccessRuleRead = new CustomAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            customObjectSecurity.RemoveAccessRuleSpecific(customAccessRuleRead);

            Assert.Contains(customAccessRuleReadWrite, customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>());
        }

        [Fact]
        public void RemoveAccessRuleAll_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.RemoveAccessRuleAll(null));
        }

        [Fact]
        public void RemoveAccessRuleAll_AccessControlType_Allow_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            var customAccessRuleSynchronize = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, SynchronizeAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            customObjectSecurity.AddAccessRule(customAccessRuleSynchronize);
            customObjectSecurity.RemoveAccessRuleAll(customAccessRuleReadWrite);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            List<CustomAccessRule> existingRules = ruleCollection.Cast<CustomAccessRule>().ToList();

            Assert.False(existingRules.Contains(customAccessRuleReadWrite));
            Assert.False(existingRules.Contains(customAccessRuleSynchronize));
        }

        [Fact]        
        public void RemoveAccessRuleAll_AccessControlType_Deny_ThrowException()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.ObjectInherit,
                PropagationFlags.InheritOnly, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            AssertExtensions.Throws<InvalidOperationException, SystemException>(() => customObjectSecurity.RemoveAccessRuleAll(customAccessRuleReadWrite));
        }

        [Fact]
        public void RemoveAccessRuleAll_AccessControlType_Deny_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            var customAccessRuleSynchronize = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, SynchronizeAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            customObjectSecurity.AddAccessRule(customAccessRuleSynchronize);
            customObjectSecurity.RemoveAccessRuleAll(customAccessRuleSynchronize);

            AuthorizationRuleCollection ruleCollection =
                customObjectSecurity
               .GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            List<CustomAccessRule> existingRules = ruleCollection.Cast<CustomAccessRule>().ToList();
            Assert.False(existingRules.Contains(customAccessRuleReadWrite));
            Assert.False(existingRules.Contains(customAccessRuleSynchronize));
        }


        [Fact]
        public void ResetAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.ResetAccessRule(null));
        }

        [Fact]
        public void ResetAccessRule_AccessControlType_Allow_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            var customAccessRuleNetworkService = new CustomAccessRule(
                Helpers.s_NetworkServiceNTAccount, SynchronizeAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            var customAccessRuleRead = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            Assert.Contains(customAccessRuleReadWrite, customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>());

            customObjectSecurity.AddAccessRule(customAccessRuleNetworkService);
            List<CustomAccessRule> existingRules = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>().ToList();
            Assert.Contains(customAccessRuleReadWrite, existingRules);
            Assert.Contains(customAccessRuleNetworkService, existingRules);

            customObjectSecurity.ResetAccessRule(customAccessRuleRead);
            existingRules = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>().ToList();
            Assert.DoesNotContain(customAccessRuleReadWrite, existingRules);
            Assert.Contains(customAccessRuleNetworkService, existingRules);
            Assert.Contains(customAccessRuleRead, existingRules);
        }

        [Fact]
        public void ResetAccessRule_AccessControlType_Deny_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            var customAccessRuleNetworkService = new CustomAccessRule(
                Helpers.s_NetworkServiceNTAccount, SynchronizeAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            var customAccessRuleWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, WriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            Assert.Contains(customAccessRuleReadWrite, customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>());

            customObjectSecurity.AddAccessRule(customAccessRuleNetworkService);
            List<CustomAccessRule> existingRules = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>().ToList();
            Assert.Contains(customAccessRuleReadWrite, existingRules);
            Assert.Contains(customAccessRuleNetworkService, existingRules);

            customObjectSecurity.ResetAccessRule(customAccessRuleWrite);
            existingRules = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>().ToList();
            Assert.DoesNotContain(customAccessRuleReadWrite, existingRules);
            Assert.Contains(customAccessRuleNetworkService, existingRules);
            Assert.Contains(customAccessRuleWrite, existingRules);
        }

        [Fact]
        public void SetAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.SetAccessRule(null));
        }

        [Fact]
        public void SetAccessRule_AccessControlType_Allow_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            var customAccessRuleRead = new CustomAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Allow
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            customObjectSecurity.SetAccessRule(customAccessRuleRead);

            List<CustomAccessRule> existingRules = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>().ToList();

            Assert.DoesNotContain(customAccessRuleReadWrite, existingRules);
            Assert.Contains(customAccessRuleRead, existingRules);
        }

        [Fact]
        public void SetAccessRule_AccessControlType_Deny_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);
            var objectTypeGuid = Guid.NewGuid();

            var customAccessRuleReadWrite = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadWriteAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            var customAccessRuleRead = new CustomAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, objectTypeGuid, Guid.NewGuid(), AccessControlType.Deny
                );

            customObjectSecurity.AddAccessRule(customAccessRuleReadWrite);
            Assert.Contains(customAccessRuleReadWrite, customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>());

            customObjectSecurity.SetAccessRule(customAccessRuleRead);
            var existingRules = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<CustomAccessRule>().ToList();
            Assert.DoesNotContain(customAccessRuleReadWrite, existingRules);
            Assert.Contains(customAccessRuleRead, existingRules);
        }

        [Fact]
        public void AddAccessRule_InvalidObjectAccessRule()
        {
            var customObjectSecurity = new CustomDirectoryObjectSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => customObjectSecurity.AddAccessRule(null));
        }

        [Fact]
        public void AddAccessRule_Succeeds()
        {
            var descriptor = new CommonSecurityDescriptor(true, true, string.Empty);
            var customObjectSecurity = new CustomDirectoryObjectSecurity(descriptor);

            var customAccessRuleAllow = new CustomAccessRule(
                Helpers.s_NetworkServiceNTAccount, ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, Guid.NewGuid(), Guid.NewGuid(), AccessControlType.Allow
                );

            var customAccessRuleDeny = new CustomAccessRule(
                Helpers.s_LocalSystemNTAccount, ReadAccessMask, true, InheritanceFlags.None,
                PropagationFlags.None, Guid.NewGuid(), Guid.NewGuid(), AccessControlType.Deny
                );

            customObjectSecurity.AddAccessRule(customAccessRuleAllow);
            customObjectSecurity.AddAccessRule(customAccessRuleDeny);
            AuthorizationRuleCollection ruleCollection = customObjectSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.NotNull(ruleCollection);
            List<CustomAccessRule> addedRules = ruleCollection.Cast<CustomAccessRule>().ToList();

            Assert.Contains(customAccessRuleAllow, addedRules);
            Assert.Contains(customAccessRuleDeny, addedRules);
        }

        private class CustomDirectoryObjectSecurity : DirectoryObjectSecurity
        {
            public CustomDirectoryObjectSecurity() { }

            public CustomDirectoryObjectSecurity(CommonSecurityDescriptor securityDescriptor)
                : base(securityDescriptor)
            { }

            public override Type AccessRightType => throw new NotImplementedException();

            public override Type AccessRuleType => throw new NotImplementedException();

            public override Type AuditRuleType => throw new NotImplementedException();

            public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited,
                InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            {
                return new CustomAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
            }
            public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited,
                InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type, Guid objectType,
                Guid inheritedObjectType)
            {
                return new CustomAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags,
                    objectType, inheritedObjectType, type);
            }
            public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited,
                InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            {
                return new CustomAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
            }
            public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited,
                InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags, Guid objectType,
                Guid inheritedObjectType)
            {
                return new CustomAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags,
                    objectType, inheritedObjectType, flags);
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

        private class CustomAuditRule : ObjectAuditRule
        {
            public int AccessMaskValue
            {
                get
                {
                    return AccessMask;
                }
            }

            public CustomAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
                PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AuditFlags auditFlags)
                : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, objectType, inheritedObjectType, auditFlags)
            {
            }

            public CustomAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags
                , PropagationFlags propagationFlags, AuditFlags auditFlags)
                : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, Guid.NewGuid(), Guid.NewGuid(), auditFlags)
            {
            }

            public override bool Equals(object value)
            {
                if (object.ReferenceEquals(null, value))
                {
                    return false;
                }
                if (object.ReferenceEquals(this, value))
                {
                    return true;
                }
                if (value.GetType() != this.GetType())
                {
                    return false;
                }
                return IsEqual((CustomAuditRule)value);
            }

            private bool IsEqual(CustomAuditRule auditRule)
            {
                return IdentityReference.Equals(auditRule.IdentityReference)
                    && AccessMask.Equals(auditRule.AccessMask)
                    && AuditFlags.Equals(auditRule.AuditFlags)
                    && InheritanceFlags.Equals(auditRule.InheritanceFlags)
                    && PropagationFlags.Equals(auditRule.PropagationFlags);
            }

            public override int GetHashCode()
            {
                return IdentityReference.GetHashCode() ^
                       AccessMask.GetHashCode() ^
                       AuditFlags.GetHashCode() ^
                       InheritanceFlags.GetHashCode() ^
                       PropagationFlags.GetHashCode();
            }
        }

        private class CustomAccessRule : ObjectAccessRule
        {
            public int AccessMaskValue
            {
                get
                {
                    return AccessMask;
                }
            }
            public CustomAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags
                , PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AccessControlType type)
                : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, objectType, inheritedObjectType, type)
            {
            }

            public CustomAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags
                , PropagationFlags propagationFlags, AccessControlType type)
                : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, Guid.NewGuid(), Guid.NewGuid(), type)
            {
            }

            public override bool Equals(object value)
            {
                if (object.ReferenceEquals(null, value))
                {
                    return false;
                }
                if (object.ReferenceEquals(this, value))
                {
                    return true;
                }
                if (value.GetType() != this.GetType())
                {
                    return false;
                }
                return IsEqual((CustomAccessRule)value);
            }

            private bool IsEqual(CustomAccessRule accessRule)
            {
                return IdentityReference.Equals(accessRule.IdentityReference)
                    && AccessMask.Equals(accessRule.AccessMask)
                    && AccessControlType.Equals(accessRule.AccessControlType)
                    && InheritanceFlags.Equals(accessRule.InheritanceFlags)
                    && PropagationFlags.Equals(accessRule.PropagationFlags);
            }

            public override int GetHashCode()
            {
                return IdentityReference.GetHashCode() ^
                       AccessMask.GetHashCode() ^
                       AccessControlType.GetHashCode() ^
                       InheritanceFlags.GetHashCode() ^
                       PropagationFlags.GetHashCode();
            }
        }
    }
}
