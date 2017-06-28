// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using Xunit;

namespace System.IO
{
    public class FileSystemAclExtensionsTests
    {
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
    }
}
