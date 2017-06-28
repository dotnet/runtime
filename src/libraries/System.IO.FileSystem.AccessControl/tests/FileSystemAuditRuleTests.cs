// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl
{
    public class FileSystemAuditRuleTests
    {
        [Fact]
        public void ObjectInitialization_IdentityReference_FileSystemRights_AuditFlags_Success()
        {
            var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var auditRule = new FileSystemAuditRule(identity, FileSystemRights.ReadData, AuditFlags.Failure);
            Assert.Equal(auditRule.IdentityReference, identity);
            Assert.Equal(auditRule.FileSystemRights, FileSystemRights.ReadData);
            Assert.Equal(auditRule.AuditFlags, AuditFlags.Failure);
        }

        [Fact]
        public void ObjectInitialization_Identity_FileSystemRights_AuditFlags_InheritanceFlag_PropagationFlag_Success()
        {
            var auditRule = new FileSystemAuditRule(@"MYDOMAIN\MyAccount", FileSystemRights.ReadData,
                    InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AuditFlags.Failure);
            Assert.Equal(auditRule.PropagationFlags, PropagationFlags.InheritOnly);
            Assert.Equal(auditRule.InheritanceFlags, InheritanceFlags.ObjectInherit);
        }

        [Fact]
        public void ObjectInitialization_Identity_FileSystemRights_AuditFlags_Success()
        {
            var auditRule = new FileSystemAuditRule(@"MYDOMAIN\MyAccount", FileSystemRights.ReadData, AuditFlags.Failure);
            Assert.Equal(auditRule.FileSystemRights, FileSystemRights.ReadData);
            Assert.Equal(auditRule.AuditFlags, AuditFlags.Failure);
            Assert.Equal(auditRule.IdentityReference.Value, @"MYDOMAIN\MyAccount");
        }

        [Fact]
        public void ObjectInitialization_InvalidFileSystemRights()
        {
            var fileSystemRights = (FileSystemRights)(-1);
            var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("fileSystemRights", () => new FileSystemAuditRule(@"MYDOMAIN\MyAccount", fileSystemRights, AuditFlags.Failure));
        }


        [Fact]
        public void FileSystemRights_ReturnValidObject()
        {
            var auditRule = new FileSystemAuditRule(@"MYDOMAIN\MyAccount", FileSystemRights.ReadData, AuditFlags.Failure);
            FileSystemRights fileSystemRights = auditRule.FileSystemRights;
            Assert.Equal(fileSystemRights, FileSystemRights.ReadData);
        }
    }
}
