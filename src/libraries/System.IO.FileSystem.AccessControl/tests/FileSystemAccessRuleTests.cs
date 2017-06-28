// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO
{
    public class FileSystemAccessRuleTests
    {
        [Fact]
        public void FileSystemAccessRule_Returns_Valid_Object()
        {
            var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var accessRule = new FileSystemAccessRule(identity, FileSystemRights.AppendData, AccessControlType.Allow);
            var expectedFileSystemRights = FileSystemRights.AppendData | FileSystemRights.Synchronize;
            Assert.Equal(identity, accessRule.IdentityReference);
            Assert.Equal(expectedFileSystemRights, accessRule.FileSystemRights);
            Assert.Equal(AccessControlType.Allow, accessRule.AccessControlType);
            Assert.Equal(PropagationFlags.None, accessRule.PropagationFlags);
            Assert.Equal(InheritanceFlags.None, accessRule.InheritanceFlags);
        }

        [Fact]
        public void FileSystemAccessRule_InvalidFileSystemRights()
        {
            var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("fileSystemRights", () =>
            new FileSystemAccessRule(identity, (FileSystemRights)(-1), AccessControlType.Allow));
        }

        [Fact]
        public void FileSystemAccessRule_AcessControlTypeDeny_Returns_Valid_Object()
        {
            var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var accessRule = new FileSystemAccessRule(identity, FileSystemRights.AppendData, AccessControlType.Deny);
            var expectedFileSystemRights = FileSystemRights.AppendData & ~FileSystemRights.Synchronize;
            Assert.Equal(expectedFileSystemRights, accessRule.FileSystemRights);
            Assert.Equal(AccessControlType.Deny, accessRule.AccessControlType);
        }

        [Fact]
        public void FileSystemAccessRule_FileSystemRightsFullControl_Returns_Valid_Object()
        {
            var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var accessRule = new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Deny);
            Assert.Equal(FileSystemRights.FullControl, accessRule.FileSystemRights);
            Assert.Equal(AccessControlType.Deny, accessRule.AccessControlType);
        }

        [Fact]
        public void FileSystemAccessRule_IdentityAsString_Returns_Valid_Object()
        {
            var accessRule = new FileSystemAccessRule(@"MYDOMAIN\MyAccount", FileSystemRights.AppendData,
                AccessControlType.Allow);
            var expectedFileSystemRights = FileSystemRights.AppendData | FileSystemRights.Synchronize;
            Assert.Equal(new NTAccount(@"MYDOMAIN\MyAccount"), accessRule.IdentityReference);
            Assert.Equal(expectedFileSystemRights, accessRule.FileSystemRights);
            Assert.Equal(AccessControlType.Allow, accessRule.AccessControlType);
            Assert.Equal(PropagationFlags.None, accessRule.PropagationFlags);
            Assert.Equal(InheritanceFlags.None, accessRule.InheritanceFlags);
        }

        [Fact]
        public void FileSystemAccessRule_InhertianceFlag_PropagationFlag_Returns_Valid_Object()
        {
            var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var accessRule = new FileSystemAccessRule(identity, FileSystemRights.AppendData,
                InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit,
                AccessControlType.Allow);

            Assert.Equal(PropagationFlags.NoPropagateInherit, accessRule.PropagationFlags);
            Assert.Equal(InheritanceFlags.ContainerInherit, accessRule.InheritanceFlags);
        }

        [Fact]
        public void FileSystemAccessRule_InhertianceFlag_PropagationFlag_IdentityAsString_Returns_Valid_Object()
        {
            var accessRule = new FileSystemAccessRule(@"MYDOMAIN\MyAccount", FileSystemRights.AppendData,
                InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly,
                AccessControlType.Allow);

            Assert.Equal(new NTAccount(@"MYDOMAIN\MyAccount"), accessRule.IdentityReference);
            Assert.Equal(PropagationFlags.InheritOnly, accessRule.PropagationFlags);
            Assert.Equal(InheritanceFlags.ObjectInherit, accessRule.InheritanceFlags);
        }
    }
}
