// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectory_Roundtrip_Tests : TarTestsBase
    {
        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData("./file.txt", "subDirectory")]
        [InlineData("../file.txt", "subDirectory")]
        [InlineData("../file.txt", "subDirectory1/subDirectory1.1")]
        [InlineData("./file.txt", "subDirectory1/subDirectory1.1")]
        [InlineData("./file.txt", null)]
        public void SymlinkRelativeTargets_InsideTheArchive_RoundtripsSuccessfully(string symlinkTargetPath, string subDirectory)
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateDirectory(sourceDirectoryName);

            string destinationDirectoryName = Path.Join(root.Path, "destinationDirectory");
            Directory.CreateDirectory(destinationDirectoryName);

            string sourceSubDirectory = Path.Join(sourceDirectoryName, subDirectory);
            if(subDirectory != null)  Directory.CreateDirectory(sourceSubDirectory);

            File.Create(Path.Join(sourceDirectoryName, subDirectory, symlinkTargetPath)).Dispose();
            File.CreateSymbolicLink(Path.Join(sourceSubDirectory, "linkToFile"), symlinkTargetPath);

            TarFile.CreateFromDirectory(sourceDirectoryName, destinationArchive, includeBaseDirectory: false);

            using FileStream archiveStream = File.OpenRead(destinationArchive);
            TarFile.ExtractToDirectory(archiveStream, destinationDirectoryName, overwriteFiles: true);

            string destinationSubDirectory = Path.Join(destinationDirectoryName, subDirectory);
            string symlinkPath = Path.Join(destinationSubDirectory, "linkToFile");
            Assert.True(File.Exists(symlinkPath));

            FileInfo? fileInfo = new(symlinkPath);
            Assert.Equal(symlinkTargetPath, fileInfo.LinkTarget);

            FileSystemInfo? symlinkTarget = File.ResolveLinkTarget(symlinkPath, returnFinalTarget: true);
            Assert.True(File.Exists(symlinkTarget.FullName));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData("../file.txt", null)]
        [InlineData("../../file.txt", "subDirectory")]
        public void SymlinkRelativeTargets_OutsideTheArchive_Fails(string symlinkTargetPath, string subDirectory)
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateDirectory(sourceDirectoryName);

            string destinationDirectoryName = Path.Join(root.Path, "destinationDirectory");
            Directory.CreateDirectory(destinationDirectoryName);

            string sourceSubDirectory = Path.Join(sourceDirectoryName, subDirectory);
            if(subDirectory != null)  Directory.CreateDirectory(sourceSubDirectory);

            File.CreateSymbolicLink(Path.Join(sourceSubDirectory, "linkToFile"), symlinkTargetPath);

            TarFile.CreateFromDirectory(sourceDirectoryName, destinationArchive, includeBaseDirectory: false);

            using FileStream archiveStream = File.OpenRead(destinationArchive);
            Exception exception = Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(archiveStream, destinationDirectoryName, overwriteFiles: true));

            Assert.Equal(SR.Format(SR.TarExtractingResultsLinkOutside, symlinkTargetPath, destinationDirectoryName), exception.Message);
        }
    }
}
