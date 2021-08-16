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
            var directoryInfo = new DirectoryInfo(directory.Path);
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
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
            var directoryInfo = new DirectoryInfo(directory.Path);
            var accessControlSections = new AccessControlSections();
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
            var fileInfo = new FileInfo(file.Path);
            FileSecurity fileSecurity = fileInfo.GetAccessControl();
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
            var fileInfo = new FileInfo(file.Path);
            var accessControlSections = new AccessControlSections();
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
            using FileStream fileStream = File.Open(file.Path, FileMode.Append, FileAccess.Write, FileShare.None);
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
            var directoryInfo = new DirectoryInfo(directory.Path);
            AssertExtensions.Throws<ArgumentNullException>("directorySecurity", () => directoryInfo.SetAccessControl(directorySecurity: null));
        }

        [Fact]
        public void SetAccessControl_DirectoryInfo_DirectorySecurity_Success()
        {
            using var directory = new TempAclDirectory();
            var directoryInfo = new DirectoryInfo(directory.Path);
            var directorySecurity = new DirectorySecurity();
            directoryInfo.SetAccessControl(directorySecurity);
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_InvalidArguments()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            var fileInfo = new FileInfo(file.Path);
            AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => fileInfo.SetAccessControl(fileSecurity: null));
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_Success()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            var fileInfo = new FileInfo(file.Path);
            var fileSecurity = new FileSecurity();
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
            using FileStream fileStream = File.Open(file.Path, FileMode.Append, FileAccess.Write, FileShare.None);
            AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => FileSystemAclExtensions.SetAccessControl(fileStream, fileSecurity: null));
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_Success()
        {
            using var directory = new TempAclDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            using FileStream fileStream = File.Open(file.Path, FileMode.Append, FileAccess.Write, FileShare.None);
            var fileSecurity = new FileSecurity();
            FileSystemAclExtensions.SetAccessControl(fileStream, fileSecurity);
        }

        #endregion

        #region DirectoryInfo Create

        [Fact]
        public void DirectoryInfo_Create_NullDirectoryInfo()
        {
            DirectoryInfo info = null;
            var security = new DirectorySecurity();
            Assert.Throws<ArgumentNullException>("directoryInfo", () => CreateDirectoryWithSecurity(info, security));
        }

        [Fact]
        public void DirectoryInfo_Create_NullDirectorySecurity()
        {
            var info = new DirectoryInfo("path");
            Assert.Throws<ArgumentNullException>("directorySecurity", () => CreateDirectoryWithSecurity(info, null));
        }

        [Fact]
        public void DirectoryInfo_Create_NotFound()
        {
            using var tempRootDir = new TempAclDirectory();
            string dirPath = Path.Combine(tempRootDir.Path, Guid.NewGuid().ToString(), "ParentDoesNotExist");

            var dirInfo = new DirectoryInfo(dirPath);
            var security = new DirectorySecurity();
            // Fails because the DirectorySecurity lacks any rights to create parent folder
            Assert.Throws<UnauthorizedAccessException>(() =>  CreateDirectoryWithSecurity(dirInfo, security));
        }

        [Fact]
        public void DirectoryInfo_Create_NotFound_FullControl()
        {
            using var tempRootDir = new TempAclDirectory();
            string dirPath = Path.Combine(tempRootDir.Path, Guid.NewGuid().ToString(), "ParentDoesNotExist");

            var dirInfo = new DirectoryInfo(dirPath);
            var security = GetDirectorySecurity(FileSystemRights.FullControl);
            // Succeeds because it creates the missing parent folder
            CreateDirectoryWithSecurity(dirInfo, security);
        }

        private void CreateDirectoryWithSecurity(DirectoryInfo info, DirectorySecurity security)
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

        [Fact]
        public void DirectoryInfo_Create_DefaultDirectorySecurity()
        {
            var security = new DirectorySecurity();
            Verify_DirectorySecurity_CreateDirectory(security);
        }

        [Theory]
        // Must have at least one ReadData, otherwise the TempAclDirectory will fail to delete that item on dispose
        [MemberData(nameof(RightsToAllow))]
        public void DirectoryInfo_Create_AllowSpecific_AccessRules(FileSystemRights rights)
        {
            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.Path, "directory");
            var dirInfo = new DirectoryInfo(path);

            DirectorySecurity expectedSecurity = GetDirectorySecurity(rights);
            dirInfo.Create(expectedSecurity);
            Assert.True(dirInfo.Exists);
            tempRootDir.CreatedSubdirectories.Add(dirInfo);

            var actualInfo = new DirectoryInfo(dirInfo.FullName);
            DirectorySecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);
            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        [Theory]
        [MemberData(nameof(RightsToDeny))]
        public void DirectoryInfo_Create_DenySpecific_AddAccessRules(FileSystemRights rightsToDeny)
        {
            var expectedSecurity = new DirectorySecurity();
            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            var allowAccessRule = new FileSystemAccessRule(identity, FileSystemRights.Read, AccessControlType.Allow);
            expectedSecurity.AddAccessRule(allowAccessRule);

            var denyAccessRule = new FileSystemAccessRule(identity, rightsToDeny, AccessControlType.Deny);
            expectedSecurity.AddAccessRule(denyAccessRule);

            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.Path, "directory");
            var dirInfo = new DirectoryInfo(path);

            dirInfo.Create(expectedSecurity);
            Assert.True(dirInfo.Exists);
            tempRootDir.CreatedSubdirectories.Add(dirInfo);

            var actualInfo = new DirectoryInfo(dirInfo.FullName);
            DirectorySecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);
            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        #endregion

        #region FileInfo Create

        [Fact]
        public void FileInfo_Create_NullFileInfo()
        {
            FileInfo info = null;
            var security = new FileSecurity();

            Assert.Throws<ArgumentNullException>("fileInfo", () =>
                CreateFileWithSecurity(info, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        [Fact]
        public void FileInfo_Create_NullFileSecurity()
        {
            var info = new FileInfo("path");

            Assert.Throws<ArgumentNullException>("fileSecurity", () =>
                CreateFileWithSecurity(info, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, null));
        }

        [Fact]
        public void FileInfo_Create_NotFound()
        {
            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.Path, Guid.NewGuid().ToString(), "file.txt");
            var fileInfo = new FileInfo(path);
            var security = new FileSecurity();

            Assert.Throws<DirectoryNotFoundException>(() =>
                CreateFileWithSecurity(fileInfo, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData((FileMode)int.MinValue)]
        [InlineData((FileMode)0)]
        [InlineData((FileMode)int.MaxValue)]
        public void FileInfo_Create_FileSecurity_InvalidFileMode(FileMode invalidMode)
        {
            var security = new FileSecurity();
            var info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("mode", () =>
                CreateFileWithSecurity(info, invalidMode, FileSystemRights.WriteData, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData((FileShare)(-1))]
        [InlineData((FileShare)int.MaxValue)]
        public void FileInfo_Create_FileSecurity_InvalidFileShare(FileShare invalidFileShare)
        {
            var security = new FileSecurity();
            var info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("share", () =>
                CreateFileWithSecurity(info, FileMode.CreateNew, FileSystemRights.WriteData, invalidFileShare, DefaultBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        public void FileInfo_Create_FileSecurity_InvalidBufferSize(int invalidBufferSize)
        {
            var security = new FileSecurity();
            var info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () =>
                CreateFileWithSecurity(info, FileMode.CreateNew, FileSystemRights.WriteData, FileShare.Delete, invalidBufferSize, FileOptions.None, security));
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
            var security = new FileSecurity();
            var info = new FileInfo("path");

            Assert.Throws<ArgumentException>(() =>
                CreateFileWithSecurity(info, mode, rights, FileShare.Delete, DefaultBufferSize, FileOptions.None, security));
        }

        [Fact]
        public void FileInfo_Create_DefaultFileSecurity()
        {
            var security = new FileSecurity();
            Verify_FileSecurity_CreateFile(security);
        }

        private void CreateFileWithSecurity(FileInfo info, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity security)
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

        [Theory]
        // Must have at least one Read, otherwise the TempAclDirectory will fail to delete that item on dispose
        [MemberData(nameof(RightsToAllow))]
        public void FileInfo_Create_AllowSpecific_AccessRules(FileSystemRights rights)
        {
            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.Path, "file.txt");
            var fileInfo = new FileInfo(path);

            FileSecurity expectedSecurity = GetFileSecurity(rights);

            using FileStream stream = fileInfo.Create(
                FileMode.Create,
                FileSystemRights.FullControl,
                FileShare.ReadWrite | FileShare.Delete,
                DefaultBufferSize,
                FileOptions.None,
                expectedSecurity);

            Assert.True(fileInfo.Exists);
            tempRootDir.CreatedSubfiles.Add(fileInfo);

            var actualInfo = new FileInfo(fileInfo.FullName);
            FileSecurity actualSecurity = actualInfo.GetAccessControl(AccessControlSections.Access);
            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }


        [Theory]
        [MemberData(nameof(RightsToDeny))]
        public void FileInfo_Create_DenySpecific_AccessRules(FileSystemRights rightsToDeny)
        {
            var expectedSecurity = new FileSecurity();

            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var allowAccessRule = new FileSystemAccessRule(identity, FileSystemRights.Read, AccessControlType.Allow);
            expectedSecurity.AddAccessRule(allowAccessRule);

            var denyAccessRule = new FileSystemAccessRule(identity, rightsToDeny, AccessControlType.Deny);
            expectedSecurity.AddAccessRule(denyAccessRule);

            using var tempRootDir = new TempAclDirectory();

            string path = Path.Combine(tempRootDir.Path, "file.txt");
            var fileInfo = new FileInfo(path);

            using FileStream stream = fileInfo.Create(
                FileMode.Create,
                FileSystemRights.FullControl,
                FileShare.ReadWrite | FileShare.Delete,
                DefaultBufferSize,
                FileOptions.None,
                expectedSecurity);

            Assert.True(fileInfo.Exists);
            tempRootDir.CreatedSubfiles.Add(fileInfo);

            var actualInfo = new FileInfo(fileInfo.FullName);
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
            var security = new DirectorySecurity();

            Assert.Throws<ArgumentNullException>("path", () => security.CreateDirectory(null));
            Assert.Throws<ArgumentException>(() => security.CreateDirectory(""));
        }

        [Fact]
        public void DirectorySecurity_CreateDirectory_DirectoryAlreadyExists()
        {
            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.Path, "createMe");

            DirectorySecurity expectedSecurity = GetDirectorySecurity(FileSystemRights.FullControl);
            DirectoryInfo dirInfo = expectedSecurity.CreateDirectory(path);
            Assert.True(dirInfo.Exists);
            tempRootDir.CreatedSubdirectories.Add(dirInfo);

            var basicSecurity = new DirectorySecurity();
            // Already exists, existingDirInfo should have the original security, not the new basic security
            DirectoryInfo existingDirInfo = basicSecurity.CreateDirectory(path);

            DirectorySecurity actualSecurity = existingDirInfo.GetAccessControl(AccessControlSections.Access);
            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        #endregion

        #endregion


        #region Helper methods

        public static IEnumerable<object[]> RightsToDeny()
        {
            yield return new object[] { FileSystemRights.AppendData };
            yield return new object[] { FileSystemRights.ChangePermissions };
            // yield return new object[] { FileSystemRights.CreateDirectories }; // CreateDirectories == AppendData
            yield return new object[] { FileSystemRights.CreateFiles };
            yield return new object[] { FileSystemRights.Delete };
            yield return new object[] { FileSystemRights.DeleteSubdirectoriesAndFiles };
            yield return new object[] { FileSystemRights.ExecuteFile };
            // yield return new object[] { FileSystemRights.FullControl }; // Contains ReadData, should not deny that
            // yield return new object[] { FileSystemRights.ListDirectory }; ListDirectory == ReadData
            // yield return new object[] { FileSystemRights.Modify }; // Contains ReadData, should not deny that
            // yield return new object[] { FileSystemRights.Read }; // Contains ReadData, should not deny that
            // yield return new object[] { FileSystemRights.ReadAndExecute }; // Contains ReadData, should not deny that
            yield return new object[] { FileSystemRights.ReadAttributes };
            // yield return new object[] { FileSystemRights.ReadData }; // Minimum right required to delete a file or directory
            yield return new object[] { FileSystemRights.ReadExtendedAttributes };
            yield return new object[] { FileSystemRights.ReadPermissions };
            // yield return new object[] { FileSystemRights.Synchronize }; // CreateFile always requires Synchronize access
            yield return new object[] { FileSystemRights.TakeOwnership };
            //yield return new object[] { FileSystemRights.Traverse }; // Traverse == ExecuteFile
            yield return new object[] { FileSystemRights.Write };
            yield return new object[] { FileSystemRights.WriteAttributes };
            // yield return new object[] { FileSystemRights.WriteData }; // WriteData == CreateFiles
            yield return new object[] { FileSystemRights.WriteExtendedAttributes };
        }

        public static IEnumerable<object[]> RightsToAllow()
        {
            yield return new object[] { FileSystemRights.AppendData };
            yield return new object[] { FileSystemRights.ChangePermissions };
            // yield return new object[] { FileSystemRights.CreateDirectories }; // CreateDirectories == AppendData
            yield return new object[] { FileSystemRights.CreateFiles };
            yield return new object[] { FileSystemRights.Delete };
            yield return new object[] { FileSystemRights.DeleteSubdirectoriesAndFiles };
            yield return new object[] { FileSystemRights.ExecuteFile };
            yield return new object[] { FileSystemRights.FullControl };
            // yield return new object[] { FileSystemRights.ListDirectory }; ListDirectory == ReadData
            yield return new object[] { FileSystemRights.Modify };
            yield return new object[] { FileSystemRights.Read };
            yield return new object[] { FileSystemRights.ReadAndExecute };
            yield return new object[] { FileSystemRights.ReadAttributes };
            // yield return new object[] { FileSystemRights.ReadData }; // Minimum right required to delete a file or directory
            yield return new object[] { FileSystemRights.ReadExtendedAttributes };
            yield return new object[] { FileSystemRights.ReadPermissions };
            yield return new object[] { FileSystemRights.Synchronize };
            yield return new object[] { FileSystemRights.TakeOwnership };
            // yield return new object[] { FileSystemRights.Traverse }; // Traverse == ExecuteFile
            yield return new object[] { FileSystemRights.Write };
            yield return new object[] { FileSystemRights.WriteAttributes };
            // yield return new object[] { FileSystemRights.WriteData }; // WriteData == CreateFiles
            yield return new object[] { FileSystemRights.WriteExtendedAttributes };
        }

        private void Verify_FileSecurity_CreateFile(FileSecurity expectedSecurity)
        {
            Verify_FileSecurity_CreateFile(FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, expectedSecurity);
        }

        private void Verify_FileSecurity_CreateFile(FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity expectedSecurity)
        {
            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.Path, "file.txt");
            var fileInfo = new FileInfo(path);

            fileInfo.Create(mode, rights, share, bufferSize, options, expectedSecurity).Dispose();
            Assert.True(fileInfo.Exists);
            tempRootDir.CreatedSubfiles.Add(fileInfo);

            var actualFileInfo = new FileInfo(path);
            FileSecurity actualSecurity = actualFileInfo.GetAccessControl(AccessControlSections.Access);
            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        private void Verify_DirectorySecurity_CreateDirectory(DirectorySecurity expectedSecurity)
        {
            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.Path, "createMe");
            DirectoryInfo dirInfo = expectedSecurity.CreateDirectory(path);
            Assert.True(dirInfo.Exists);
            tempRootDir.CreatedSubdirectories.Add(dirInfo);

            var actualDirInfo = new DirectoryInfo(path);
            DirectorySecurity actualSecurity = actualDirInfo.GetAccessControl(AccessControlSections.Access);

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        private DirectorySecurity GetDirectorySecurity(FileSystemRights rights)
        {
            var security = new DirectorySecurity();
            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var accessRule = new FileSystemAccessRule(identity, rights, AccessControlType.Allow);
            security.AddAccessRule(accessRule);
            return security;
        }

        private FileSecurity GetFileSecurity(FileSystemRights rights)
        {
            var security = new FileSecurity();
            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var accessRule = new FileSystemAccessRule(identity, rights, AccessControlType.Allow);
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
