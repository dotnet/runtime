// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_Exists : FileSystemTest
    {
        #region Utilities

        public bool Exists(string path)
        {
            return Directory.Exists(path);
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
            Directory.CreateDirectory(path);
            Assert.True(Exists(path));
        }

        [Theory, MemberData(nameof(PathsWithInvalidCharacters))]
        public void PathWithInvalidCharactersAsPath_ReturnsFalse(string invalidPath)
        {
            // Checks that errors aren't thrown when calling Exists() on paths with impossible to create characters
            char[] trimmed = { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0 };
            Assert.False(Exists(invalidPath));
            if (!trimmed.Contains(invalidPath.ToCharArray()[0]))
                Assert.False(Exists(TestDirectory + Path.DirectorySeparatorChar + invalidPath));
        }

        [Fact]
        public void PathAlreadyExistsAsFile()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();

            Assert.False(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [Fact]
        public void PathAlreadyExistsAsDirectory()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

            Assert.True(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [Fact]
        public void DotAsPath_ReturnsTrue()
        {
            Assert.True(Exists(Path.Combine(TestDirectory, ".")));
        }

        [Fact]
        public void DirectoryGetCurrentDirectoryAsPath_ReturnsTrue()
        {
            Assert.True(Exists(Directory.GetCurrentDirectory()));
        }

        [Fact]
        public void DotDotAsPath_ReturnsTrue()
        {
            Assert.True(Exists(Path.Combine(TestDirectory, GetTestFileName(), "..")));
        }

        [Fact]
        public void DirectoryLongerThanMaxLongPath_DoesntThrow()
        {
            Assert.All((IOInputs.GetPathsLongerThanMaxLongPath(GetTestFilePath())), (path) =>
            {
                Assert.False(Exists(path), path);
            });
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinksMayExistIndependentlyOfTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();

            Directory.CreateDirectory(path);
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: true));

            // Both the symlink and the target exist
            Assert.True(Directory.Exists(path), "path should exist");
            Assert.True(Directory.Exists(linkPath), "linkPath should exist");
            Assert.False(File.Exists(linkPath));

            // Delete the target.  The symlink should still exist.  On Unix, the symlink will now be
            // considered a file (since it's broken and we don't know what it'll eventually point to).
            Directory.Delete(path);
            Assert.False(Directory.Exists(path), "path should now not exist");
            if (OperatingSystem.IsWindows())
            {
                Assert.True(Directory.Exists(linkPath), "linkPath should still exist as a directory");
                Assert.False(File.Exists(linkPath), "linkPath should not be a file");
            }
            else
            {
                Assert.False(Directory.Exists(linkPath), "linkPath should no longer be a directory");
                Assert.True(File.Exists(linkPath), "linkPath should now be a file");
            }

            // Now delete the symlink.
            // On Unix, deleting the symlink should fail, because it's not a directory, it's a file.
            // On Windows, it should succeed.
            try
            {
                Directory.Delete(linkPath);
                Assert.True(OperatingSystem.IsWindows(), "Should only succeed on Windows");
            }
            catch (IOException)
            {
                Assert.False(OperatingSystem.IsWindows(), "Should only fail on Unix");
                File.Delete(linkPath);
            }

            Assert.False(Directory.Exists(linkPath), "linkPath should no longer exist as a directory");
            Assert.False(File.Exists(linkPath), "linkPath should no longer exist as a file");
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymlinkToNewDirectory()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

            string linkPath = GetTestFilePath();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: true));

            Assert.True(Directory.Exists(path));
            Assert.True(Directory.Exists(linkPath));
        }

        #endregion

        #region PlatformSpecific

        [ConditionalTheory(nameof(UsingNewNormalization)),
            MemberData(nameof(ValidPathComponentNames))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Extended path exists
        public void ValidExtendedPathExists_ReturnsTrue(string component)
        {
            string path = IOInputs.ExtendedPrefix + Path.Combine(TestDirectory, "extended", component);
            Directory.CreateDirectory(path);
            Assert.True(Exists(path));
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Extended path already exists as file
        public void ExtendedPathAlreadyExistsAsFile()
        {
            string path = IOInputs.ExtendedPrefix + GetTestFilePath();
            File.Create(path).Dispose();

            Assert.False(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Extended path already exists as directory
        public void ExtendedPathAlreadyExistsAsDirectory()
        {
            string path = IOInputs.ExtendedPrefix + GetTestFilePath();
            Directory.CreateDirectory(path);

            Assert.True(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Long directory path doesn't throw on Exists
        public void DirectoryLongerThanMaxDirectoryAsPath_DoesntThrow()
        {
            Assert.All((IOInputs.GetPathsLongerThanMaxDirectory(GetTestFilePath())), (path) =>
            {
                Assert.False(Exists(path));
            });
        }

        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix equivalent tested already in CreateDirectory
        public void WindowsWhiteSpaceAsPath_ReturnsFalse(string component)
        {
            // Checks that errors aren't thrown when calling Exists() on impossible paths
            Assert.False(Exists(component));

        }

        [Fact]
        [PlatformSpecific(CaseInsensitivePlatforms)]
        public void DoesCaseInsensitiveInvariantComparisons()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.True(Exists(testDir.FullName));
            Assert.True(Exists(testDir.FullName.ToUpperInvariant()));
            Assert.True(Exists(testDir.FullName.ToLowerInvariant()));
        }

        [Fact]
        [PlatformSpecific(CaseSensitivePlatforms)]
        public void DoesCaseSensitiveComparisons()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.True(Exists(testDir.FullName));
            Assert.False(Exists(testDir.FullName.ToUpperInvariant()));
            Assert.False(Exists(testDir.FullName.ToLowerInvariant()));
        }

        [ConditionalTheory(nameof(UsingNewNormalization)),
            MemberData(nameof(SimpleWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)] // In Windows, trailing whitespace in a path is trimmed appropriately
        public void TrailingWhitespaceExistence_SimpleWhiteSpace(string component)
        {
            // This test relies on \\?\ support

            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string path = GetTestFilePath(memberName: "Extended") + component;
            testDir = Directory.CreateDirectory(IOInputs.ExtendedPrefix + path);
            Assert.False(Exists(path), path);
            Assert.True(Exists(testDir.FullName));
        }

        [Theory,
            MemberData(nameof(NonControlWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void NonControlWhiteSpaceExists(string component)
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath() + component);

            string path = testDir.FullName;
            Assert.True(Exists(path), "directory with non control whitespace should exist");
        }

        [Theory,
            MemberData(nameof(SimpleWhiteSpace))] // *Just spaces*
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrailingSpaceExists(string component)
        {
            // Windows trims spaces
            string path = GetTestFilePath();
            Directory.CreateDirectory(path + component);
            Assert.True(Exists(path), "can find without space");
            Assert.True(Exists(path + component), "can find with space");
        }

        [Theory,
            MemberData(nameof(WhiteSpace))]
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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // max directory length not fixed on Unix
        public void DirectoryEqualToMaxDirectory_ReturnsTrue()
        {
            // Creates directories up to the maximum directory length all at once
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string path = IOServices.GetPath(testDir.FullName, IOInputs.MaxDirectory);
            Directory.CreateDirectory(path);
            Assert.True(Exists(path));
        }

        [Theory,
            MemberData(nameof(PathsWithComponentLongerThanMaxComponent))]
        [PlatformSpecific(TestPlatforms.Windows)] // max directory length not fixed on Unix
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_ReturnsFalse(string component)
        {
            Assert.False(Exists(component));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/14378")]
        [PlatformSpecific(TestPlatforms.Windows)] // drive labels
        public void NotReadyDriveAsPath_ReturnsFalse()
        {
            var drive = IOServices.GetNotReadyDrive();
            if (drive == null)
            {
                Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
                return;
            }

            bool result = Exists(drive);

            Assert.False(result);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/14378")]
        [PlatformSpecific(TestPlatforms.Windows)] // drive labels
        public void SubdirectoryOnNotReadyDriveAsPath_ReturnsFalse()
        {
            var drive = IOServices.GetNotReadyDrive();
            if (drive == null)
            {
                Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
                return;
            }

            bool result = Exists(Path.Combine(drive, "Subdirectory"));

            Assert.False(result);
        }

        // Not all drives may be accessible (locked, no rights, etc.), and as such would return false.
        // eg. Create a new volume, bitlocker it, and lock it. This new volume is no longer accessible
        // and any attempt to access this drive will return false.
        // We just care that we can access an accessible drive directly, we don't care which one.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't read root in appcontainer
        [PlatformSpecific(TestPlatforms.Windows)] // drive labels
        public void DriveAsPath()
        {
            Assert.False(Exists(IOServices.GetNonExistentDrive()));
            Assert.Contains(IOServices.GetReadyDrives(), drive => Exists(drive));
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)] // drive labels
        public void ExtendedDriveAsPath()
        {
            Assert.False(Exists(IOInputs.ExtendedPrefix + IOServices.GetNonExistentDrive()));

            if (PlatformDetection.IsNotInAppContainer)
                Assert.Contains(IOServices.GetReadyDrives(), drive => Exists(IOInputs.ExtendedPrefix + drive));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // drive labels
        public void SubdirectoryOnNonExistentDriveAsPath_ReturnsFalse()
        {
            Assert.False(Exists(Path.Combine(IOServices.GetNonExistentDrive(), "nonexistentsubdir")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]  // Makes call to native code (libc)
        public void FalseForNonRegularFile()
        {
            string fileName = GetTestFilePath();
            Assert.Equal(0, mkfifo(fileName, 0));
            Assert.False(Directory.Exists(fileName));
        }

        #endregion
    }
}
