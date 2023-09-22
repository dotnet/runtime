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
        public void SetAccessControl_FileInfo_FileSecurity_Success_NameLongerThanMaxShortPath()
        {
            using var directory = new TempAclDirectory();

            const int MaxShortPath = 260;
            int fileNameLength = Math.Max(MaxShortPath - directory.Path.Length, 1);

            string path = Path.Combine(directory.Path, new string('1', fileNameLength) + ".txt");
            using var file = new TempFile(path, 1);
            var fileInfo = new FileInfo(file.Path);
            FileSecurity fileSecurity = fileInfo.GetAccessControl(AccessControlSections.Access);

            var newAccessRule = new FileSystemAccessRule(Helpers.s_NetworkServiceNTAccount, FileSystemRights.Write, AccessControlType.Allow);
            fileSecurity.SetAccessRule(newAccessRule);

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
            string dirPath = Path.Combine(tempRootDir.GenerateSubItemPath(), "ParentDoesNotExist");

            var dirInfo = new DirectoryInfo(dirPath);
            var security = new DirectorySecurity();
            // Fails because the DirectorySecurity lacks any rights to create parent folder
            Assert.Throws<UnauthorizedAccessException>(() => CreateDirectoryWithSecurity(dirInfo, security));
        }

        [Fact]
        public void DirectoryInfo_Create_NotFound_FullControl()
        {
            using var tempRootDir = new TempAclDirectory();
            string dirPath = Path.Combine(tempRootDir.GenerateSubItemPath(), "ParentDoesNotExist");

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
                info.Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, DefaultBufferSize, FileOptions.None, security));
        }

        [Fact]
        public void FileInfo_Create_DirectoryNotFound()
        {
            using var tempRootDir = new TempAclDirectory();
            string path = Path.Combine(tempRootDir.GenerateSubItemPath(), "file.txt");
            var info = new FileInfo(path);
            var security = new FileSecurity();
            Assert.Throws<DirectoryNotFoundException>(() =>
                info.Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, DefaultBufferSize, FileOptions.None, security));
        }

        [Theory]
        [InlineData((FileMode)int.MinValue)]
        [InlineData((FileMode)0)]
        [InlineData((FileMode)int.MaxValue)]
        public void FileInfo_Create_FileSecurity_OutOfRange_FileMode(FileMode invalidMode) =>
            FileInfo_Create_FileSecurity_ArgumentOutOfRangeException("mode", mode: invalidMode);

        [Theory]
        [InlineData((FileShare)(-1))]
        [InlineData((FileShare)int.MaxValue)]
        public void FileInfo_Create_FileSecurity_OutOfRange_FileShare(FileShare invalidFileShare) =>
            FileInfo_Create_FileSecurity_ArgumentOutOfRangeException("share", share: invalidFileShare);

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        public void FileInfo_Create_FileSecurity_OutOfRange_BufferSize(int invalidBufferSize) =>
            FileInfo_Create_FileSecurity_ArgumentOutOfRangeException("bufferSize", bufferSize: invalidBufferSize);

        public static IEnumerable<object[]> WriteModes_ReadRights_ForbiddenCombo_Data() =>
            from mode in s_writableModes
            from rights in s_readableRights
            where
                // These combinations are allowed, exclude them
                !(rights == FileSystemRights.CreateFiles &&
                  (mode == FileMode.Append || mode == FileMode.Create || mode == FileMode.CreateNew))
            select new object[] { mode, rights };

        // Do not combine writing modes with exclusively read rights
        [Theory]
        [MemberData(nameof(WriteModes_ReadRights_ForbiddenCombo_Data))]
        public void FileInfo_Create_FileSecurity_WriteModes_ReadRights_ForbiddenCombo(FileMode mode, FileSystemRights rights) =>
            FileInfo_Create_FileSecurity_ArgumentException(mode, rights);

        public static IEnumerable<object[]> OpenOrCreateMode_ReadRights_AllowedCombo_Data() =>
            from rights in s_readableRights
            select new object[] { rights };

        // OpenOrCreate allows using exclusively read rights
        [Theory]
        [MemberData(nameof(OpenOrCreateMode_ReadRights_AllowedCombo_Data))]
        public void FileInfo_Create_FileSecurity_OpenOrCreateMode_ReadRights_AllowedCombo(FileSystemRights rights) =>
            FileInfo_Create_FileSecurity_Successful(FileMode.OpenOrCreate, rights);

        // Append, Create and CreateNew allow using CreateFiles rights
        // These combinations were excluded from WriteModes_ReadRights_ForbiddenCombo_Data
        [Theory]
        [InlineData(FileMode.Append)]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        public void FileInfo_Create_FileSecurity_WriteModes_CreateFilesRights_AllowedCombo(FileMode mode) =>
            FileInfo_Create_FileSecurity_Successful(mode, FileSystemRights.CreateFiles);

        public static IEnumerable<object[]> AppendMode_UnexpectedReadRights_Data() =>
            from writeRights in s_writableRights
            from readRights in new[] {
                FileSystemRights.ExecuteFile,
                FileSystemRights.ReadAttributes, FileSystemRights.ReadData, FileSystemRights.ReadExtendedAttributes, FileSystemRights.ReadPermissions,
                FileSystemRights.Read, // Contains ReadAttributes, ReadData, ReadExtendedAttributes, ReadPermissions
                FileSystemRights.ReadAndExecute, // Contains Read and ExecuteFile
            }
            select new object[] { writeRights | readRights };

        // Append is allowed if at least one write permission is provided
        // But append is disallowed if any read rights are provided
        [Theory]
        [MemberData(nameof(AppendMode_UnexpectedReadRights_Data))]
        public void FileInfo_Create_FileSecurity_AppendMode_UnexpectedReadRights(FileSystemRights rights) =>
            FileInfo_Create_FileSecurity_ArgumentException(FileMode.Append, rights);

        public static IEnumerable<object[]> AppendMode_OnlyWriteRights_Data() =>
            from writeRights in s_writableRights
            select new object[] { writeRights };

        // Append succeeds if only write permissions were provided (no read permissions)
        [Theory]
        [MemberData(nameof(AppendMode_OnlyWriteRights_Data))]
        public void FileInfo_Create_FileSecurity_AppendMode_OnlyWriteRights(FileSystemRights rights) =>
            FileInfo_Create_FileSecurity_Successful(FileMode.Append, rights);

        public static IEnumerable<object[]> WritableRights_Data() =>
            from rights in s_writableRights
            select new object[] { rights };

        // Cannot truncate unless all write rights are provided
        [Theory]
        [MemberData(nameof(WritableRights_Data))]
        public void FileInfo_Create_FileSecurity_TruncateMode_IncompleteWriteRights(FileSystemRights rights) =>
            FileInfo_Create_FileSecurity_ArgumentException(FileMode.Truncate, rights);

        [Fact]
        public void FileInfo_Create_FileSecurity_TruncateMode_AllWriteRights_Throws()
        {
            // Truncate, with all write rights, throws with different messages in each framework:
            // - In .NET Framework, throws "Could not find file"
            // - In .NET, throws IOException: "The parameter is incorrect"

            var security = new FileSecurity();
            var info = new FileInfo(PathGenerator.GenerateTestFileName());
            Assert.Throws<IOException>(() => info.Create(FileMode.Truncate, FileSystemRights.Write | FileSystemRights.ReadData, FileShare.None, DefaultBufferSize, FileOptions.None, security));
        }

        public static IEnumerable<object[]> WriteRights_AllArguments_Data() =>
            from mode in s_writableModes
            from rights in s_writableRights
            from share in Enum.GetValues<FileShare>()
            from options in Enum.GetValues<FileOptions>()
            where !(rights == FileSystemRights.CreateFiles &&
                    (mode == FileMode.Append || mode == FileMode.Create || mode == FileMode.CreateNew)) &&
                  !(mode == FileMode.Truncate && rights != FileSystemRights.Write) &&
                  (options != FileOptions.Encrypted && // Using FileOptions.Encrypted throws UnauthorizedAccessException when attempting to read the created file
                  !(options == FileOptions.Asynchronous && !PlatformDetection.IsAsyncFileIOSupported))
            select new object[] { mode, rights, share, options };

        [Theory]
        [MemberData(nameof(WriteRights_AllArguments_Data))]
        public void FileInfo_WriteRights_WithSecurity_Null(FileMode mode, FileSystemRights rights, FileShare share, FileOptions options) =>
            Verify_FileSecurity_CreateFile(mode, rights, share, DefaultBufferSize, options, expectedSecurity: null); // Null security

        [Theory]
        [MemberData(nameof(WriteRights_AllArguments_Data))]
        public void FileInfo_WriteRights_WithSecurity_Default(FileMode mode, FileSystemRights rights, FileShare share, FileOptions options) =>
            Verify_FileSecurity_CreateFile(mode, rights, share, DefaultBufferSize, options, new FileSecurity()); // Default security

        [Theory]
        [MemberData(nameof(WriteRights_AllArguments_Data))]
        public void FileInfo_WriteRights_WithSecurity_Custom(FileMode mode, FileSystemRights rights, FileShare share, FileOptions options) =>
            Verify_FileSecurity_CreateFile(mode, rights, share, DefaultBufferSize, options, GetFileSecurity(rights)); // Custom security (AccessRule Allow)

        public static IEnumerable<object[]> ReadRights_AllArguments_Data() =>
            from mode in new[] { FileMode.Create, FileMode.CreateNew, FileMode.OpenOrCreate }
            from rights in s_readableRights
            from share in Enum.GetValues<FileShare>()
            from options in Enum.GetValues<FileOptions>()
            where options != FileOptions.Encrypted && // Using FileOptions.Encrypted throws UnauthorizedAccessException when attempting to read the created file
            !(options == FileOptions.Asynchronous && !PlatformDetection.IsAsyncFileIOSupported)
            select new object[] { mode, rights, share, options };

        [Theory]
        [MemberData(nameof(ReadRights_AllArguments_Data))]
        public void FileInfo_ReadRights_WithSecurity_Null(FileMode mode, FileSystemRights rights, FileShare share, FileOptions options) =>
            // Writable FileModes require at least one write right
            Verify_FileSecurity_CreateFile(mode, rights | FileSystemRights.WriteData, share, DefaultBufferSize, options, expectedSecurity: null); // Null security

        [Theory]
        [MemberData(nameof(ReadRights_AllArguments_Data))]
        public void FileInfo_ReadRights_WithSecurity_Default(FileMode mode, FileSystemRights rights, FileShare share, FileOptions options) =>
            // Writable FileModes require at least one write right
            Verify_FileSecurity_CreateFile(mode, rights | FileSystemRights.WriteData, share, DefaultBufferSize, options, new FileSecurity()); // Default security

        [Theory]
        [MemberData(nameof(ReadRights_AllArguments_Data))]
        public void FileInfo_ReadRights_WithSecurity_Custom(FileMode mode, FileSystemRights rights, FileShare share, FileOptions options) =>
            // Writable FileModes require at least one write right
            Verify_FileSecurity_CreateFile(mode, rights | FileSystemRights.WriteData, share, DefaultBufferSize, options, GetFileSecurity(rights)); // Custom security (AccessRule Allow)

        [Theory]
        [MemberData(nameof(RightsToDeny))]
        public void FileInfo_Create_FileSecurity_DenyAccessRule(FileSystemRights rightsToDeny)
        {
            var expectedSecurity = new FileSecurity();

            var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            // Add write deny rule
            var denyAccessRule = new FileSystemAccessRule(identity, rightsToDeny, AccessControlType.Deny);
            expectedSecurity.AddAccessRule(denyAccessRule);

            using var tempRootDir = new TempAclDirectory();

            string path = tempRootDir.GenerateSubItemPath();
            var fileInfo = new FileInfo(path);

            using FileStream stream = fileInfo.Create(
                FileMode.CreateNew,
                FileSystemRights.Write, // Create expects at least one write right
                FileShare.None,
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
            string path = tempRootDir.GenerateSubItemPath();

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

        public static IEnumerable<object[]> RightsToDeny() =>
            from rights in new[] {
                FileSystemRights.AppendData, // Same as CreateDirectories
                FileSystemRights.ChangePermissions,
                FileSystemRights.Delete,
                FileSystemRights.DeleteSubdirectoriesAndFiles,
                FileSystemRights.ExecuteFile, // Same as Traverse
                FileSystemRights.ReadAttributes,
                FileSystemRights.ReadExtendedAttributes,
                FileSystemRights.ReadPermissions,
                FileSystemRights.TakeOwnership,
                FileSystemRights.Write, // Contains AppendData, WriteData, WriteAttributes, WriteExtendedAttributes
                FileSystemRights.WriteAttributes,
                FileSystemRights.WriteData, // Same as CreateFiles
                FileSystemRights.WriteExtendedAttributes,
                // Rights that should not be denied:
                // - Synchronize: CreateFile always requires Synchronize access
                // - ReadData: Minimum right required to delete a file or directory
                // - ListDirectory: Equivalent to ReadData
                // - Modify: Contains ReadData
                // - Read: Contains ReadData
                // - ReadAndExecute: Contains ReadData
                // - FullControl: Contains ReadData and Synchronize
            }
            select new object[] { rights };

        public static IEnumerable<object[]> RightsToAllow() =>
            from rights in new[] {
                FileSystemRights.AppendData, // Same as CreateDirectories
                FileSystemRights.ChangePermissions,
                FileSystemRights.Delete,
                FileSystemRights.DeleteSubdirectoriesAndFiles,
                FileSystemRights.ExecuteFile, // Same as Traverse
                FileSystemRights.Modify, // Contains Read, Write and ReadAndExecute
                FileSystemRights.Read, // Contains ReadData, ReadPermissions, ReadAttributes, ReadExtendedAttributes
                FileSystemRights.ReadAndExecute, // Contains Read and ExecuteFile
                FileSystemRights.ReadAttributes,
                FileSystemRights.ReadData, // Minimum right required to delete a file or directory. Equivalent to ListDirectory
                FileSystemRights.ReadExtendedAttributes,
                FileSystemRights.ReadPermissions,
                FileSystemRights.Synchronize, // CreateFile always requires Synchronize access
                FileSystemRights.TakeOwnership,
                FileSystemRights.Write, // Contains AppendData, WriteData, WriteAttributes, WriteExtendedAttributes
                FileSystemRights.WriteAttributes,
                FileSystemRights.WriteData, // Same as CreateFiles
                FileSystemRights.WriteExtendedAttributes,
                FileSystemRights.FullControl, // Contains Modify, DeleteSubdirectoriesAndFiles, Delete, ChangePermissions, TakeOwnership, Synchronize
            }
            select new object[] { rights };

        private static readonly FileMode[] s_writableModes = new[]
        {
            FileMode.Append,
            FileMode.Create,
            FileMode.CreateNew,
            FileMode.Truncate
            // Excludes OpenOrCreate because it has a different behavior compared to Create/CreateNew
        };

        private static readonly FileSystemRights[] s_readableRights = new[]
        {
            // Excludes combined rights
            FileSystemRights.ExecuteFile,
            FileSystemRights.ReadAttributes,
            FileSystemRights.ReadData,
            FileSystemRights.ReadExtendedAttributes,
            FileSystemRights.ReadPermissions
        };

        private static readonly FileSystemRights[] s_writableRights = new[]
        {
            // Excludes combined rights
            FileSystemRights.AppendData, // Same as CreateDirectories
            FileSystemRights.WriteAttributes,
            FileSystemRights.WriteData,
            FileSystemRights.WriteExtendedAttributes
        };

        private void FileInfo_Create_FileSecurity_ArgumentOutOfRangeException(string paramName, FileMode mode = FileMode.CreateNew, FileShare share = FileShare.None, int bufferSize = DefaultBufferSize)
        {
            var security = new FileSecurity();
            var info = new FileInfo(PathGenerator.GenerateTestFileName());
            Assert.Throws<ArgumentOutOfRangeException>(paramName, () =>
                info.Create(mode, FileSystemRights.FullControl, share, bufferSize, FileOptions.None, security));
        }

        private void FileInfo_Create_FileSecurity_ArgumentException(FileMode mode, FileSystemRights rights)
        {
            var security = new FileSecurity();
            var info = new FileInfo(PathGenerator.GenerateTestFileName());
            Assert.Throws<ArgumentException>(() =>
                info.Create(mode, rights, FileShare.None, DefaultBufferSize, FileOptions.None, security));
        }

        private void FileInfo_Create_FileSecurity_Successful(FileMode mode, FileSystemRights rights)
        {
            var security = new FileSecurity();
            var info = new FileInfo(PathGenerator.GenerateTestFileName());
            info.Create(mode, rights, FileShare.None, DefaultBufferSize, FileOptions.DeleteOnClose, security).Dispose();
        }

        private void Verify_FileSecurity_CreateFile(FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity expectedSecurity)
        {
            using var tempRootDir = new TempAclDirectory();
            string path = tempRootDir.GenerateSubItemPath();
            var fileInfo = new FileInfo(path);

            using (FileStream fs = fileInfo.Create(mode, rights, share, bufferSize, options, expectedSecurity))
            {
                Assert.True(fileInfo.Exists);
                tempRootDir.CreatedSubfiles.Add(fileInfo);

                var actualFileInfo = new FileInfo(path);
                FileSecurity actualSecurity = actualFileInfo.GetAccessControl();

                if (expectedSecurity != null)
                {
                    VerifyAccessSecurity(expectedSecurity, actualSecurity);
                }
                else
                {
                    int count = actualSecurity.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier)).Count;
                    Assert.Equal(0, count);
                }
            }
        }

        private void Verify_DirectorySecurity_CreateDirectory(DirectorySecurity expectedSecurity)
        {
            using var tempRootDir = new TempAclDirectory();
            string path = tempRootDir.GenerateSubItemPath();
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
