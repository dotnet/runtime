// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_CreateDirectory : FileSystemTest
    {
        #region Utilities

        public virtual DirectoryInfo Create(string path)
        {
            return Directory.CreateDirectory(path);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullAsPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Create(null));
        }

        [Fact]
        public void EmptyAsPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Create(string.Empty));
        }

        [Fact]
        public void PathWithInvalidCharactersAsPath_ThrowsArgumentException()
        {
            var paths = IOInputs.GetPathsWithInvalidCharacters();
            Assert.All(paths, (path) =>
            {
                Assert.Throws<ArgumentException>(() => Create(path));
            });
        }

        [Fact]
        public void PathAlreadyExistsAsFile()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();

            Assert.Throws<IOException>(() => Create(path));
            Assert.Throws<IOException>(() => Create(IOServices.AddTrailingSlashIfNeeded(path)));
            Assert.Throws<IOException>(() => Create(IOServices.RemoveTrailingSlash(path)));
        }

        [Theory]
        [InlineData(FileAttributes.Hidden)]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Normal)]
        public void PathAlreadyExistsAsDirectory(FileAttributes attributes)
        {
            DirectoryInfo testDir = Create(GetTestFilePath());
            testDir.Attributes = attributes;
            Assert.Equal(testDir.FullName, Create(testDir.FullName).FullName);
        }

        [Fact]
        public void RootPath()
        {
            string dirName = Path.GetPathRoot(Directory.GetCurrentDirectory());
            DirectoryInfo dir = Create(dirName);
            Assert.Equal(dir.FullName, dirName);
        }

        [Fact]
        public void DotIsCurrentDirectory()
        {
            string path = GetTestFilePath();
            DirectoryInfo result = Create(Path.Combine(path, "."));
            Assert.Equal(IOServices.RemoveTrailingSlash(path), result.FullName);

            result = Create(Path.Combine(path, ".") + Path.DirectorySeparatorChar);
            Assert.Equal(IOServices.AddTrailingSlashIfNeeded(path), result.FullName);
        }

        [Fact]
        public void CreateCurrentDirectory()
        {
            DirectoryInfo result = Create(Directory.GetCurrentDirectory());
            Assert.Equal(Directory.GetCurrentDirectory(), result.FullName);
        }

        [Fact]
        public void DotDotIsParentDirectory()
        {
            DirectoryInfo result = Create(Path.Combine(GetTestFilePath(), ".."));
            Assert.Equal(IOServices.RemoveTrailingSlash(TestDirectory), result.FullName);

            result = Create(Path.Combine(GetTestFilePath(), "..") + Path.DirectorySeparatorChar);
            Assert.Equal(IOServices.AddTrailingSlashIfNeeded(TestDirectory), result.FullName);
        }

        [Fact]
        public void ValidPathWithTrailingSlash()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            var components = IOInputs.GetValidPathComponentNames();
            Assert.All(components, (component) =>
            {
                string path = IOServices.AddTrailingSlashIfNeeded(Path.Combine(testDir.FullName, component));
                DirectoryInfo result = Create(path);

                Assert.Equal(path, result.FullName);
                Assert.True(result.Exists);
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void ValidExtendedPathWithTrailingSlash()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            var components = IOInputs.GetValidPathComponentNames();
            Assert.All(components, (component) =>
            {
                string path = IOInputs.ExtendedPrefix + IOServices.AddTrailingSlashIfNeeded(Path.Combine(testDir.FullName, component));
                DirectoryInfo result = Create(path);

                Assert.Equal(path, result.FullName);
                Assert.True(result.Exists);
            });
        }

        [Fact]
        public void ValidPathWithoutTrailingSlash()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            var components = IOInputs.GetValidPathComponentNames();
            Assert.All(components, (component) =>
            {
                string path = testDir.FullName + Path.DirectorySeparatorChar + component;
                DirectoryInfo result = Create(path);

                Assert.Equal(path, result.FullName);
                Assert.True(Directory.Exists(result.FullName));
            });
        }

        [Fact]
        public void ValidPathWithMultipleSubdirectories()
        {
            string dirName = Path.Combine(GetTestFilePath(), "Test", "Test", "Test");
            DirectoryInfo dir = Create(dirName);

            Assert.Equal(dir.FullName, dirName);
        }

        [Fact]
        public void AllowedSymbols()
        {
            string dirName = Path.Combine(TestDirectory, Path.GetRandomFileName() + "!@#$%^&");
            DirectoryInfo dir = Create(dirName);

            Assert.Equal(dir.FullName, dirName);
        }

        [Fact]
        public void DirectoryEqualToMaxDirectory_CanBeCreated()
        {
            DirectoryInfo testDir = Create(GetTestFilePath());
            PathInfo path = IOServices.GetPath(testDir.FullName, IOInputs.MaxDirectory, IOInputs.MaxComponent);
            Assert.All(path.SubPaths, (subpath) =>
            {
                DirectoryInfo result = Create(subpath);

                Assert.Equal(subpath, result.FullName);
                Assert.True(Directory.Exists(result.FullName));
            });
        }

        [Fact]
        public void DirectoryEqualToMaxDirectory_CanBeCreatedAllAtOnce()
        {
            DirectoryInfo testDir = Create(GetTestFilePath());
            PathInfo path = IOServices.GetPath(testDir.FullName, IOInputs.MaxDirectory, maxComponent: 10);
            DirectoryInfo result = Create(path.FullPath);

            Assert.Equal(path.FullPath, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }

        [Fact]
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_ThrowsPathTooLongException()
        {
            // While paths themselves can be up to 260 characters including trailing null, file systems
            // limit each components of the path to a total of 255 characters.
            var paths = IOInputs.GetPathsWithComponentLongerThanMaxComponent();

            Assert.All(paths, (path) =>
            {
                Assert.Throws<PathTooLongException>(() => Create(path));
            });
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void DirectoryLongerThanMaxPathAsPath_ThrowsPathTooLongException()
        {
            var paths = IOInputs.GetPathsLongerThanMaxPath();
            Assert.All(paths, (path) =>
            {
                Assert.Throws<PathTooLongException>(() => Create(path));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void ExtendedDirectoryLongerThanLegacyMaxPathSucceeds()
        {
            var paths = IOInputs.GetPathsLongerThanMaxPath(useExtendedSyntax: true, includeExtendedMaxPath: false);
            Assert.All(paths, (path) =>
            {
                Assert.True(Create(path).Exists);
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void DirectoryLongerThanMaxDirectoryAsPath_ThrowsPathTooLongException()
        {
            var paths = IOInputs.GetPathsLongerThanMaxDirectory();
            Assert.All(paths, (path) =>
            {
                Assert.Throws<PathTooLongException>(() => Create(path));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixPathLongerThan256_Allowed()
        {
            DirectoryInfo testDir = Create(GetTestFilePath());
            PathInfo path = IOServices.GetPath(testDir.FullName, 257, IOInputs.MaxComponent);
            DirectoryInfo result = Create(path.FullPath);
            Assert.Equal(path.FullPath, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixPathWithDeeplyNestedDirectories()
        {
            DirectoryInfo parent = Create(GetTestFilePath());
            for (int i = 1; i <= 100; i++) // 100 == arbitrarily large number of directories
            {
                parent = Create(Path.Combine(parent.FullName, "dir" + i));
                Assert.True(Directory.Exists(parent.FullName));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsWhiteSpaceAsPath_ThrowsArgumentException()
        {
            var paths = IOInputs.GetWhiteSpace();
            Assert.All(paths, (path) =>
            {
                Assert.Throws<ArgumentException>(() => Create(path));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixWhiteSpaceAsPath_Allowed()
        {
            var paths = IOInputs.GetWhiteSpace();
            Assert.All(paths, (path) =>
            {
                Create(Path.Combine(TestDirectory, path));
                Assert.True(Directory.Exists(Path.Combine(TestDirectory, path)));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsTrailingWhiteSpace()
        {
            // Windows will remove all nonsignificant whitespace in a path
            DirectoryInfo testDir = Create(GetTestFilePath());
            var components = IOInputs.GetWhiteSpace();

            Assert.All(components, (component) =>
            {
                string path = IOServices.RemoveTrailingSlash(testDir.FullName) + component;
                DirectoryInfo result = Create(path);

                Assert.True(Directory.Exists(result.FullName));
                Assert.Equal(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsExtendedSyntaxWhiteSpace()
        {
            var paths = IOInputs.GetSimpleWhiteSpace();
            using (TemporaryDirectory directory = new TemporaryDirectory())
            {
                foreach (var path in paths)
                {
                    string extendedPath = Path.Combine(IOInputs.ExtendedPrefix + directory.Path, path);
                    Directory.CreateDirectory(extendedPath);
                    Assert.True(Directory.Exists(extendedPath), extendedPath);
                }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixNonSignificantTrailingWhiteSpace()
        {
            // Unix treats trailing/prename whitespace as significant and a part of the name.
            DirectoryInfo testDir = Create(GetTestFilePath());
            var components = IOInputs.GetWhiteSpace();

            Assert.All(components, (component) =>
            {
                string path = IOServices.RemoveTrailingSlash(testDir.FullName) + component;
                DirectoryInfo result = Create(path);

                Assert.True(Directory.Exists(result.FullName));
                Assert.NotEqual(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // alternate data streams
        public void PathWithAlternateDataStreams_ThrowsNotSupportedException()
        {
            var paths = IOInputs.GetPathsWithAlternativeDataStreams();
            Assert.All(paths, (path) =>
            {
                Assert.Throws<NotSupportedException>(() => Create(path));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // device name prefixes
        public void PathWithReservedDeviceNameAsPath_ThrowsDirectoryNotFoundException()
        {   // Throws DirectoryNotFoundException, when the behavior really should be an invalid path
            var paths = IOInputs.GetPathsWithReservedDeviceNames();
            Assert.All(paths, (path) =>
            {
                Assert.Throws<DirectoryNotFoundException>(() => Create(path));
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // device name prefixes
        public void PathWithReservedDeviceNameAsExtendedPath()
        {
            var paths = IOInputs.GetReservedDeviceNames();
            using (TemporaryDirectory directory = new TemporaryDirectory())
            {
                Assert.All(paths, (path) =>
                {
                    Assert.True(Create(IOInputs.ExtendedPrefix + Path.Combine(directory.Path, path)).Exists, path);
                });
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // UNC shares
        public void UncPathWithoutShareNameAsPath_ThrowsArgumentException()
        {
            var paths = IOInputs.GetUncPathsWithoutShareName();
            foreach (var path in paths)
            {
                Assert.Throws<ArgumentException>(() => Create(path));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // UNC shares
        public void UNCPathWithOnlySlashes()
        {
            Assert.Throws<ArgumentException>(() => Create("//"));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // drive labels
        public void CDriveCase()
        {
            DirectoryInfo dir = Create("c:\\");
            DirectoryInfo dir2 = Create("C:\\");
            Assert.NotEqual(dir.FullName, dir2.FullName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void DriveLetter_Windows()
        {
            // On Windows, DirectoryInfo will replace "<DriveLetter>:" with "."
            var driveLetter = Create(Directory.GetCurrentDirectory()[0] + ":");
            var current = Create(".");
            Assert.Equal(current.Name, driveLetter.Name);
            Assert.Equal(current.FullName, driveLetter.FullName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        [ActiveIssue(2459)]
        public void DriveLetter_Unix()
        {
            // On Unix, there's no special casing for drive letters, which are valid file names
            var driveLetter = Create("C:");
            var current = Create(".");
            Assert.Equal("C:", driveLetter.Name);
            Assert.Equal(Path.Combine(current.FullName, "C:"), driveLetter.FullName);
            Directory.Delete("C:");
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // testing drive labels
        public void NonExistentDriveAsPath_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                Create(IOServices.GetNonExistentDrive());
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // testing drive labels
        public void SubdirectoryOnNonExistentDriveAsPath_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                Create(Path.Combine(IOServices.GetNonExistentDrive(), "Subdirectory"));
            });
        }

        [Fact]
        [ActiveIssue(1221)]
        [PlatformSpecific(PlatformID.Windows)] // testing drive labels
        public void NotReadyDriveAsPath_ThrowsDirectoryNotFoundException()
        {   // Behavior is suspect, should really have thrown IOException similar to the SubDirectory case
            var drive = IOServices.GetNotReadyDrive();
            if (drive == null)
            {
                Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
                return;
            }

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                Create(drive);
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // testing drive labels
        [ActiveIssue(1221)]
        public void SubdirectoryOnNotReadyDriveAsPath_ThrowsIOException()
        {
            var drive = IOServices.GetNotReadyDrive();
            if (drive == null)
            {
                Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
                return;
            }

            // 'Device is not ready'
            Assert.Throws<IOException>(() =>
            {
                Create(Path.Combine(drive, "Subdirectory"));
            });
        }

#if !TEST_WINRT // Cannot set current directory to root from appcontainer with it's default ACL
        /*
        [Fact]
        [ActiveIssue(1220)] // SetCurrentDirectory
        public void DotDotAsPath_WhenCurrentDirectoryIsRoot_DoesNotThrow()
        {
            string root = Path.GetPathRoot(Directory.GetCurrentDirectory());

            using (CurrentDirectoryContext context = new CurrentDirectoryContext(root))
            {
                DirectoryInfo result = Create("..");

                Assert.True(Directory.Exists(result.FullName));
                Assert.Equal(root, result.FullName);
            }
        }
        */
#endif
        #endregion
    }
}

