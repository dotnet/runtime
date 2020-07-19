// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Theory,
            MemberData(nameof(ValidPathComponentNames))]
        public void NonExistentValidPath_ReturnsFalse(string path)
        {
            Assert.False(Exists(path), path);
        }

        [Theory,
            MemberData(nameof(ValidPathComponentNames))]
        public void ValidPathExists_ReturnsTrue(string component)
        {
            string path = Path.Combine(TestDirectory, component);
            FileInfo testFile = new FileInfo(path);
            testFile.Create().Dispose();
            Assert.True(Exists(path));
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
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PathEndsInAltTrailingSlash_Windows()
        {
            string path = GetTestFilePath() + Path.DirectorySeparatorChar;
            Assert.False(Exists(path));
        }

        [Fact]
        public void PathEndsInTrailingSlash_AndExists()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.False(Exists(path + Path.DirectorySeparatorChar));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PathEndsInAltTrailingSlash_AndExists_Windows()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.False(Exists(path + Path.DirectorySeparatorChar));
        }

        [Fact]
        public void PathAlreadyExistsAsDirectory()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

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

        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix equivalent tested already in CreateDirectory
        public void WindowsNonSignificantWhiteSpaceAsPath_ReturnsFalse(string component)
        {
            // Checks that errors aren't thrown when calling Exists() on impossible paths
            Assert.False(Exists(component));

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

        [Theory,
           MemberData(nameof(NonControlWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrailingWhiteSpace_NotTrimmed(string component)
        {
            // In .NET Core we don't trim anything other than space (' ')
            string path = GetTestFilePath() + component;
            FileInfo testFile = new FileInfo(path);
            testFile.Create().Dispose();

            Assert.True(Exists(path));
        }

        [Theory,
           MemberData(nameof(SimpleWhiteSpace))] //*Just* spaces
        [PlatformSpecific(TestPlatforms.Windows)] // In Windows, trailing whitespace in a path is trimmed
        public void TrailingSpace_Trimmed(string component)
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            // Windows will trim trailing spaces
            Assert.True(Exists(testFile.FullName + component));
        }


        [Theory,
            MemberData(nameof(PathsWithColons))]
        [PlatformSpecific(TestPlatforms.Windows)] // alternate data stream
        public void PathWithAlternateDataStreams_ReturnsFalse(string component)
        {
            Assert.False(Exists(component));
        }

        [Theory,
            MemberData(nameof(PathsWithReservedDeviceNames))]
        [OuterLoop]
        [PlatformSpecific(TestPlatforms.Windows)] // device names
        public void PathWithReservedDeviceNameAsPath_ReturnsFalse(string component)
        {
            Assert.False(Exists(component));
        }

        [Theory,
            MemberData(nameof(UncPathsWithoutShareName))]
        public void UncPathWithoutShareNameAsPath_ReturnsFalse(string component)
        {
            Assert.False(Exists(component));
        }

        [Theory,
            MemberData(nameof(PathsWithComponentLongerThanMaxComponent))]
        [PlatformSpecific(TestPlatforms.Windows)] // max directory length not fixed on Unix
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_ReturnsFalse(string component)
        {
            Assert.False(Exists(component));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes
        public void FalseForNonRegularFile()
        {
            string fileName = GetTestFilePath();
            Assert.Equal(0, mkfifo(fileName, 0));
            Assert.True(File.Exists(fileName));
        }

        #endregion
    }
}
