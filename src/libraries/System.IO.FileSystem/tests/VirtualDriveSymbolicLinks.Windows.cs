// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    // Need to reuse the same virtual drive for all the test methods.
    // Creating and disposing one virtual drive per class achieves this.
    [PlatformSpecific(TestPlatforms.Windows)]
    public class VirtualDrive_SymbolicLinks : BaseSymbolicLinks
    {
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (VirtualDriveLetter != default)
                {
                    MountHelper.DeleteVirtualDrive(VirtualDriveLetter);
                    Directory.Delete(VirtualDriveTargetDir, recursive: true);
                }
            }
            catch { } // avoid exceptions on dispose
            base.Dispose(disposing);
        }

        // When the immediate target is requested (via LinkTarget or using returnFinalTarget: false),
        // the returned path will point to the target using the virtual drive path
        // The only case of returnFinalTarget: true that will return a target using the virtual drive, is when both
        // the link and the target reside in the virtual drive
        [Theory]
        [InlineData(true, true, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        public void CreateSymbolicLinkVD_VirtualPathReturned(bool linkInVD, bool targetInVD, bool returnFinalTarget)
        {
            // File
            CreateSymbolicLinkVD_VirtualPathReturned_Internal(
                linkInVD,
                targetInVD,
                returnFinalTarget,
                isDirectoryTest: false,
                CreateFile,
                CreateFileSymbolicLink,
                AssertFileIsCorrectTypeAndDirectoryAttribute);

            // Directory
            CreateSymbolicLinkVD_VirtualPathReturned_Internal(
                linkInVD,
                targetInVD,
                returnFinalTarget,
                isDirectoryTest: true,
                CreateDirectory,
                CreateDirectorySymbolicLink,
                AssertDirectoryIsCorrectTypeAndDirectoryAttribute);

            // FileInfo
            CreateSymbolicLinkVD_VirtualPathReturned_Internal(
                linkInVD,
                targetInVD,
                returnFinalTarget,
                isDirectoryTest: false,
                CreateFile,
                CreateFileInfoSymbolicLink,
                AssertFileIsCorrectTypeAndDirectoryAttribute);

            // DirectoryInfo
            CreateSymbolicLinkVD_VirtualPathReturned_Internal(
                linkInVD,
                targetInVD,
                returnFinalTarget,
                isDirectoryTest: true,
                CreateDirectory,
                CreateDirectoryInfoSymbolicLink,
                AssertDirectoryIsCorrectTypeAndDirectoryAttribute);
        }

        // When the link target resides in a virtual drive, and returnFinalTarget: true is used,
        // All segments of the target path get resolved, including mount points, to their real path
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateSymbolicLinkVD_ResolvedPathReturned(bool linkInVD)
        {
            // File
            CreateSymbolicLinkVD_ResolvedPathReturned_Internal(
                linkInVD,
                targetInVD: true,
                returnFinalTarget: true,
                isDirectoryTest: false,
                CreateFile,
                CreateFileSymbolicLink,
                AssertFileIsCorrectTypeAndDirectoryAttribute);

            //.Directory
            CreateSymbolicLinkVD_ResolvedPathReturned_Internal(
                linkInVD,
                targetInVD: true,
                returnFinalTarget: true,
                isDirectoryTest: true,
                CreateDirectory,
                CreateDirectorySymbolicLink,
                AssertDirectoryIsCorrectTypeAndDirectoryAttribute);

            // FileInfo
            CreateSymbolicLinkVD_ResolvedPathReturned_Internal(
                linkInVD,
                targetInVD: true,
                returnFinalTarget: true,
                isDirectoryTest: false,
                CreateFile,
                CreateFileInfoSymbolicLink,
                AssertFileIsCorrectTypeAndDirectoryAttribute);

            //.DirectoryInfo
            CreateSymbolicLinkVD_ResolvedPathReturned_Internal(
                linkInVD,
                targetInVD: true,
                returnFinalTarget: true,
                isDirectoryTest: true,
                CreateDirectory,
                CreateDirectoryInfoSymbolicLink,
                AssertDirectoryIsCorrectTypeAndDirectoryAttribute);
        }

        private void CreateSymbolicLinkVD_VirtualPathReturned_Internal(
            bool linkInVD,
            bool targetInVD,
            bool returnFinalTarget,
            bool isDirectoryTest,
            TargetCreationMethod targetCreationMethod,
            SymbolicLinkCreationMethod symbolicLinkCreationMethod,
            AssertIsCorrectTypeAndDirectoryAttributeMethod assertIsCorrectTypeAndDirectoryAttributeMethod)
        {
            (string targetPath, FileSystemInfo linkInfo, FileSystemInfo targetInfo) =
                   CreateSymbolicLinkVD_Internal(
                       linkInVD,
                       targetInVD,
                       returnFinalTarget,
                       isDirectoryTest,
                       targetCreationMethod,
                       symbolicLinkCreationMethod,
                       assertIsCorrectTypeAndDirectoryAttributeMethod);

            Assert.Equal(targetPath, linkInfo.LinkTarget);
            Assert.Equal(targetPath, targetInfo.FullName);
        }

        private void CreateSymbolicLinkVD_ResolvedPathReturned_Internal(
            bool linkInVD,
            bool targetInVD,
            bool returnFinalTarget,
            bool isDirectoryTest,
            TargetCreationMethod targetCreationMethod,
            SymbolicLinkCreationMethod symbolicLinkCreationMethod,
            AssertIsCorrectTypeAndDirectoryAttributeMethod assertIsCorrectTypeAndDirectoryAttributeMethod)
        {
            (string targetPath, FileSystemInfo linkInfo, FileSystemInfo targetInfo) =
                  CreateSymbolicLinkVD_Internal(
                      linkInVD,
                      targetInVD,
                      returnFinalTarget,
                      isDirectoryTest,
                      targetCreationMethod,
                      symbolicLinkCreationMethod,
                      assertIsCorrectTypeAndDirectoryAttributeMethod);

            Assert.Equal(targetPath, linkInfo.LinkTarget);
            string resolvedTargetPath = Path.Join(VirtualDriveTargetDir, Path.GetFileName(targetPath));
            Assert.Equal(resolvedTargetPath, targetInfo.FullName);
        }

        private (string, FileSystemInfo, FileSystemInfo) CreateSymbolicLinkVD_Internal(
            bool linkInVD,
            bool targetInVD,
            bool returnFinalTarget,
            bool isDirectoryTest,
            TargetCreationMethod createFileOrDirectory,
            SymbolicLinkCreationMethod createSymbolicLink,
            AssertIsCorrectTypeAndDirectoryAttributeMethod assertIsCorrectTypeAndDirectoryAttribute)
        {
            string targetFileName = isDirectoryTest ? GetRandomDirName() : GetRandomFileName();
            string pathToTarget = Path.Join(
                targetInVD ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                targetFileName);

            string linkFileName = GetRandomLinkName();
            string linkPath = Path.Join(
                linkInVD ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                linkFileName);

            createFileOrDirectory(pathToTarget);

            FileSystemInfo? link = createSymbolicLink(linkPath, pathToTarget);
            assertIsCorrectTypeAndDirectoryAttribute(link);

            FileSystemInfo? target = link.ResolveLinkTarget(returnFinalTarget);
            assertIsCorrectTypeAndDirectoryAttribute(target);

            return (pathToTarget, link, target);
        }

        private delegate void TargetCreationMethod(string targetPath);

        private void CreateFile(string targetPath) => File.Create(targetPath).Dispose();

        private void CreateDirectory(string targetPath) => Directory.CreateDirectory(targetPath);

        private delegate FileSystemInfo SymbolicLinkCreationMethod(string path, string pathToTarget);

        private FileSystemInfo CreateFileSymbolicLink(string path, string pathToTarget) => File.CreateSymbolicLink(path, pathToTarget);

        private FileSystemInfo CreateDirectorySymbolicLink(string path, string pathToTarget) => Directory.CreateSymbolicLink(path, pathToTarget);

        private FileSystemInfo CreateFileInfoSymbolicLink(string path, string pathToTarget)
        {
            FileInfo link = new(path);
            link.CreateAsSymbolicLink(pathToTarget);
            return link;
        }

        private FileSystemInfo CreateDirectoryInfoSymbolicLink(string path, string pathToTarget)
        {
            DirectoryInfo link = new(path);
            link.CreateAsSymbolicLink(pathToTarget);
            return link;
        }

        private delegate void AssertIsCorrectTypeAndDirectoryAttributeMethod(FileSystemInfo link);

        private void AssertFileIsCorrectTypeAndDirectoryAttribute(FileSystemInfo link)
        {
            if (link.Exists)
            {
                Assert.False(link.Attributes.HasFlag(FileAttributes.Directory));
            }
            Assert.True(link is FileInfo);
        }

        private void AssertDirectoryIsCorrectTypeAndDirectoryAttribute(FileSystemInfo link)
        {
            if (link.Exists)
            {
                Assert.True(link.Attributes.HasFlag(FileAttributes.Directory));
            }
            Assert.True(link is DirectoryInfo);
        }

        // Temporary Windows directory that can be mounted to a drive letter using the subst command
        private string? _virtualDriveTargetDir = null;
        private string VirtualDriveTargetDir
        {
            get
            {
                if (_virtualDriveTargetDir == null)
                {
                    // Create a folder inside the temp directory so that it can be mounted to a drive letter with subst
                    _virtualDriveTargetDir = Path.Join(Path.GetTempPath(), GetRandomDirName());
                    Directory.CreateDirectory(_virtualDriveTargetDir);
                }

                return _virtualDriveTargetDir;
            }
        }

        // Windows drive letter that points to a mounted directory using the subst command
        private char _virtualDriveLetter = default;
        private char VirtualDriveLetter
        {
            get
            {
                if (_virtualDriveLetter == default)
                {
                    // Mount the folder to a drive letter
                    _virtualDriveLetter = MountHelper.CreateVirtualDrive(VirtualDriveTargetDir);
                }
                return _virtualDriveLetter;
            }
        }
    }
}
