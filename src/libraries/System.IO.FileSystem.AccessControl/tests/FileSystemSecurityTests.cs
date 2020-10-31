// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
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
            var accessRule = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.AppendData, AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var actualAddedRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), actualAddedRule.IdentityReference);
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
            var accessRuleRead = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read, AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleRead);
            var accessRuleWrite = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
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
            var accessRuleRead = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read, AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleRead);

            var newAccessRule = new FileSystemAccessRule(Helpers.s_NetworkServiceNTAccount,
                FileSystemRights.Write, AccessControlType.Allow);
            fileSecurity.SetAccessRule(newAccessRule);

            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(2, rules.Count);
            var existingAccessRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), existingAccessRule.IdentityReference);
            existingAccessRule = (FileSystemAccessRule)rules[1];
            Assert.Equal(Helpers.s_NetworkServiceNTAccount, existingAccessRule.IdentityReference);
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
            var accessRuleRead = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read, AccessControlType.Allow);
            var accessRuleAppendData = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.AppendData, AccessControlType.Deny);
            var accessRuleWrite = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
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
            var accessRule = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            AuthorizationRuleCollection rules =
               fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            //Removing the "write" access right.
            Assert.True(fileSecurity.RemoveAccessRule(new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
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
            var accessRule = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            Assert.True(fileSecurity.RemoveAccessRule(new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                 FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow)));
            var rules = fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(0, rules.Count);
        }

        [Fact]
        public void RemoveAccessRule_NoMatchableRules_Succeeds()
        {
            var accessRuleAppendData = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount, FileSystemRights.AppendData,
                AccessControlType.Allow);
            var accessRuleWrite = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Write, AccessControlType.Deny);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRuleAppendData);
            Assert.True(fileSecurity.RemoveAccessRule(accessRuleWrite));
            AuthorizationRuleCollection rules =
                fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var remainingRule = (FileSystemAccessRule)rules[0];
            Assert.Equal(Helpers.s_LocalSystemNTAccount, accessRuleAppendData.IdentityReference);
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
            var accessRuleReadWrite = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read | FileSystemRights.Write,
                AccessControlType.Allow);
            var accessRuleWrite = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
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
            var accessRule = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount, FileSystemRights.AppendData
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
            var accessRuleAppendData = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount, FileSystemRights.AppendData,
                AccessControlType.Allow);
            var accessRuleRead = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read, AccessControlType.Allow);
            var accessRuleWrite = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Write, AccessControlType.Allow);
            var accessRuleReadPermissionDeny = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount,
              FileSystemRights.ReadPermissions, AccessControlType.Deny);
            var accessRuleReadNetworkService = new FileSystemAccessRule(Helpers.s_NetworkServiceNTAccount,
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
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), existingAccessRule.IdentityReference);
            Assert.Equal(AccessControlType.Deny, existingAccessRule.AccessControlType);
            Assert.Equal(FileSystemRights.ReadPermissions, existingAccessRule.FileSystemRights);
            existingAccessRule = (FileSystemAccessRule)rules[1];
            Assert.Equal(Helpers.s_NetworkServiceNTAccount, existingAccessRule.IdentityReference);
            Assert.Equal(AccessControlType.Allow, existingAccessRule.AccessControlType);
        }

        [Fact]
        public void AccessRuleType_Returns_Valid_Object()
        {
            var accessRule = new FileSystemAccessRule(Helpers.s_LocalSystemNTAccount, FileSystemRights.AppendData,
             AccessControlType.Allow);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAccessRule(accessRule);
            Type accessRuleType = fileSecurity.AccessRuleType;
            Assert.Equal(typeof(FileSystemAccessRule), accessRuleType);
        }

        [Fact]
        public void AddAuditRule_Succeeds()
        {
            var auditRule = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.AppendData, AuditFlags.Success);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRule);
            AuthorizationRuleCollection auditRules =
                fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, auditRules.Count);
            var actualAddedRule = (FileSystemAuditRule)auditRules[0];
            Assert.Equal(Helpers.s_LocalSystemNTAccount, actualAddedRule.IdentityReference);
            Assert.Equal(AuditFlags.Success, actualAddedRule.AuditFlags);
            Assert.Equal(FileSystemRights.AppendData, actualAddedRule.FileSystemRights);
        }

        [Fact]
        public void SetAuditRule_Succeeds()
        {
            var auditRuleAppendData = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.AppendData, AuditFlags.Success);
            var auditRuleNetworkService = new FileSystemAuditRule(Helpers.s_NetworkServiceNTAccount,
                FileSystemRights.CreateFiles, AuditFlags.Failure);
            var auditRuleDelete = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Delete, AuditFlags.Success);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleNetworkService);
            fileSecurity.AddAuditRule(auditRuleAppendData);
            fileSecurity.SetAuditRule(auditRuleDelete);
            var auditRules = fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(2, auditRules.Count);
            var firstAuditRule = (FileSystemAuditRule)auditRules[0];
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), firstAuditRule.IdentityReference);
            Assert.Equal(AuditFlags.Success, firstAuditRule.AuditFlags);
            Assert.Equal(FileSystemRights.Delete, firstAuditRule.FileSystemRights);
            var secondAuditRule = (FileSystemAuditRule)auditRules[1];
            Assert.Equal(Helpers.s_NetworkServiceNTAccount, secondAuditRule.IdentityReference);
            Assert.Equal(AuditFlags.Failure, secondAuditRule.AuditFlags);
            Assert.Equal(FileSystemRights.CreateFiles, secondAuditRule.FileSystemRights);
        }

        [Fact]
        public void RemoveAuditRule_Succeeds()
        {
            var auditRule = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Read | FileSystemRights.Write,
                AuditFlags.Failure);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRule);
            AuthorizationRuleCollection rules =
               fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            Assert.True(fileSecurity.RemoveAuditRule(new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Write, AuditFlags.Failure)));

            rules = fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var existingRule = (FileSystemAuditRule)rules[0];
            Assert.Equal(FileSystemRights.Read, existingRule.FileSystemRights);
            Assert.Equal(AuditFlags.Failure, existingRule.AuditFlags);
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), existingRule.IdentityReference);
        }

        [Fact]
        public void RemoveAuditRuleSpecific_Succeeds()
        {
            var auditRuleReadWrite = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
               FileSystemRights.Write | FileSystemRights.Read, AuditFlags.Success);
            var auditRuleNetworkService = new FileSystemAuditRule(Helpers.s_NetworkServiceNTAccount,
                FileSystemRights.Read, AuditFlags.Failure);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleReadWrite);
            fileSecurity.AddAuditRule(auditRuleNetworkService);
            fileSecurity.RemoveAuditRuleSpecific(auditRuleReadWrite);
            AuthorizationRuleCollection rules =
              fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            Assert.Equal(1, rules.Count);
            var existingAuditRule = (FileSystemAuditRule)rules[0];
            Assert.Equal(Helpers.s_NetworkServiceNTAccount, existingAuditRule.IdentityReference);
            Assert.Equal(FileSystemRights.Read, existingAuditRule.FileSystemRights);
            Assert.Equal(AuditFlags.Failure, existingAuditRule.AuditFlags);
        }

        [Fact]
        public void RemoveAuditRuleSpecific_NoMatchingRules_Succeeds()
        {
            var auditRuleReadWrite = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
              FileSystemRights.Write | FileSystemRights.Read, AuditFlags.Success);
            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleReadWrite);
            fileSecurity.RemoveAuditRuleSpecific(new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
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
            var auditRuleAppend = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount, FileSystemRights.AppendData,
                AuditFlags.Success);
            var auditRuleWrite = new FileSystemAuditRule(Helpers.s_LocalSystemNTAccount,
                FileSystemRights.Write, AuditFlags.Success);
            var auditRuleNetworkService = new FileSystemAuditRule(Helpers.s_NetworkServiceNTAccount,
                FileSystemRights.Read, AuditFlags.Failure);

            var fileSecurity = new FileSecurity();
            fileSecurity.AddAuditRule(auditRuleAppend);
            fileSecurity.AddAuditRule(auditRuleNetworkService);
            fileSecurity.RemoveAuditRuleAll(auditRuleWrite);
            AuthorizationRuleCollection rules =
                fileSecurity.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            Assert.Equal(1, rules.Count);
            var existingAuditRule = (FileSystemAuditRule)rules[0];
            Assert.Equal(Helpers.s_NetworkServiceNTAccount, existingAuditRule.IdentityReference);
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

        [Fact]
        public void DirNotFound_DirectorySecurity()
        {
            using TempDirectory tempDirectory = new TempDirectory();
            string longDir = CreateLongDirectory(tempDirectory.Path, create: false);

            // A non-existent directory path is considered:
            // - In NetFX: an invalid name because paths that are too long are not correctly handled
            // - NetCore: a valid name because long paths are correctly handled, and non-existent, as expected
            AssertExtensions.Throws<DirectoryNotFoundException, ArgumentException>(() =>
            {
                new DirectorySecurity(longDir, AccessControlSections.Owner);
            });
            
        }

        [Fact]
        public void FileNotFound_FileSecurity()
        {
            using TempDirectory tempDirectory = new TempDirectory();
            string longDir = CreateLongDirectory(tempDirectory.Path, create: false);
            string filePath = Path.Combine(longDir, "file.txt");

            // A non-existent file path is considered:
            // - In NetFX: an invalid name because paths that are too long are not correctly handled
            // - NetCore: a valid name because long paths are correctly handled, and non-existent, as expected
            AssertExtensions.Throws<FileNotFoundException, ArgumentException>(() =>
            {
                new FileSecurity(filePath, AccessControlSections.Owner);
            });
        }

        // Attempting to create a directory with a very long path in .NET Framework fails with DirectoryNotFoundException
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Fact]
        public void MaxLengthPath_DirectorySecurity()
        {
            using TempDirectory tempDirectory = new TempDirectory();
            string longDir = CreateLongDirectory(tempDirectory.Path);
            var security = new DirectorySecurity(longDir, AccessControlSections.Owner);
            Assert.NotNull(security);
        }

        // Attempting to create a directory with a very long path in .NET Framework fails with DirectoryNotFoundException
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Fact]
        public void MaxLengthPath_FileSecurity()
        {
            using TempDirectory tempDirectory = new TempDirectory();
            string longDir = CreateLongDirectory(tempDirectory.Path);
            using TempFile tempFile = new TempFile(Path.Combine(longDir, "file.txt"));
            var security = new FileSecurity(tempFile.Path, AccessControlSections.Owner);
            Assert.NotNull(security);
        }

        private string CreateLongDirectory(string basePath, bool create = true)
        {
            string name = TempDirectory.GetMaxLengthRandomName();

            string fullPath = Path.Combine(basePath, name, name, name, name, name);

            if (create)
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }
    }
}
