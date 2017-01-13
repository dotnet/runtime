// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class File_Exists : FileSystemTest
    {
        #region Utilities

        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullAsPath_ReturnsFalse()
        {
            Assert.False(Exists(null));
        }

        [Fact]
        public void EmptyAsPath_ReturnsFalse()
        {
            Assert.False(Exists(string.Empty));
        }

        [Fact]
        public void NonExistentValidPath_ReturnsFalse()
        {
            Assert.All((IOInputs.GetValidPathComponentNames()), (path) =>
            {
                Assert.False(Exists(path), path);
            });
        }

        [Fact]
        public void ValidPathExists_ReturnsTrue()
        {
            Assert.All((IOInputs.GetValidPathComponentNames()), (component) =>
            {
                string path = Path.Combine(TestDirectory, component);
                FileInfo testFile = new FileInfo(path);
                testFile.Create().Dispose();
                Assert.True(Exists(path));
            });
        }

        [Theory, MemberData(nameof(PathsWithInvalidCharacters))]
        public void PathWithInvalidCharactersAsPath_ReturnsFalse(string invalidPath)
        {
            // Checks that errors aren't thrown when calling Exists() on paths with impossible to create characters
            Assert.False(Exists(invalidPath));

            Assert.False(Exists(".."));
            Assert.False(Exists("."));
        }

        [Fact]
        public void PathAlreadyExistsAsFile()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();

            Assert.True(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [Fact]
        public void PathEndsInTrailingSlash()
        {
            string path = GetTestFilePath() + Path.DirectorySeparatorChar;
            Assert.False(Exists(path));
        }

        [Fact]
        public void PathAlreadyExistsAsDirectory()
        {
            string path = GetTestFilePath();
            DirectoryInfo testDir = Directory.CreateDirectory(path);

            Assert.False(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [Fact]
        public void DirectoryLongerThanMaxDirectoryAsPath_DoesntThrow()
        {
            Assert.All((IOInputs.GetPathsLongerThanMaxDirectory(GetTestFilePath())), (path) =>
            {
                Assert.False(Exists(path));
            });
        }

        [Fact]
        public void DirectoryLongerThanMaxPathAsPath_DoesntThrow()
        {
            Assert.All((IOInputs.GetPathsLongerThanMaxPath(GetTestFilePath())), (path) =>
            {
                Assert.False(Exists(path), path);
            });
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinksMayExistIndependentlyOfTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();

            File.Create(path).Dispose();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: false));

            // Both the symlink and the target exist
            Assert.True(File.Exists(path), "path should exist");
            Assert.True(File.Exists(linkPath), "linkPath should exist");

            // Delete the target.  The symlink should still exist
            File.Delete(path);
            Assert.False(File.Exists(path), "path should now not exist");
            Assert.True(File.Exists(linkPath), "linkPath should still exist");

            // Now delete the symlink.
            File.Delete(linkPath);
            Assert.False(File.Exists(linkPath), "linkPath should no longer exist");
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix equivalent tested already in CreateDirectory
        public void WindowsNonSignificantWhiteSpaceAsPath_ReturnsFalse()
        {
            // Checks that errors aren't thrown when calling Exists() on impossible paths
            Assert.All((IOInputs.GetWhiteSpace()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [PlatformSpecific(CaseInsensitivePlatforms)]
        public void DoesCaseInsensitiveInvariantComparions()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.True(Exists(testFile.FullName));
            Assert.True(Exists(testFile.FullName.ToUpperInvariant()));
            Assert.True(Exists(testFile.FullName.ToLowerInvariant()));
        }

        [Fact]
        [PlatformSpecific(CaseSensitivePlatforms)]
        public void DoesCaseSensitiveComparions()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.True(Exists(testFile.FullName));
            Assert.False(Exists(testFile.FullName.ToUpperInvariant()));
            Assert.False(Exists(testFile.FullName.ToLowerInvariant()));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // In Windows, trailing whitespace in a path is trimmed
        public void TrimTrailingWhitespacePath()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.All((IOInputs.GetWhiteSpace()), (component) =>
            {
                Assert.True(Exists(testFile.FullName + component)); // string concat in case Path.Combine() trims whitespace before Exists gets to it
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // alternate data stream
        public void PathWithAlternateDataStreams_ReturnsFalse()
        {
            Assert.All((IOInputs.GetPathsWithAlternativeDataStreams()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [OuterLoop]
        [PlatformSpecific(TestPlatforms.Windows)] // device names
        public void PathWithReservedDeviceNameAsPath_ReturnsFalse()
        {
            Assert.All((IOInputs.GetPathsWithReservedDeviceNames()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // UNC paths
        public void UncPathWithoutShareNameAsPath_ReturnsFalse()
        {
            Assert.All((IOInputs.GetUncPathsWithoutShareName()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // max directory length not fixed on Unix
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_ReturnsFalse()
        {
            Assert.All((IOInputs.GetPathsWithComponentLongerThanMaxComponent()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void FalseForNonRegularFile()
        {
            string fileName = GetTestFilePath();
            Assert.Equal(0, mkfifo(fileName, 0));
            Assert.True(File.Exists(fileName));
        }

        #endregion
    }
}
