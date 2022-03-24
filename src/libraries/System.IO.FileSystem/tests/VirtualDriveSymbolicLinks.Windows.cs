// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    // Need to reuse the same virtual drive for all the test methods.
    // Creating and disposing one virtual drive per class achieves this.
    [PlatformSpecific(TestPlatforms.Windows)]
    [ConditionalClass(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks), nameof(MountHelper.IsSubstAvailable))]
    public class VirtualDrive_SymbolicLinks : BaseSymbolicLinks
    {
        private VirtualDriveHelper VirtualDrive { get; } = new VirtualDriveHelper();

        protected override void Dispose(bool disposing)
        {
            VirtualDrive.Dispose();
            base.Dispose(disposing);
        }

        [Theory]
        // false, false, false, false          // Target is not in virtual drive
        // false, false, true, false           // Target is not in virtual drive
        [InlineData(false, true, false, true)] // Immediate target expected, target is in virtual drive
        [InlineData(false, true, true, false)] // Final target expected, target is in virtual drive
        // true, false, false, false           // Target is not in virtual drive
        // true, false, true, false            // Target is not in virtual drive
        [InlineData(true, true, false, true)]  // Immediate target expected, target is in virtual drive
        [InlineData(true, true, true, false)]  // Final target expected, target is in virtual drive
        public void VirtualDrive_SymbolicLinks_LinkAndTarget(
            bool isLinkInVirtualDrive,
            bool isTargetInVirtualDrive,
            bool returnFinalTarget,
            bool isExpectedTargetPathVirtual)
        {
            string linkExpectedFolderPath = GetVirtualOrRealPath(isLinkInVirtualDrive);
            // File link
            string fileLinkName = GetRandomLinkName();
            string fileLinkPath = Path.Join(linkExpectedFolderPath, fileLinkName);
            // Directory link
            string dirLinkName = GetRandomLinkName();
            string dirLinkPath = Path.Join(linkExpectedFolderPath, dirLinkName);

            string targetExpectedFolderPath = GetVirtualOrRealPath(isTargetInVirtualDrive);
            // File target
            string fileTargetFileName = GetRandomFileName();
            string fileTargetPath = Path.Join(targetExpectedFolderPath, fileTargetFileName);
            // Directory target
            string dirTargetFileName = GetRandomDirName();
            string dirTargetPath = Path.Join(targetExpectedFolderPath, dirTargetFileName);

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

            // Verify the LinkTarget values of the link infos
            Assert.Equal(fileTargetPath, fileLinkInfo.LinkTarget);
            Assert.Equal(dirTargetPath, dirLinkInfo.LinkTarget);

            // When the target is in a virtual drive, and returnFinalTarget is true,
            // the expected target path is the real path, not the virtual path
            string expectedTargetPath = GetVirtualOrRealPath(isExpectedTargetPathVirtual);

            string expectedTargetFileInfoFullName = Path.Join(expectedTargetPath, fileTargetFileName);
            string expectedTargetDirectoryInfoFullName = Path.Join(expectedTargetPath, dirTargetFileName);

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
        // false, false, false, false, false           // Target is not in virtual drive
        // false, false, false, true, false            // Target is not in virtual drive
        [InlineData(false, false, true, false, false)] // Immediate target expected, middle link is NOT in virtual drive
        [InlineData(false, false, true, true, false)]  // Final target expected, target is in virtual drive
        // false, true, false, false, false            // Target is not in virtual drive
        // false, true, false, true, false             // Target is not in virtual drive
        [InlineData(false, true, true, false, true)]   // Immediate target expected, target is in virtual drive
        [InlineData(false, true, true, true, false)]   // Final target expected, target is in virtual drive
        // true, false, false, false, false            // Target is not in virtual drive
        // true, false, false, true, false             // Target is not in virtual drive
        [InlineData(true, false, true, false, false)]  // Immediate target expected, middle link is NOT in virtual drive
        [InlineData(true, false, true, true, false)]   // Final target expected, target is in virtual drive
        // true, true, false, false, false             // Target is not in virtual drive
        // true, true, false, true, false              // Target is not in virtual drive
        [InlineData(true, true, true, false, true)]    // Immediate target expected, target is in virtual drive
        [InlineData(true, true, true, true, false)]    // Final target expected, target is in virtual drive
        public void VirtualDrive_SymbolicLinks_WithIndirection(
            bool isFirstLinkInVirtualDrive,
            bool isMiddleLinkInVirtualDrive,
            bool isTargetInVirtualDrive,
            bool returnFinalTarget,
            bool isExpectedTargetPathVirtual)
        {
            string firstLinkExpectedFolderPath = GetVirtualOrRealPath(isFirstLinkInVirtualDrive);
            // File link
            string fileLinkPath = Path.Join(firstLinkExpectedFolderPath, GetRandomLinkName());
            // Directory link
            string dirLinkPath = Path.Join(firstLinkExpectedFolderPath, GetRandomLinkName());

            string middleLinkExpectedFolderPath = GetVirtualOrRealPath(isMiddleLinkInVirtualDrive);
            // File middle link
            string fileMiddleLinkFileName = GetRandomLinkName();
            string fileMiddleLinkPath = Path.Join(middleLinkExpectedFolderPath, fileMiddleLinkFileName);
            // Directory middle link
            string dirMiddleLinkFileName = GetRandomLinkName();
            string dirMiddleLinkPath = Path.Join(middleLinkExpectedFolderPath, dirMiddleLinkFileName);

            string targetExpectedFolderPath = GetVirtualOrRealPath(isTargetInVirtualDrive);
            // File final target
            string fileFinalTargetFileName = GetRandomFileName();
            string fileFinalTargetPath = Path.Join(targetExpectedFolderPath, fileFinalTargetFileName);
            // Directory final target
            string dirFinalTargetFileName = GetRandomDirName();
            string dirFinalTargetPath = Path.Join(targetExpectedFolderPath, dirFinalTargetFileName);

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

            // Verify the LinkTarget values of the link infos
            Assert.Equal(fileMiddleLinkPath, fileLinkInfo.LinkTarget);
            Assert.Equal(dirMiddleLinkPath, dirLinkInfo.LinkTarget);

            // When the target is in a virtual drive,
            // the expected target path is the real path, not the virtual path
            // When returnFinalTarget is true, the expected target path is the
            // resolved path from the final target in the chain of links
            string expectedTargetPath = GetVirtualOrRealPath(isExpectedTargetPathVirtual);

            string expectedTargetFileInfoFullName = Path.Join(expectedTargetPath,
                returnFinalTarget ? fileFinalTargetFileName : fileMiddleLinkFileName);

            string expectedTargetDirectoryInfoFullName = Path.Join(expectedTargetPath,
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

        private string GetVirtualOrRealPath(bool condition) => condition ? $"{VirtualDrive.VirtualDriveLetter}:" : VirtualDrive.VirtualDriveTargetDir;
    }
}
