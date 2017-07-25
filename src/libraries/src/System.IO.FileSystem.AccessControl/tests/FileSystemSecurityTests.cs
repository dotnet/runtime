// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO
{
    public class FileSystemSecurityTests
    {
        [Fact]
        public void AddAccessRule_InvalidFileSystemAccessRule()
        {
            var fileSecurity = new FileSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => fileSecurity.AddAccessRule(null));
        }

        [Fact]
        public void AddAccessRule_Succeeds()
        {
            var accessRule = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.AppendData, AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var actualAddedRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(accessRule.IdentityReference, actualAddedRule.IdentityReference);
            Assert.Equal(accessRule.FileSystemRights, actualAddedRule.FileSystemRights);
            Assert.Equal(accessRule.AccessControlType, actualAddedRule.AccessControlType);
        }

        [Fact]
        public void SetAccessRule_InvalidFileSystemAccessRule()
        {
            var fileSecurity = new FileSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => fileSecurity.SetAccessRule(null));
        }

        [Fact]
        public void SetAccessRule_Succeeds()
        {
            var accessRuleRead = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read, AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleRead);
            var accessRuleWrite = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Write, AccessControlType.Allow);
            //Changing the value of file system rights from "read" to "write".
            fileSecurity.SetAccessRule(accessRuleWrite);

            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(1, rules.Count);
            var existingAccessRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(accessRuleWrite.FileSystemRights, existingAccessRule.FileSystemRights);
        }

        [Fact]
        public void SetAccessRule_IgnoreExistingRule_Succeeds()
        {
            var accessRuleRead = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read, AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleRead);
            var newAccessRule = new FileSystemAccessRule(@"NT AUTHORITY\Network Service",
                FileSystemRights.Write, AccessControlType.Allow);
            fileSecurity.SetAccessRule(newAccessRule);

            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(2, rules.Count);
            var existingAccessRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\SYSTEM"), existingAccessRule.IdentityReference);
            existingAccessRule = (FileSystemAccessRule)rules[1];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\Network Service"), existingAccessRule.IdentityReference);
        }

        [Fact]
        public void ResetAccessRule_InvalidFileSystemAccessRule()
        {
            var fileSecurity = new FileSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => fileSecurity.ResetAccessRule(null));
        }

        [Fact]
        public void ResetSetAccessRule_Succeeds()
        {
            var accessRuleRead = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read, AccessControlType.Allow);
            var accessRuleAppendData = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.AppendData, AccessControlType.Deny);
            var accessRuleWrite = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Write, AccessControlType.Allow);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleRead);
            fileSecurity.AddAccessRule(accessRuleAppendData);
            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(2, rules.Count);
            //Resetting the access rules.
            fileSecurity.ResetAccessRule(accessRuleWrite);
            rules = fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var existingAccessRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(accessRuleWrite.FileSystemRights, existingAccessRule.FileSystemRights);
            Assert.Equal(AccessControlType.Allow, existingAccessRule.AccessControlType);
        }

        [Fact]
        public void RemoveAccessRule_InvalidFileSystemAccessRule()
        {
            var fileSecurity = new FileSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => fileSecurity.RemoveAccessRule(null));
        }

        [Fact]
        public void RemoveAccessRule_Succeeds()
        {
            var accessRule = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            AuthorizationRuleCollection rules =
               fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            //Removing the "write" access right.
            Assert.True(fileSecurity.RemoveAccessRule(new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                 FileSystemRights.Write,
                AccessControlType.Allow)));
            rules = fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var remainingRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(FileSystemRights.Read | FileSystemRights.Synchronize, remainingRule.FileSystemRights);
        }

        [Fact]
        public void RemoveAccessRule_IdenticalRule_Succeeds()
        {
            var accessRule = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            Assert.True(fileSecurity.RemoveAccessRule(new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                 FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow)));
            var rules = fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(0, rules.Count);
        }

        [Fact]
        public void RemoveAccessRule_NoMatchableRules_Succeeds()
        {
            var accessRuleAppendData = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM", FileSystemRights.AppendData,
                AccessControlType.Allow);
            var accessRuleWrite = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Write, AccessControlType.Deny);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleAppendData);
            Assert.True(fileSecurity.RemoveAccessRule(accessRuleWrite));
            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var remainingRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\SYSTEM"), accessRuleAppendData.IdentityReference);
            Assert.Equal(accessRuleAppendData.FileSystemRights, remainingRule.FileSystemRights);
            Assert.Equal(AccessControlType.Allow, remainingRule.AccessControlType);
        }

        [Fact]
        public void RemoveAccessRuleSpecific_Invalid_FileSystemAccessRule()
        {
            var fileSecurity = new FileSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => fileSecurity.RemoveAccessRuleSpecific(null));
        }

        [Fact]
        public void RemoveAccessRuleSpecific_NoMatchingRules_Succeeds()
        {
            var accessRuleReadWrite = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow);
            var accessRuleWrite = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Write, AccessControlType.Allow);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleReadWrite);
            fileSecurity.RemoveAccessRuleSpecific(accessRuleWrite);
            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var remainingRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(FileSystemRights.Write | FileSystemRights.Read | FileSystemRights.Synchronize,
                remainingRule.FileSystemRights);
        }

        [Fact]
        public void RemoveAccessRuleSpecific_Succeeds()
        {
            var accessRule = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM", FileSystemRights.AppendData
                | FileSystemRights.Write, AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            AuthorizationRuleCollection rules =
               fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            fileSecurity.RemoveAccessRuleSpecific(accessRule);
            rules = fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(0, rules.Count);
        }

        [Fact]
        public void RemoveAccessRuleAll_InvalidFileSystemAccessRule()
        {
            var fileSecurity = new FileSecurity();
            AssertExtensions.Throws<ArgumentNullException>("rule", () => fileSecurity.RemoveAccessRuleAll(null));
        }

        [Fact]
        public void RemoveAccessRuleAll_Succeeds()
        {
            var accessRuleAppendData = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM", FileSystemRights.AppendData,
                AccessControlType.Allow);
            var accessRuleRead = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read, AccessControlType.Allow);
            var accessRuleWrite = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Write, AccessControlType.Allow);
            var accessRuleReadPermissionDeny = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM",
              FileSystemRights.ReadPermissions, AccessControlType.Deny);
            var accessRuleReadNetworkService = new FileSystemAccessRule(@"NT AUTHORITY\Network Service",
                FileSystemRights.Read, AccessControlType.Allow);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleAppendData);
            fileSecurity.AddAccessRule(accessRuleRead);
            fileSecurity.AddAccessRule(accessRuleReadPermissionDeny);
            fileSecurity.AddAccessRule(accessRuleReadNetworkService);
            //Removing all the access rules of the "System" user with the access control type "allow".
            fileSecurity.RemoveAccessRuleAll(accessRuleWrite);
            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(2, rules.Count);
            var existingAccessRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\SYSTEM"), existingAccessRule.IdentityReference);
            Assert.Equal(AccessControlType.Deny, existingAccessRule.AccessControlType);
            Assert.Equal(FileSystemRights.ReadPermissions, existingAccessRule.FileSystemRights);
            existingAccessRule = (FileSystemAccessRule)rules[1];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\Network Service"), existingAccessRule.IdentityReference);
            Assert.Equal(AccessControlType.Allow, existingAccessRule.AccessControlType);
        }

        [Fact]
        public void AccessRuleType_Returns_Valid_Object()
        {
            var accessRule = new FileSystemAccessRule(@"NT AUTHORITY\SYSTEM", FileSystemRights.AppendData,
             AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            Type accessRuleType = fileSecurity.AccessRuleType;
            Assert.Equal(typeof(FileSystemAccessRule), accessRuleType);
        }

        [Fact]
        public void AddAuditRule_Succeeds()
        {
            var auditRule = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.AppendData, AuditFlags.Success);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRule);
            AuthorizationRuleCollection auditRules =
                fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, auditRules.Count);
            var actualAddedRule = (FileSystemAuditRule)auditRules[0];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\SYSTEM"), actualAddedRule.IdentityReference);
            Assert.Equal(AuditFlags.Success, actualAddedRule.AuditFlags);
            Assert.Equal(FileSystemRights.AppendData, actualAddedRule.FileSystemRights);
        }

        [Fact]
        public void SetAuditRule_Succeeds()
        {
            var auditRuleAppendData = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.AppendData, AuditFlags.Success);
            var auditRuleNetworkService = new FileSystemAuditRule(@"NT AUTHORITY\Network Service",
                FileSystemRights.CreateFiles, AuditFlags.Failure);
            var auditRuleDelete = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Delete, AuditFlags.Success);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleNetworkService);
            fileSecurity.AddAuditRule(auditRuleAppendData);
            fileSecurity.SetAuditRule(auditRuleDelete);
            var auditRules = fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(2, auditRules.Count);
            var firstAuditRule = (FileSystemAuditRule)auditRules[0];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\SYSTEM"), firstAuditRule.IdentityReference);
            Assert.Equal(AuditFlags.Success, firstAuditRule.AuditFlags);
            Assert.Equal(FileSystemRights.Delete, firstAuditRule.FileSystemRights);
            var secondAuditRule = (FileSystemAuditRule)auditRules[1];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\Network Service"), secondAuditRule.IdentityReference);
            Assert.Equal(AuditFlags.Failure, secondAuditRule.AuditFlags);
            Assert.Equal(FileSystemRights.CreateFiles, secondAuditRule.FileSystemRights);
        }

        [Fact]
        public void RemoveAuditRule_Succeeds()
        {
            var auditRule = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Read | FileSystemRights.Write,
                AuditFlags.Failure);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRule);
            AuthorizationRuleCollection rules =
               fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            Assert.True(fileSecurity.RemoveAuditRule(new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Write, AuditFlags.Failure)));

            rules = fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var existingRule = (FileSystemAuditRule)rules[0];
            Assert.Equal(FileSystemRights.Read, existingRule.FileSystemRights);
            Assert.Equal(AuditFlags.Failure, existingRule.AuditFlags);
            Assert.Equal(new NTAccount(@"NT AUTHORITY\SYSTEM"), existingRule.IdentityReference);
        }

        [Fact]
        public void RemoveAuditRuleSpecific_Succeeds()
        {
            var auditRuleReadWrite = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
               FileSystemRights.Write | FileSystemRights.Read, AuditFlags.Success);
            var auditRuleNetworkService = new FileSystemAuditRule(@"NT AUTHORITY\Network Service",
                FileSystemRights.Read, AuditFlags.Failure);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleReadWrite);
            fileSecurity.AddAuditRule(auditRuleNetworkService);
            fileSecurity.RemoveAuditRuleSpecific(auditRuleReadWrite);
            AuthorizationRuleCollection rules =
              fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var existingAuditRule = (FileSystemAuditRule)rules[0];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\Network Service"), existingAuditRule.IdentityReference);
            Assert.Equal(FileSystemRights.Read, existingAuditRule.FileSystemRights);
            Assert.Equal(AuditFlags.Failure, existingAuditRule.AuditFlags);
        }

        [Fact]
        public void RemoveAuditRuleSpecific_NoMatchingRules_Succeeds()
        {
            var auditRuleReadWrite = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
              FileSystemRights.Write | FileSystemRights.Read, AuditFlags.Success);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleReadWrite);
            fileSecurity.RemoveAuditRuleSpecific(new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
              FileSystemRights.Write, AuditFlags.Success));
            AuthorizationRuleCollection rules =
              fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var existingRule = (FileSystemAuditRule)rules[0];
            Assert.Equal(FileSystemRights.Write | FileSystemRights.Read, existingRule.FileSystemRights);
        }

        [Fact]
        public void RemoveAuditRuleAll_Succeeds()
        {
            var auditRuleAppend = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM", FileSystemRights.AppendData,
                AuditFlags.Success);
            var auditRuleWrite = new FileSystemAuditRule(@"NT AUTHORITY\SYSTEM",
                FileSystemRights.Write, AuditFlags.Success);
            var auditRuleNetworkService = new FileSystemAuditRule(@"NT AUTHORITY\Network Service",
                FileSystemRights.Read, AuditFlags.Failure);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleAppend);
            fileSecurity.AddAuditRule(auditRuleNetworkService);
            fileSecurity.RemoveAuditRuleAll(auditRuleWrite);
            AuthorizationRuleCollection rules =
                fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(1, rules.Count);
            var existingAuditRule = (FileSystemAuditRule)rules[0];
            Assert.Equal(new NTAccount(@"NT AUTHORITY\Network Service"), existingAuditRule.IdentityReference);
            Assert.Equal(FileSystemRights.Read, existingAuditRule.FileSystemRights);
            Assert.Equal(AuditFlags.Failure, existingAuditRule.AuditFlags);
        }

        [Fact]
        public void AuditRuleType_Returns_Valid_Object()
        {
            var fileSecurity = new FileSecurity();
            Type type = fileSecurity.AuditRuleType;
            Assert.Equal(typeof(FileSystemAuditRule), type);
        }
    }
}
