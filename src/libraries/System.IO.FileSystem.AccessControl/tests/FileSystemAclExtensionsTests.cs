// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.VisualBasic;
using Xunit;

namespace System.IO
{
    public class FileSystemAclExtensionsTests
    {
        #region Test methods

        [Fact]
        public void GetAccessControl_DirectoryInfo_InvalidArguments()
        {
            Assert.Throws<NullReferenceException>(() => FileSystemAclExtensions.GetAccessControl((DirectoryInfo)null));
        }

        [Fact]
        public void GetAccessControl_DirectoryInfo_ReturnsValidObject()
        {
            using (var directory = new TempDirectory())
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);

                DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();

                Assert.NotNull(directorySecurity);
                Assert.Equal(typeof(FileSystemRights), directorySecurity.AccessRightType);
            }
        }

        [Fact]
        public void GetAccessControl_DirectoryInfo_AccessControlSections_InvalidArguments()
        {
            Assert.Throws<NullReferenceException>(() => FileSystemAclExtensions.GetAccessControl((DirectoryInfo)null, new AccessControlSections()));
        }

        [Fact]
        public void GetAccessControl_DirectoryInfo_AccessControlSections_ReturnsValidObject()
        {
            using (var directory = new TempDirectory())
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
                AccessControlSections accessControlSections = new AccessControlSections();

                DirectorySecurity directorySecurity = directoryInfo.GetAccessControl(accessControlSections);

                Assert.NotNull(directorySecurity);
                Assert.Equal(typeof(FileSystemRights), directorySecurity.AccessRightType);
            }
        }

        [Fact]
        public void GetAccessControl_FileInfo_InvalidArguments()
        {
            Assert.Throws<NullReferenceException>(() => FileSystemAclExtensions.GetAccessControl((FileInfo)null));
        }

        [Fact]
        public void GetAccessControl_FileInfo_ReturnsValidObject()
        {
            using (var directory = new TempDirectory())
            using (var file = new TempFile(Path.Combine(directory.Path, "file.txt")))
            {
                FileInfo fileInfo = new FileInfo(file.Path);

                FileSecurity fileSecurity = fileInfo.GetAccessControl();

                Assert.NotNull(fileSecurity);
                Assert.Equal(typeof(FileSystemRights), fileSecurity.AccessRightType);
            }
        }

        [Fact]
        public void GetAccessControl_FileInfo_AccessControlSections_InvalidArguments()
        {
            Assert.Throws<NullReferenceException>(() => FileSystemAclExtensions.GetAccessControl((FileInfo)null, new AccessControlSections()));
        }

        [Fact]
        public void GetAccessControl_FileInfo_AccessControlSections_ReturnsValidObject()
        {
            using (var directory = new TempDirectory())
            using (var file = new TempFile(Path.Combine(directory.Path, "file.txt")))
            {
                FileInfo fileInfo = new FileInfo(file.Path);
                AccessControlSections accessControlSections = new AccessControlSections();

                FileSecurity fileSecurity = fileInfo.GetAccessControl(accessControlSections);

                Assert.NotNull(fileSecurity);
                Assert.Equal(typeof(FileSystemRights), fileSecurity.AccessRightType);
            }
        }

        [Fact]
        public void GetAccessControl_Filestream_InvalidArguments()
        {
            Assert.Throws<NullReferenceException>(() => FileSystemAclExtensions.GetAccessControl((FileStream)null));
        }

        [Fact]
        public void GetAccessControl_Filestream_ReturnValidObject()
        {
            using (var directory = new TempDirectory())
            using (var file = new TempFile(Path.Combine(directory.Path, "file.txt")))
            using (FileStream fileStream = File.Open(file.Path, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                FileSecurity fileSecurity = FileSystemAclExtensions.GetAccessControl(fileStream);
                Assert.NotNull(fileSecurity);
                Assert.Equal(typeof(FileSystemRights), fileSecurity.AccessRightType);
            }
        }

        [Fact]
        public void SetAccessControl_DirectoryInfo_DirectorySecurity_InvalidArguments()
        {
            using (var directory = new TempDirectory())
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
                AssertExtensions.Throws<ArgumentNullException>("directorySecurity", () => directoryInfo.SetAccessControl((DirectorySecurity)null));
            }
        }

        [Fact]
        public void SetAccessControl_DirectoryInfo_DirectorySecurity_Success()
        {
            using (var directory = new TempDirectory())
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
                DirectorySecurity directorySecurity = new DirectorySecurity();

                directoryInfo.SetAccessControl(directorySecurity);
            }
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_InvalidArguments()
        {
            using (var directory = new TempDirectory())
            using (var file = new TempFile(Path.Combine(directory.Path, "file.txt")))
            {
                FileInfo fileInfo = new FileInfo(file.Path);
                AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => fileInfo.SetAccessControl((FileSecurity)null));
            }
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_Success()
        {
            using (var directory = new TempDirectory())
            using (var file = new TempFile(Path.Combine(directory.Path, "file.txt")))
            {
                FileInfo fileInfo = new FileInfo(file.Path);
                FileSecurity fileSecurity = new FileSecurity();

                fileInfo.SetAccessControl(fileSecurity);
            }
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_InvalidArguments()
        {
            Assert.Throws<NullReferenceException>(() => FileSystemAclExtensions.SetAccessControl((FileStream)null, (FileSecurity)null));
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_InvalidFileSecurityObject()
        {
            using (var directory = new TempDirectory())
            using (var file = new TempFile(Path.Combine(directory.Path, "file.txt")))
            using (FileStream fileStream = File.Open(file.Path, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => FileSystemAclExtensions.SetAccessControl(fileStream, (FileSecurity)null));
            }
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_Success()
        {
            using (var directory = new TempDirectory())
            using (var file = new TempFile(Path.Combine(directory.Path, "file.txt")))
            using (FileStream fileStream = File.Open(file.Path, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                FileSecurity fileSecurity = new FileSecurity();
                FileSystemAclExtensions.SetAccessControl(fileStream, fileSecurity);
            }
        }

        [Fact]
        public void DirectoryInfo_Create_NullDirectoryInfo()
        {
            DirectoryInfo info = null;
            DirectorySecurity security = new DirectorySecurity();

            if (PlatformDetection.IsFullFramework)
            {
                Assert.Throws<ArgumentNullException>(() => FileSystemAclExtensions.Create(info, security));
            }
            else
            {
                Assert.Throws<ArgumentNullException>(() => info.Create(security));
            }
        }

        [Fact]
        public void DirectoryInfo_Create_DefaultDirectorySecurity()
        {
            DirectorySecurity security = new DirectorySecurity();
            VerifyDirectorySecurity(security);
        }

        [Fact]
        public void DirectoryInfo_Create_NullDirectorySecurity()
        {
            DirectoryInfo info = new DirectoryInfo("path");
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Throws<ArgumentNullException>(() => FileSystemAclExtensions.Create(info, null));
            }
            else
            {
                Assert.Throws<ArgumentNullException>(() => info.Create(null));
            }
        }

        [Fact]
        public void DirectoryInfo_Create_NotFound()
        {
            DirectoryInfo info = new DirectoryInfo(@"W:\\I\\Do\\Not\\Exist");
            DirectorySecurity security = new DirectorySecurity();
            Assert.Throws<DirectoryNotFoundException>(() => info.Create(security));
        }

        [Theory]
        [InlineData(WellKnownSidType.BuiltinUsersSid, FileSystemRights.FullControl, AccessControlType.Allow)]
        [InlineData(WellKnownSidType.BuiltinUsersSid, FileSystemRights.ReadData, AccessControlType.Allow)]
        [InlineData(WellKnownSidType.BuiltinUsersSid, FileSystemRights.Write, AccessControlType.Allow)]
        [InlineData(WellKnownSidType.BuiltinUsersSid, FileSystemRights.Write, AccessControlType.Deny)]
        [InlineData(WellKnownSidType.BuiltinUsersSid, FileSystemRights.FullControl, AccessControlType.Deny)]
        public void DirectoryInfo_Create_DirectorySecurityWithSpecificAccessRule(
            WellKnownSidType sid,
            FileSystemRights rights,
            AccessControlType controlType)
        {

            DirectorySecurity security = GetDirectorySecurity(sid, rights, controlType);
            VerifyDirectorySecurity(security);
        }

        #endregion

        #region Helper methods

        private DirectorySecurity GetDirectorySecurity(WellKnownSidType sid, FileSystemRights rights, AccessControlType controlType)
        {
            DirectorySecurity security = new DirectorySecurity();

            SecurityIdentifier identity = new SecurityIdentifier(sid, null);
            FileSystemAccessRule accessRule = new FileSystemAccessRule(identity, rights, controlType);
            security.AddAccessRule(accessRule);

            return security;
        }

        private void VerifyDirectorySecurity(DirectorySecurity expectedSecurity)
        {
            using var directory = new TempDirectory();

            string path = Path.Combine(directory.Path, "directory");
            DirectoryInfo info = new DirectoryInfo(path);

            info.Create(expectedSecurity);

            Assert.True(Directory.Exists(path));
            Assert.Equal(typeof(FileSystemRights), expectedSecurity.AccessRightType);

            DirectoryInfo actualInfo = new DirectoryInfo(info.FullName);

            DirectorySecurity actualSecurity = actualInfo.GetAccessControl();

            VerifyDirectoryAccessSecurity(expectedSecurity, actualSecurity);
        }

        private void VerifyDirectoryAccessSecurity(DirectorySecurity expectedSecurity, DirectorySecurity actualSecurity)
        {
            Assert.Equal(typeof(FileSystemRights), actualSecurity.AccessRightType);

            List<FileSystemAccessRule> expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>().ToList();

            List<FileSystemAccessRule> actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>().ToList();

            // If DirectorySecurity is created without arguments, GetAccessRules will return zero rules
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

        private bool AreAccessRulesEqual(FileSystemAccessRule expectedRule, FileSystemAccessRule actualRule)
        {
            return
                expectedRule.AccessControlType == actualRule.AccessControlType &&
                expectedRule.FileSystemRights  == actualRule.FileSystemRights &&
                expectedRule.InheritanceFlags  == actualRule.InheritanceFlags &&
                expectedRule.PropagationFlags  == actualRule.PropagationFlags;
        }

        #endregion
    }
}
