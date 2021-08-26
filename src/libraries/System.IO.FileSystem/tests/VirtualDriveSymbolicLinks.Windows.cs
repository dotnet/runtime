// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    // Need to reuse the same virtual drive for all the test methods.
    // Creating and disposing one virtual drive per class achieves this.
    [PlatformSpecific(TestPlatforms.Windows)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
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

        [Theory]
        //[InlineData(false, false, false, false)] // Target is not in virtual drive
        // [InlineData(false, false, true, false)] // Target is not in virtual drive
        [InlineData(false, true, false, true)]     // Immediate target expected, target is in virtual drive
        [InlineData(false, true, true, false)]     // Final target expected, target is in virtual drive
        // [InlineData(true, false, false, false)] // Target is not in virtual drive
        // [InlineData(true, false, true, false)]  // Target is not in virtual drive
        [InlineData(true, true, false, true)]      // Immediate target expected, target is in virtual drive
        [InlineData(true, true, true, false)]      // Final target expected, target is in virtual drive
        public void VirtualDrive_SymbolicLinks_LinkAndTarget(bool isLinkInVirtualDrive, bool isTargetInVirtualDrive, bool returnFinalTarget, bool isExpectedTargetPathVirtual)
        {
            // File link
            string fileLinkName = GetRandomLinkName();
            string fileLinkPath = Path.Join(
                isLinkInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                fileLinkName);

            // Directory link
            string dirLinkName = GetRandomLinkName();
            string dirLinkPath = Path.Join(
                isLinkInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                dirLinkName);

            // File target
            string fileTargetFileName = GetRandomFileName();
            string fileTargetPath = Path.Join(
                isTargetInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                fileTargetFileName);

            // Directory target
            string dirTargetFileName = GetRandomDirName();
            string dirTargetPath = Path.Join(
                isTargetInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                dirTargetFileName);

            // Create targets
            File.Create(fileTargetPath).Dispose();
            Directory.CreateDirectory(dirTargetPath);

            // Create links
            FileInfo fileLinkInfo = new FileInfo(fileLinkPath);
            fileLinkInfo.CreateAsSymbolicLink(fileTargetPath);
            DirectoryInfo dirLinkInfo = new DirectoryInfo(dirLinkPath);
            dirLinkInfo.CreateAsSymbolicLink(dirTargetPath);

            // The expected results depend on the target location and the value of returnFinalTarget

            // LinkTarget always retrieves the immediate target, so the expected value
            // is always the path that was provided by the user for the target
            string expectedFileTargetPath = fileTargetPath;
            string expectedDirTargetPath = dirTargetPath;

            // Verify the LinkTarget values of the link infos
            Assert.Equal(expectedFileTargetPath, fileLinkInfo.LinkTarget);
            Assert.Equal(expectedDirTargetPath, dirLinkInfo.LinkTarget);

            // When the target is in a virtual drive, and returnFinalTarget is true,
            // the expected target path is the real path, not the virtual path
            string expectedTargetFileInfoFullName = Path.Join(
                isExpectedTargetPathVirtual ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                fileTargetFileName);

            string expectedTargetDirectoryInfoFullName = Path.Join(
                isExpectedTargetPathVirtual ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                dirTargetFileName);

            // Verify target infos from link info instances
            FileSystemInfo? targetFileInfoFromFileInfoLink = fileLinkInfo.ResolveLinkTarget(returnFinalTarget);
            FileSystemInfo? targetDirInfoFromDirInfoLink = dirLinkInfo.ResolveLinkTarget(returnFinalTarget);

            Assert.True(targetFileInfoFromFileInfoLink is FileInfo);
            Assert.True(targetDirInfoFromDirInfoLink is DirectoryInfo);

            Assert.Equal(expectedTargetFileInfoFullName, targetFileInfoFromFileInfoLink.FullName);
            Assert.Equal(expectedTargetDirectoryInfoFullName, targetDirInfoFromDirInfoLink.FullName);

            // Verify targets infos via static methods
            FileSystemInfo? targetFileInfoFromFile = File.ResolveLinkTarget(fileLinkPath, returnFinalTarget);
            FileSystemInfo? targetFileInfoFromDirectory = Directory.ResolveLinkTarget(dirLinkPath, returnFinalTarget);

            Assert.True(targetFileInfoFromFile is FileInfo);
            Assert.True(targetFileInfoFromDirectory is DirectoryInfo);

            Assert.Equal(expectedTargetFileInfoFullName, targetFileInfoFromFile.FullName);
            Assert.Equal(expectedTargetDirectoryInfoFullName, targetFileInfoFromDirectory.FullName);
        }


        [Theory]
        //[InlineData(false, false, false, false, false)] // Target is not in virtual drive
        // [InlineData(false, false, false, true, false)] // Target is not in virtual drive
        [InlineData(false, false, true, false, false)]     // Immediate target expected, middle link is NOT in virtual drive
        [InlineData(false, false, true, true, false)]     // Final target expected, target is in virtual drive
        //[InlineData(false, true, false, false, false)]  // Target is not in virtual drive
        // [InlineData(false, true, false, true, false)]  // Target is not in virtual drive
        [InlineData(false, true, true, false, true)]      // Immediate target expected, target is in virtual drive
        [InlineData(false, true, true, true, false)]      // Final target expected, target is in virtual drive
        // [InlineData(true, false, false, false, false)] // Target is not in virtual drive
        // [InlineData(true, false, false, true, false)]  // Target is not in virtual drive
        [InlineData(true, false, true, false, false)]     // Immediate target expected, middle link is NOT in virtual drive
        [InlineData(true, false, true, true, false)]      // Final target expected, target is in virtual drive
        // [InlineData(true, true, false, false, false)]  // Target is not in virtual drive
        // [InlineData(true, true, false, true, false)]   // Target is not in virtual drive
        [InlineData(true, true, true, false, true)]       // Immediate target expected, target is in virtual drive
        [InlineData(true, true, true, true, false)]       // Final target expected, target is in virtual drive
        public void VirtualDrive_SymbolicLinks_WithIndirection(bool isFirstLinkInVirtualDrive, bool isMiddleLinkInVirtualDrive, bool isTargetInVirtualDrive, bool returnFinalTarget, bool isExpectedTargetPathVirtual)
        {
            // File link
            string fileLinkName = GetRandomLinkName();
            string fileLinkPath = Path.Join(
                isFirstLinkInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                fileLinkName);

            // Directory link
            string dirLinkName = GetRandomLinkName();
            string dirLinkPath = Path.Join(
                isFirstLinkInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                dirLinkName);

            // File middle link
            string fileMiddleLinkFileName = GetRandomLinkName();
            string fileMiddleLinkPath = Path.Join(
                isMiddleLinkInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                fileMiddleLinkFileName);

            // Directory middle link
            string dirMiddleLinkFileName = GetRandomLinkName();
            string dirMiddleLinkPath = Path.Join(
                isMiddleLinkInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                dirMiddleLinkFileName);

            // File final target
            string fileFinalTargetFileName = GetRandomFileName();
            string fileFinalTargetPath = Path.Join(
                isTargetInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                fileFinalTargetFileName);

            // Directory final target
            string dirFinalTargetFileName = GetRandomDirName();
            string dirFinalTargetPath = Path.Join(
                isTargetInVirtualDrive ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                dirFinalTargetFileName);

            // Create targets
            File.Create(fileFinalTargetPath).Dispose();
            Directory.CreateDirectory(dirFinalTargetPath);

            // Create initial links
            FileInfo fileLinkInfo = new FileInfo(fileLinkPath);
            fileLinkInfo.CreateAsSymbolicLink(fileMiddleLinkPath);
            DirectoryInfo dirLinkInfo = new DirectoryInfo(dirLinkPath);
            dirLinkInfo.CreateAsSymbolicLink(dirMiddleLinkPath);

            // Create middle links
            FileInfo fileMiddleLinkInfo = new FileInfo(fileMiddleLinkPath);
            fileMiddleLinkInfo.CreateAsSymbolicLink(fileFinalTargetPath);
            DirectoryInfo dirMiddleLinkInfo = new DirectoryInfo(dirMiddleLinkPath);
            dirMiddleLinkInfo.CreateAsSymbolicLink(dirFinalTargetPath);

            // The expected results depend on the target location and the value of returnFinalTarget

            // LinkTarget always retrieves the immediate target, so the expected value
            // is always the path that was provided by the user for the middle link
            string expectedFileTargetPath = fileMiddleLinkPath;
            string expectedDirTargetPath = dirMiddleLinkPath;

            // Verify the LinkTarget values of the link infos
            Assert.Equal(expectedFileTargetPath, fileLinkInfo.LinkTarget);
            Assert.Equal(expectedDirTargetPath, dirLinkInfo.LinkTarget);

            // When the target is in a virtual drive,
            // the expected target path is the real path, not the virtual path
            // When returnFinalTarget is true, the expected target path is the
            // resolved path from the final target in the chain of links
            string expectedTargetFileInfoFullName = Path.Join(
                isExpectedTargetPathVirtual ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                returnFinalTarget ? fileFinalTargetFileName : fileMiddleLinkFileName);

            string expectedTargetDirectoryInfoFullName = Path.Join(
                isExpectedTargetPathVirtual ? $"{VirtualDriveLetter}:" : VirtualDriveTargetDir,
                returnFinalTarget ? dirFinalTargetFileName : dirMiddleLinkFileName);

            // Verify target infos from link info instances
            FileSystemInfo? targetFileInfoFromFileInfoLink = fileLinkInfo.ResolveLinkTarget(returnFinalTarget);
            FileSystemInfo? targetDirInfoFromDirInfoLink = dirLinkInfo.ResolveLinkTarget(returnFinalTarget);

            Assert.True(targetFileInfoFromFileInfoLink is FileInfo);
            Assert.True(targetDirInfoFromDirInfoLink is DirectoryInfo);

            Assert.Equal(expectedTargetFileInfoFullName, targetFileInfoFromFileInfoLink.FullName);
            Assert.Equal(expectedTargetDirectoryInfoFullName, targetDirInfoFromDirInfoLink.FullName);

            // Verify targets infos via static methods
            FileSystemInfo? targetFileInfoFromFile = File.ResolveLinkTarget(fileLinkPath, returnFinalTarget);
            FileSystemInfo? targetFileInfoFromDirectory = Directory.ResolveLinkTarget(dirLinkPath, returnFinalTarget);

            Assert.True(targetFileInfoFromFile is FileInfo);
            Assert.True(targetFileInfoFromDirectory is DirectoryInfo);

            Assert.Equal(expectedTargetFileInfoFullName, targetFileInfoFromFile.FullName);
            Assert.Equal(expectedTargetDirectoryInfoFullName, targetFileInfoFromDirectory.FullName);
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
