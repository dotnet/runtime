// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
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
                DirectoryInfo testDir = Directory.CreateDirectory(path);
                Assert.True(Exists(path));
            });
        }

        [Fact]
        public void PathWithInvalidCharactersAsPath_ReturnsFalse()
        {
            // Checks that errors aren't thrown when calling Exists() on paths with impossible to create characters
            char[] trimmed = { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0 };
            Assert.All((IOInputs.GetPathsWithInvalidCharacters()), (component) =>
            {
                Assert.False(Exists(component));
                if (!trimmed.Contains(component.ToCharArray()[0]))
                    Assert.False(Exists(TestDirectory + Path.DirectorySeparatorChar + component));
            });
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
            DirectoryInfo testDir = Directory.CreateDirectory(path);

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

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void ValidExtendedPathExists_ReturnsTrue()
        {
            Assert.All((IOInputs.GetValidPathComponentNames()), (component) =>
            {
                string path = IOInputs.ExtendedPrefix + Path.Combine(TestDirectory, "extended", component);
                DirectoryInfo testDir = Directory.CreateDirectory(path);
                Assert.True(Exists(path));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void ExtendedPathAlreadyExistsAsFile()
        {
            string path = IOInputs.ExtendedPrefix + GetTestFilePath();
            File.Create(path).Dispose();

            Assert.False(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.False(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void ExtendedPathAlreadyExistsAsDirectory()
        {
            string path = IOInputs.ExtendedPrefix + GetTestFilePath();
            DirectoryInfo testDir = Directory.CreateDirectory(path);

            Assert.True(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void DirectoryLongerThanMaxDirectoryAsPath_DoesntThrow()
        {
            Assert.All((IOInputs.GetPathsLongerThanMaxDirectory(GetTestFilePath())), (path) =>
            {
                Assert.False(Exists(path));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // Unix equivalent tested already in CreateDirectory
        public void WindowsWhiteSpaceAsPath_ReturnsFalse()
        {
            // Checks that errors aren't thrown when calling Exists() on impossible paths
            Assert.All(IOInputs.GetWhiteSpace(), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows | PlatformID.OSX)]
        public void DoesCaseInsensitiveInvariantComparisons()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.True(Exists(testDir.FullName));
            Assert.True(Exists(testDir.FullName.ToUpperInvariant()));
            Assert.True(Exists(testDir.FullName.ToLowerInvariant()));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Linux | PlatformID.FreeBSD)]
        public void DoesCaseSensitiveComparisons()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.True(Exists(testDir.FullName));
            Assert.False(Exists(testDir.FullName.ToUpperInvariant()));
            Assert.False(Exists(testDir.FullName.ToLowerInvariant()));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // In Windows, trailing whitespace in a path is trimmed appropriately
        public void TrailingWhitespaceExistence()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.All(IOInputs.GetWhiteSpace(), (component) =>
            {
                string path = testDir.FullName + component;
                Assert.True(Exists(path), path); // string concat in case Path.Combine() trims whitespace before Exists gets to it
                Assert.False(Exists(IOInputs.ExtendedPrefix + path), path);
            });

            Assert.All(IOInputs.GetSimpleWhiteSpace(), (component) =>
            {
                string path = GetTestFilePath(memberName: "Extended") + component;
                testDir = Directory.CreateDirectory(IOInputs.ExtendedPrefix + path);
                Assert.False(Exists(path), path);
                Assert.True(Exists(testDir.FullName));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // alternate data stream
        public void PathWithAlternateDataStreams_ReturnsFalse()
        {
            Assert.All(IOInputs.GetWhiteSpace(), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [OuterLoop]
        [PlatformSpecific(PlatformID.Windows)] // device names
        public void PathWithReservedDeviceNameAsPath_ReturnsFalse()
        {
            Assert.All((IOInputs.GetPathsWithReservedDeviceNames()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // UNC paths
        public void UncPathWithoutShareNameAsPath_ReturnsFalse()
        {
            Assert.All((IOInputs.GetUncPathsWithoutShareName()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // max directory length not fixed on Unix
        public void DirectoryEqualToMaxDirectory_ReturnsTrue()
        {
            // Creates directories up to the maximum directory length all at once
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            PathInfo path = IOServices.GetPath(testDir.FullName, IOInputs.MaxDirectory, maxComponent: 10);
            Directory.CreateDirectory(path.FullPath);
            Assert.True(Exists(path.FullPath));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // max directory length not fixed on Unix
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_ReturnsFalse()
        {
            Assert.All((IOInputs.GetPathsWithComponentLongerThanMaxComponent()), (component) =>
            {
                Assert.False(Exists(component));
            });
        }

        [Fact]
        [ActiveIssue(1221)]
        [PlatformSpecific(PlatformID.Windows)] // drive labels
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
        [ActiveIssue(1221)]
        [PlatformSpecific(PlatformID.Windows)] // drive labels
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

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // drive labels
        public void NonExistentDriveAsPath_ReturnsFalse()
        {
            Assert.False(Exists(IOServices.GetNonExistentDrive()));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // drive labels
        public void SubdirectoryOnNonExistentDriveAsPath_ReturnsFalse()
        {
            Assert.False(Exists(Path.Combine(IOServices.GetNonExistentDrive(), "nonexistentsubdir")));
        }


        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymlinkToNewDirectory()
        {
            string targetPath = GetTestFilePath();
            Directory.CreateDirectory(targetPath);

            string linkPath = GetTestFilePath();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, targetPath));

            Assert.NotEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), Directory.Exists(linkPath));
        }

        #endregion
    }
}
