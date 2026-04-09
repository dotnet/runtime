// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl
{
    public class FileSystemAuditRuleTests
    {
        [Fact]
        public void ObjectInitialization_IdentityReference_FileSystemRights_AuditFlags_Success()
        {
            var auditRule = new FileSystemAuditRule(Helpers.s_WorldSidNTAccount, FileSystemRights.ReadData, AuditFlags.Failure);
            Assert.Equal(auditRule.IdentityReference, Helpers.s_WorldSidNTAccount);
            Assert.Equal(FileSystemRights.ReadData, auditRule.FileSystemRights);
            Assert.Equal(AuditFlags.Failure, auditRule.AuditFlags);
        }

        [Fact]
        public void ObjectInitialization_Identity_FileSystemRights_AuditFlags_InheritanceFlag_PropagationFlag_Success()
        {
            var auditRule = new FileSystemAuditRule(@"MYDOMAIN\MyAccount", FileSystemRights.ReadData,
                    InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AuditFlags.Failure);
            Assert.Equal(PropagationFlags.InheritOnly, auditRule.PropagationFlags);
            Assert.Equal(InheritanceFlags.ObjectInherit, auditRule.InheritanceFlags);
        }

        [Fact]
        public void ObjectInitialization_Identity_FileSystemRights_AuditFlags_Success()
        {
            var auditRule = new FileSystemAuditRule(@"MYDOMAIN\MyAccount", FileSystemRights.ReadData, AuditFlags.Failure);
            Assert.Equal(FileSystemRights.ReadData, auditRule.FileSystemRights);
            Assert.Equal(AuditFlags.Failure, auditRule.AuditFlags);
            Assert.Equal(@"MYDOMAIN\MyAccount", auditRule.IdentityReference.Value);
        }

        [Fact]
        public void ObjectInitialization_InvalidFileSystemRights()
        {
            var fileSystemRights = (FileSystemRights)(-1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("fileSystemRights", () => new FileSystemAuditRule(@"MYDOMAIN\MyAccount", fileSystemRights, AuditFlags.Failure));
        }


        [Fact]
        public void FileSystemRights_ReturnValidObject()
        {
            var auditRule = new FileSystemAuditRule(@"MYDOMAIN\MyAccount", FileSystemRights.ReadData, AuditFlags.Failure);
            FileSystemRights fileSystemRights = auditRule.FileSystemRights;
            Assert.Equal(FileSystemRights.ReadData, fileSystemRights);
        }
    }
}
