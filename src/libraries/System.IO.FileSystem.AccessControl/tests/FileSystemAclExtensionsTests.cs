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
            using var directory = new TempDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
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
            using var directory = new TempDirectory();
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
            using var directory = new TempDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            FileInfo fileInfo = new FileInfo(file.Path);
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
            using var directory = new TempDirectory();
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
            using var directory = new TempDirectory();
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
            using var directory = new TempDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
            AssertExtensions.Throws<ArgumentNullException>("directorySecurity", () => directoryInfo.SetAccessControl(directorySecurity: null));
        }

        [Fact]
        public void SetAccessControl_DirectoryInfo_DirectorySecurity_Success()
        {
            using var directory = new TempDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.Path);
            DirectorySecurity directorySecurity = new DirectorySecurity();
            directoryInfo.SetAccessControl(directorySecurity);
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_InvalidArguments()
        {
            using var directory = new TempDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            FileInfo fileInfo = new FileInfo(file.Path);
            AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => fileInfo.SetAccessControl(fileSecurity: null));
        }

        [Fact]
        public void SetAccessControl_FileInfo_FileSecurity_Success()
        {
            using var directory = new TempDirectory();
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
            using var directory = new TempDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            using FileStream fileStream = File.Open(file.Path, FileMode.Append, FileAccess.Write, FileShare.None);
            AssertExtensions.Throws<ArgumentNullException>("fileSecurity", () => FileSystemAclExtensions.SetAccessControl(fileStream, fileSecurity: null));
        }

        [Fact]
        public void SetAccessControl_FileStream_FileSecurity_Success()
        {
            using var directory = new TempDirectory();
            using var file = new TempFile(Path.Combine(directory.Path, "file.txt"));
            using FileStream fileStream = File.Open(file.Path, FileMode.Append, FileAccess.Write, FileShare.None);
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

            Assert.Throws<ArgumentNullException>("directoryInfo", () =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, security);
                }
                else
                {
                    info.Create(security);
                }
            });
        }

        [Fact]
        public void DirectoryInfo_Create_NullDirectorySecurity()
        {
            DirectoryInfo info = new DirectoryInfo("path");

            Assert.Throws<ArgumentNullException>("directorySecurity", () =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, null);
                }
                else
                {
                    info.Create(null);
                }
            });
        }

        [Fact]
        public void DirectoryInfo_Create_NotFound()
        {
            using var directory = new TempDirectory();
            string path = Path.Combine(directory.Path, Guid.NewGuid().ToString(), "ParentDoesNotExist");
            DirectoryInfo info = new DirectoryInfo(path);
            DirectorySecurity security = new DirectorySecurity();

            Assert.Throws<UnauthorizedAccessException>(() =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, security);
                }
                else
                {
                    info.Create(security);
                }
            });
        }

        [Fact]
        public void DirectoryInfo_Create_DefaultDirectorySecurity()
        {
            DirectorySecurity security = new DirectorySecurity();
            Verify_DirectoryInfo_Create(security);
        }

        [Theory]
        [InlineData(FileSystemRights.ReadAndExecute, AccessControlType.Allow)]
        [InlineData(FileSystemRights.ReadAndExecute, AccessControlType.Deny)]
        [InlineData(FileSystemRights.WriteData,      AccessControlType.Allow)]
        [InlineData(FileSystemRights.WriteData,      AccessControlType.Deny)]
        [InlineData(FileSystemRights.FullControl,    AccessControlType.Allow)]
        [InlineData(FileSystemRights.FullControl,    AccessControlType.Deny)]
        public void DirectoryInfo_Create_DirectorySecurityWithSpecificAccessRule(
            FileSystemRights rights,
            AccessControlType controlType)
        {
            DirectorySecurity security = GetDirectorySecurity(rights, controlType);
            Verify_DirectoryInfo_Create(security);
        }

        #endregion

        #region FileInfo Create

        [Fact]
        public void FileInfo_Create_NullFileInfo()
        {
            FileInfo info = null;
            FileSecurity security = new FileSecurity();

            Assert.Throws<ArgumentNullException>("fileInfo", () =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, security);
                }
                else
                {
                    info.Create(FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, security);
                }
            });
        }

        [Fact]
        public void FileInfo_Create_NullFileSecurity()
        {
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentNullException>("fileSecurity", () =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, null);
                }
                else
                {
                    info.Create(FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, null);
                }
            });
        }

        [Fact]
        public void FileInfo_Create_NotFound()
        {
            using var directory = new TempDirectory();
            string path = Path.Combine(directory.Path, Guid.NewGuid().ToString(), "file.txt");
            FileInfo info = new FileInfo(path);
            FileSecurity security = new FileSecurity();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, security);
                }
                else
                {
                    info.Create(FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, security);
                }
            });
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
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, invalidMode, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, security); ;
                }
                else
                {
                    info.Create(invalidMode, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, security);
                }
            });
        }

        [Theory]
        [InlineData((FileShare)(-1))]
        [InlineData((FileShare)int.MaxValue)]
        public void FileInfo_Create_FileSecurity_InvalidFileShare(FileShare invalidFileShare)
        {
            FileSecurity security = new FileSecurity();
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("share", () =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, FileMode.Create, FileSystemRights.WriteData, invalidFileShare, DefaultBufferSize, FileOptions.None, security);
                }
                else
                {
                    info.Create(FileMode.Create, FileSystemRights.WriteData, invalidFileShare, DefaultBufferSize, FileOptions.None, security);
                }
            });
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        public void FileInfo_Create_FileSecurity_InvalidBufferSize(int invalidBufferSize)
        {
            FileSecurity security = new FileSecurity();
            FileInfo info = new FileInfo("path");

            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () =>
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, FileMode.Create, FileSystemRights.WriteData, FileShare.Read, invalidBufferSize, FileOptions.None, security);
                }
                else
                {
                    info.Create(FileMode.Create, FileSystemRights.WriteData, FileShare.Read, invalidBufferSize, FileOptions.None, security);
                }
            });
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
            {
                if (PlatformDetection.IsNetFramework)
                {
                    FileSystemAclExtensions.Create(info, mode, rights, FileShare.Read, DefaultBufferSize, FileOptions.None, security);
                }
                else
                {
                    info.Create(mode, rights, FileShare.Read, DefaultBufferSize, FileOptions.None, security);
                }
            });
        }

        [Fact]
        public void FileInfo_Create_DefaultFileSecurity()
        {
            FileSecurity security = new FileSecurity();
            Verify_FileInfo_Create(security);
        }

        [Theory]
        [InlineData(FileSystemRights.ReadAndExecute, AccessControlType.Allow)]
        [InlineData(FileSystemRights.ReadAndExecute, AccessControlType.Deny)]
        [InlineData(FileSystemRights.WriteData,      AccessControlType.Allow)]
        [InlineData(FileSystemRights.WriteData,      AccessControlType.Deny)]
        [InlineData(FileSystemRights.FullControl,    AccessControlType.Allow)]
        [InlineData(FileSystemRights.FullControl,    AccessControlType.Deny)]
        public void FileInfo_Create_FileSecurity_SpecificAccessRule(FileSystemRights rights, AccessControlType controlType)
        {
            FileSecurity security = GetFileSecurity(rights, controlType);
            Verify_FileInfo_Create(security);
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
        public void DirectorySecurity_CreateDirectory_DefaultDirectorySecurity()
        {
            DirectorySecurity security = new DirectorySecurity();
            Verify_DirectorySecurity_CreateDirectory(security);
        }

        [Theory]
        [InlineData(FileSystemRights.ReadAndExecute, AccessControlType.Allow)]
        [InlineData(FileSystemRights.ReadAndExecute, AccessControlType.Deny)]
        [InlineData(FileSystemRights.WriteData,      AccessControlType.Allow)]
        [InlineData(FileSystemRights.WriteData,      AccessControlType.Deny)]
        [InlineData(FileSystemRights.FullControl,    AccessControlType.Allow)]
        [InlineData(FileSystemRights.FullControl,    AccessControlType.Deny)]
        public void DirectorySecurity_CreateDirectory_DirectorySecurityWithSpecificAccessRule(
            FileSystemRights rights,
            AccessControlType controlType)
        {
            DirectorySecurity security = GetDirectorySecurity(rights, controlType);
            Verify_DirectorySecurity_CreateDirectory(security);
        }

        [Fact]
        public void DirectorySecurity_CreateDirectory_DirectoryAlreadyExists()
        {
            using var directory = new TempDirectory();
            string path = Path.Combine(directory.Path, "createMe");

            DirectorySecurity basicSecurity = new DirectorySecurity();
            basicSecurity.CreateDirectory(path);

            Assert.True(Directory.Exists(path));

            DirectorySecurity specificSecurity = GetDirectorySecurity(FileSystemRights.ExecuteFile, AccessControlType.Deny);

            // Already exists, existingDirInfo should have the original basic security, not the new specific security
            DirectoryInfo existingDirInfo = specificSecurity.CreateDirectory(path);

            DirectorySecurity actualSecurity = existingDirInfo.GetAccessControl();

            VerifyAccessSecurity(basicSecurity, actualSecurity);
        }

        #endregion

        #endregion


        #region Helper methods

        private DirectorySecurity GetDirectorySecurity(FileSystemRights rights, AccessControlType controlType) => GetDirectorySecurity(WellKnownSidType.BuiltinUsersSid, rights, controlType);

        private DirectorySecurity GetDirectorySecurity(WellKnownSidType sid, FileSystemRights rights, AccessControlType controlType)
        {
            DirectorySecurity security = new DirectorySecurity();

            SecurityIdentifier identity = new SecurityIdentifier(sid, null);
            FileSystemAccessRule accessRule = new FileSystemAccessRule(identity, rights, controlType);
            security.AddAccessRule(accessRule);

            return security;
        }

        private void Verify_DirectoryInfo_Create(DirectorySecurity expectedSecurity)
        {
            using var directory = new TempDirectory();
            string path = Path.Combine(directory.Path, "directory");
            DirectoryInfo info = new DirectoryInfo(path);

            info.Create(expectedSecurity);

            Assert.True(Directory.Exists(path));

            DirectoryInfo actualInfo = new DirectoryInfo(info.FullName);

            DirectorySecurity actualSecurity = actualInfo.GetAccessControl();

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        private void Verify_DirectorySecurity_CreateDirectory(DirectorySecurity expectedSecurity)
        {
            using var directory = new TempDirectory();
            string path = Path.Combine(directory.Path, "createMe");

            expectedSecurity.CreateDirectory(path);

            Assert.True(Directory.Exists(path));

            DirectoryInfo actualInfo = new DirectoryInfo(path);

            DirectorySecurity actualSecurity = actualInfo.GetAccessControl();

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
        }

        private FileSecurity GetFileSecurity(FileSystemRights rights, AccessControlType controlType) => GetFileSecurity(WellKnownSidType.BuiltinUsersSid, rights, controlType);

        private FileSecurity GetFileSecurity(WellKnownSidType sid, FileSystemRights rights, AccessControlType controlType)
        {
            FileSecurity security = new FileSecurity();

            SecurityIdentifier identity = new SecurityIdentifier(sid, null);
            FileSystemAccessRule accessRule = new FileSystemAccessRule(identity, rights, controlType);
            security.AddAccessRule(accessRule);

            return security;
        }

        private void Verify_FileInfo_Create(FileSecurity expectedSecurity)
        {
            Verify_FileInfo_Create(FileMode.Create, FileSystemRights.WriteData, FileShare.Read, DefaultBufferSize, FileOptions.None, expectedSecurity);
        }

        private void Verify_FileInfo_Create(FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity expectedSecurity)
        {
            using var directory = new TempDirectory();

            string path = Path.Combine(directory.Path, "file.txt");
            FileInfo info = new FileInfo(path);

            info.Create(mode, rights, share, bufferSize, options, expectedSecurity);

            Assert.True(File.Exists(path));

            FileInfo actualInfo = new FileInfo(info.FullName);

            FileSecurity actualSecurity = actualInfo.GetAccessControl();

            VerifyAccessSecurity(expectedSecurity, actualSecurity);
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
