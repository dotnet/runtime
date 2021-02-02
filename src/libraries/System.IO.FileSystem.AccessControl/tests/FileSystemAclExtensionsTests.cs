// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.IO
{
    public class FileSystemAclExtensionsTests
    {
        private const int DefaultBufferSize = 4096;


        #region Test methods

        #region GetAccessControl

        [Fact]
        public void GetAccessControl_DirectoryInfo_InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => FileSystemAclExtensions.GetAccessControl((DirectoryInfo)null));
        }

        [Fact]
        public void GetAccessControl_DirectoryInfo_ReturnsValidObject()
        {
            using var directory = new TempAclDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl(AccessControlSections.Access);
            Assert.NotNull(directorySecurity);
            Assert.Equal(typeof(FileSystemRights), directorySecurity.AccessRightType);
        }

        [Fact]
        public void GetAccessControl_DirectoryInfo_AccessControlSections_InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => FileSystemAclExtensions.GetAccessControl((DirectoryInfo)null, new AccessControlSections()));
        }

        [Fact]
        public void GetAccessControl_DirectoryInfo_AccessControlSections_ReturnsValidObject()
        {
            using var directory = new TempAclDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
            AccessControlSections accessControlSections = new AccessControlSections();
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl(accessControlSections);
            Assert.NotNull(directorySecurity);
            Assert.Equal(typeof(FileSystemRights), directorySecurity.AccessRightType);
        }

        [Fact]
        public void GetAccessControl_FileInfo_InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => FileSystemAclExtensions.GetAccessControl((FileInfo)null));
        }

        [Fact]
        public void GetAccessControl_FileInfo_ReturnsValidObject()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            FileInfo fileInfo = new FileInfo(file.Path);
            FileSecurity fileSecurity = fileInfo.GetAccessControl(AccessControlSections.Access);
            Assert.NotNull(fileSecurity);
            Assert.Equal(typeof(FileSystemRights), fileSecurity.AccessRightType);
        }

        [Fact]
        public void GetAccessControl_FileInfo_AccessControlSections_InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => FileSystemAclExtensions.GetAccessControl((FileInfo)null, new AccessControlSections()));
        }

        [Fact]
        public void GetAccessControl_FileInfo_AccessControlSections_ReturnsValidObject()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            FileInfo fileInfo = new FileInfo(file.Path);
            AccessControlSections accessControlSections = new AccessControlSections();
            FileSecurity fileSecurity = fileInfo.GetAccessControl(accessControlSections);
            Assert.NotNull(fileSecurity);
            Assert.Equal(typeof(FileSystemRights), fileSecurity.AccessRightType);
        }

        [Fact]
        public void GetAccessControl_Filestream_InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>("fileStream", () => FileSystemAclExtensions.GetAccessControl((FileStream)null));
        }

        [Fact]
        public void GetAccessControl_Filestream_ReturnValidObject()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            using FileStream fileStream = File.Open(file.Path, FileMode.Open, FileAccess.Write, FileShare.Delete);
            FileSecurity fileSecurity = FileSystemAclExtensions.GetAccessControl(fileStream);
            Assert.NotNull(fileSecurity);
            Assert.Equal(typeof(FileSystemRights), fileSecurity.AccessRightType);
        }

        #endregion

        #region SetAccessControl

        [Fact]
        public void SetAccessControl_DirectoryInfo_DirectorySecurity_InvalidArguments()
        {
            using var directory = new TempAclDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
            AssertExtensions.Throws<ArgumentNullException>("directorySecurity", () => directoryInfo.SetAccessControl(directorySecurity: null));
        }

        [Fact]
        public void SetAccessControl_DirectoryInfo_DirectorySecurity_Success()
        {
            using var directory = new TempAclDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
            DirectorySecurity directorySecurity = new DirectorySecurity();
            directoryInfo.SetAccessControl(directorySecurity);
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_InvalidArguments()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            FileInfo fileInfo = new FileInfo(file.Path);
            AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => fileInfo.SetAccessControl(fileSecurity: null));
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_Success()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            FileInfo fileInfo = new FileInfo(file.Path);
            FileSecurity fileSecurity = new FileSecurity();
            fileInfo.SetAccessControl(fileSecurity);
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>("fileStream", () => FileSystemAclExtensions.SetAccessControl((FileStream)null, fileSecurity: null));
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_InvalidFileSecurityObject()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            using FileStream fileStream = File.Open(file.Path, FileMode.Open, FileAccess.Write, FileShare.Delete);
            AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => FileSystemAclExtensions.SetAccessControl(fileStream, fileSecurity: null));
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_Success()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            using FileStream fileStream = File.Open(file.Path, FileMode.Open, FileAccess.Write, FileShare.Delete);
            FileSecurity fileSecurity = new FileSecurity();
            FileSystemAclExtensions.SetAccessControl(fileStream, fileSecurity);
        }

        #endregion

        #region DirectoryInfo Create

        [Fact]
        public void DirectoryInfo_Create_NullDirectoryInfo()
        {
            DirectoryInfo info = null;
            DirectorySecurity security = new DirectorySecurity();
            Assert.Throws<ArgumentNullException>("directoryInfo", () => DirectoryInfo_Create_Framework(info, security));
        }

        [Fact]
        public void DirectoryInfo_Create_NullDirectorySecurity()
        {
            DirectoryInfo info = new DirectoryInfo("path");
            Assert.Throws<ArgumentNullException>("directorySecurity", () => DirectoryInfo_Create_Framework(info, null));
        }

        [Fact]
        public void DirectoryInfo_Create_NotFound()
        {
            var directory = new TempAclDirectory();
            string path = Path.Combine(directory.Path, "ParentDoesNotExist");
            directory.Dispose(); // Delete parent folder

            DirectoryInfo info = new DirectoryInfo(path);
            DirectorySecurity security = GetDirectorySecurity(FileSystemRights.FullControl);
            DirectoryInfo_Create_Framework(info, security);
        }

        private void DirectoryInfo_Create_Framework(DirectoryInfo info, DirectorySecurity security)
        {
            if (PlatformDetection.IsNetFramework)
            {
                FileSystemAclExtensions.Create(info, security);
            }
            else
            {
                info.Create(security);
            }
        }

        [Theory]
        // Must have at least one Read, otherwise the TempAclDirectory will fail to delete that item on dispose
        [InlineData(FileSystemRights.FullControl)]
        [InlineData(FileSystemRights.Read)]
        [InlineData(FileSystemRights.Read | FileSystemRights.Write)]
        [InlineData(FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.ExecuteFile)]
        [InlineData(FileSystemRights.ReadAndExecute)]
        public void DirectoryInfo_Create_DirectorySecurityWithSpecificAccessRule(FileSystemRights rights)
        {
            using var directory = new TempAclDirectory();
            string path = Path.Combine(directory.Path, "directory");
            DirectoryInfo info = new DirectoryInfo(path);

            DirectorySecurity expectedSecurity = GetDirectorySecurity(rights);

            info.Create(expectedSecurity);

            Assert.True(Directory.Exists(path));

            DirectoryInfo actualInfo = new DirectoryInfo(info.FullName);

            DirectorySecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        [Theory]
        [InlineData(FileSystemRights.TakeOwnership)]
        [InlineData(FileSystemRights.Write)]
        public void DirectoryInfo_Create_MultipleAddAccessRules(FileSystemRights rightsToDeny)
        {
            var expectedSecurity = new DirectorySecurity();

            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            var allowAccessRule = new FileSystemAccessRule(identity, FileSystemRights.Read, AccessControlType.Allow);
            expectedSecurity.AddAccessRule(allowAccessRule);

            var denyAccessRule = new FileSystemAccessRule(identity, rightsToDeny, AccessControlType.Deny);
            expectedSecurity.AddAccessRule(denyAccessRule);

            using var directory = new TempAclDirectory();
            string path = Path.Combine(directory.Path, "directory");
            DirectoryInfo info = new DirectoryInfo(path);

            info.Create(expectedSecurity);

            Assert.True(Directory.Exists(path));

            DirectoryInfo actualInfo = new DirectoryInfo(info.FullName);

            DirectorySecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        #endregion

        #region FileInfo Create

        [Fact]
        public void FileInfo_Create_NullFileInfo()
        {
            FileInfo info = null;
            FileSecurity security = new FileSecurity();

            Assert.Throws<ArgumentNullException>("fileInfo", () =>
                FileInfo_Create_Framework(info, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        [Fact]
        public void FileInfo_Create_NullFileSecurity()
        {
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentNullException>("fileSecurity", () =>
                FileInfo_Create_Framework(info, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, null));
        }

        [Fact]
        public void FileInfo_Create_NotFound()
        {
            using var directory = new TempAclDirectory();
            string path = Path.Combine(directory.Path, Guid.NewGuid().ToString(), "file.txt");
            FileInfo info = new FileInfo(path);
            FileSecurity security = new FileSecurity();

            Assert.Throws<DirectoryNotFoundException>(() =>
                FileInfo_Create_Framework(info, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData((FileMode)int.MinValue)]
        [InlineData((FileMode)0)]
        [InlineData((FileMode)int.MaxValue)]
        public void FileInfo_Create_FileSecurity_InvalidFileMode(FileMode invalidMode)
        {
            FileSecurity security = new FileSecurity();
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("mode", () =>
                FileInfo_Create_Framework(info, invalidMode, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData((FileShare)(-1))]
        [InlineData((FileShare)int.MaxValue)]
        public void FileInfo_Create_FileSecurity_InvalidFileShare(FileShare invalidFileShare)
        {
            FileSecurity security = new FileSecurity();
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("share", () =>
                FileInfo_Create_Framework(info, FileMode.CreateNew, FileSystemRights.WriteData, invalidFileShare, DefaultBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        public void FileInfo_Create_FileSecurity_InvalidBufferSize(int invalidBufferSize)
        {
            FileSecurity security = new FileSecurity();
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () =>
                FileInfo_Create_Framework(info, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, invalidBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData(FileMode.Truncate,  FileSystemRights.Read)]
        [InlineData(FileMode.Truncate,  FileSystemRights.ReadData)]
        [InlineData(FileMode.CreateNew, FileSystemRights.Read)]
        [InlineData(FileMode.CreateNew, FileSystemRights.ReadData)]
        [InlineData(FileMode.Create,    FileSystemRights.Read)]
        [InlineData(FileMode.Create,    FileSystemRights.ReadData)]
        [InlineData(FileMode.Append,    FileSystemRights.Read)]
        [InlineData(FileMode.Append,    FileSystemRights.ReadData)]
        public void FileInfo_Create_FileSecurity_ForbiddenCombo_FileModeFileSystemSecurity(FileMode mode, FileSystemRights rights)
        {
            FileSecurity security = new FileSecurity();
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentException>(() =>
                FileInfo_Create_Framework(info, mode, rights, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        private void FileInfo_Create_Framework(FileInfo info, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity security)
        {
            if (PlatformDetection.IsNetFramework)
            {
                FileSystemAclExtensions.Create(info, mode, rights, share, bufferSize, options, security);
            }
            else
            {
                info.Create(mode, rights, share, bufferSize, options, security);
            }
        }

        [Fact]
        public void FileInfo_Create_FileSecurity_SpecificAccessRule()
        {
            using var directory = new TempAclDirectory();

            string path = Path.Combine(directory.Path, "file.txt");
            FileInfo info = new FileInfo(path);

            FileSecurity expectedSecurity = GetFileSecurity(FileSystemRights.FullControl);

            using FileStream stream = info.Create(
                FileMode.Create,
                FileSystemRights.FullControl,
                FileShare.ReadWrite | FileShare.Delete,
                DefaultBufferSize,
                FileOptions.None,
                expectedSecurity);

            Assert.True(File.Exists(path));

            var actualInfo = new FileInfo(info.FullName);

            FileSecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }


        [Theory]
        [InlineData(FileSystemRights.TakeOwnership)]
        [InlineData(FileSystemRights.Write)]
        public void FileInfo_Create_MultipleAddAccessRules(FileSystemRights rightsToDeny)
        {
            var expectedSecurity = new FileSecurity();

            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            var allowAccessRule = new FileSystemAccessRule(identity, FileSystemRights.Read, AccessControlType.Allow);
            expectedSecurity.AddAccessRule(allowAccessRule);

            var denyAccessRule = new FileSystemAccessRule(identity, rightsToDeny, AccessControlType.Deny);
            expectedSecurity.AddAccessRule(denyAccessRule);

            using var directory = new TempAclDirectory();

            string path = Path.Combine(directory.Path, "file.txt");
            var info = new FileInfo(path);

            using FileStream stream = info.Create(
                FileMode.Create,
                FileSystemRights.FullControl,
                FileShare.ReadWrite | FileShare.Delete,
                DefaultBufferSize,
                FileOptions.None,
                expectedSecurity);

            Assert.True(File.Exists(path));

            var actualInfo = new FileInfo(info.FullName);

            FileSecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        #endregion

        #region DirectorySecurity CreateDirectory

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void DirectorySecurity_CreateDirectory_NullSecurity()
        {
            DirectorySecurity security = null;
            string path = "whatever";

            Assert.Throws<ArgumentNullException>("directorySecurity", () => security.CreateDirectory(path));
            Assert.Throws<ArgumentNullException>("directorySecurity", () => FileSystemAclExtensions.CreateDirectory(security, path));
        }

        [Fact]
        public void DirectorySecurity_CreateDirectory_InvalidPath()
        {
            DirectorySecurity security = new DirectorySecurity();

            Assert.Throws<ArgumentNullException>("path", () => security.CreateDirectory(null));
            Assert.Throws<ArgumentException>(() => security.CreateDirectory(""));
        }

        [Fact]
        public void DirectorySecurity_CreateDirectory_DirectoryAlreadyExists()
        {
            using var directory = new TempAclDirectory();
            string path = Path.Combine(directory.Path, "createMe");

            DirectorySecurity expectedSecurity = GetDirectorySecurity(FileSystemRights.FullControl);

            expectedSecurity.CreateDirectory(path);

            Assert.True(Directory.Exists(path));

            DirectorySecurity basicSecurity = new DirectorySecurity();

            // Already exists, existingDirInfo should have the original security, not the new basic security
            DirectoryInfo existingDirInfo = basicSecurity.CreateDirectory(path);

            DirectorySecurity actualSecurity = existingDirInfo.GetAccessControl(AccessControlSections.Access);

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        #endregion

        #endregion


        #region Helper methods

        private DirectorySecurity GetDirectorySecurity(FileSystemRights rights)
        {
            DirectorySecurity security = new DirectorySecurity();
            SecurityIdentifier identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            FileSystemAccessRule accessRule = new FileSystemAccessRule(identity, rights, AccessControlType.Allow);
            security.AddAccessRule(accessRule);
            return security;
        }

        private void Verify_DirectorySecurity_CreateDirectory(DirectorySecurity expectedSecurity)
        {
            using var directory = new TempAclDirectory();
            string path = Path.Combine(directory.Path, "createMe");

            expectedSecurity.CreateDirectory(path);

            Assert.True(Directory.Exists(path));

            DirectoryInfo actualInfo = new DirectoryInfo(path);

            DirectorySecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        private FileSecurity GetFileSecurity(FileSystemRights rights)
        {
            FileSecurity security = new FileSecurity();

            SecurityIdentifier identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            FileSystemAccessRule accessRule = new FileSystemAccessRule(identity, rights, AccessControlType.Allow);

            security.AddAccessRule(accessRule);

            return security;
        }

        private void VerifyAccessSecurity(CommonObjectSecurity expectedSecurity, CommonObjectSecurity actualSecurity)
        {
            Assert.Equal(typeof(FileSystemRights), expectedSecurity.AccessRightType);

            Assert.Equal(typeof(FileSystemRights), actualSecurity.AccessRightType);

            List<FileSystemAccessRule> expectedAccessRules = expectedSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>().ToList();

            List<FileSystemAccessRule> actualAccessRules = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>().ToList();

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
