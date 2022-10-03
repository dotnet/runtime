// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_CreateDirectory : FileSystemTest
    {
        public static TheoryData ReservedDeviceNames = IOInputs.GetReservedDeviceNames().ToTheoryData();
        #region Utilities

        public virtual DirectoryInfo Create(string path)
        {
            return Directory.CreateDirectory(path);
        }

        public virtual bool IsDirectoryCreate => true;

        #endregion

        #region UniversalTests

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FileNameIsToString_NotFullPath()
        {
            // We're checking that we're maintaining the original path
            RemoteExecutor.Invoke(() =>
            {
                Environment.CurrentDirectory = TestDirectory;
                string subdir = Path.GetRandomFileName();
                DirectoryInfo info = Create(subdir);
                Assert.Equal(subdir, info.ToString());
                Environment.CurrentDirectory = Path.GetTempPath();
            }).Dispose();
        }

        [Fact]
        public void FileNameIsToString_FullPath()
        {
            string subdir = Path.GetRandomFileName();
            string fullPath = Path.Combine(TestDirectory, subdir);
            DirectoryInfo info = Create(fullPath);
            Assert.Equal(fullPath, info.ToString());
        }

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

        [Theory, MemberData(nameof(PathsWithInvalidCharacters))]
        public void PathWithInvalidCharactersAsPath_Core(string invalidPath)
        {
            if (invalidPath.Contains('\0'))
                Assert.Throws<ArgumentException>("path", () => Create(invalidPath));
            else
                Assert.Throws<IOException>(() => Create(invalidPath));
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

            try
            {
                testDir.Attributes = attributes;
                Assert.Equal(testDir.FullName, Create(testDir.FullName).FullName);
            }
            finally
            {
                testDir.Attributes = FileAttributes.Normal;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInAppContainer))] // Can't read root in appcontainer
        public void RootPath_AppContainer()
        {
            string dirName = Path.GetPathRoot(Directory.GetCurrentDirectory());
            Assert.Throws<DirectoryNotFoundException>(() => Create(dirName));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't read root in appcontainer
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

        [Theory, MemberData(nameof(ValidPathComponentNames))]
        public void ValidPathWithTrailingSlash(string component)
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string path = IOServices.AddTrailingSlashIfNeeded(Path.Combine(testDir.FullName, component));
            DirectoryInfo result = Create(path);

            Assert.Equal(path, result.FullName);
            Assert.True(result.Exists);
        }

        [ConditionalTheory(nameof(UsingNewNormalization)),
            MemberData(nameof(ValidPathComponentNames))]
        [PlatformSpecific(TestPlatforms.Windows)]  // trailing slash
        public void ValidExtendedPathWithTrailingSlash(string component)
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string path = IOInputs.ExtendedPrefix + IOServices.AddTrailingSlashIfNeeded(Path.Combine(testDir.FullName, component));
            DirectoryInfo result = Create(path);

            Assert.Equal(path, result.FullName);
            Assert.True(result.Exists);

        }

        [Theory,
            MemberData(nameof(ValidPathComponentNames))]
        public void ValidPathWithoutTrailingSlash(string component)
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string path = testDir.FullName + Path.DirectorySeparatorChar + component;
            DirectoryInfo result = Create(path);

            Assert.Equal(path, result.FullName);
            Assert.True(Directory.Exists(result.FullName));

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
        public void DirectoryEqualToMaxDirectory_CanBeCreatedAllAtOnce()
        {
            DirectoryInfo testDir = Create(GetTestFilePath());
            string path = IOServices.GetPath(testDir.FullName, IOInputs.MaxDirectory);
            DirectoryInfo result = Create(path);

            Assert.Equal(path, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }
        #endregion

        #region PlatformSpecific

        [Theory,
            MemberData(nameof(PathsWithComponentLongerThanMaxComponent))]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not have a limit on the maximum component length")]
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_ThrowsException(string path)
        {
            AssertExtensions.ThrowsAny<IOException, DirectoryNotFoundException, PathTooLongException>(() => Create(path));
        }

        [Theory,
            MemberData(nameof(PathsWithComponentLongerThanMaxComponent))]
        [PlatformSpecific(TestPlatforms.Browser)] // Browser specific test in case the check changes in the future
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_BrowserDoesNotThrowException(string path)
        {
            DirectoryInfo result = Create(path);
            Assert.True(Directory.Exists(path));
        }

        [Theory, MemberData(nameof(PathsWithInvalidColons))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PathsWithInvalidColons_ThrowIOException_Core(string invalidPath)
        {
            // You can't actually create a directory with a colon in it. It was a preemptive
            // check, now we let the OS give us failures on usage.
            Assert.ThrowsAny<IOException>(() => Create(invalidPath));
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [PlatformSpecific(TestPlatforms.Windows)]  // long directory path succeeds
        public void DirectoryLongerThanMaxPath_Succeeds()
        {
            var paths = IOInputs.GetPathsLongerThanMaxPath(GetTestFilePath());
            Assert.All(paths, (path) =>
            {
                DirectoryInfo result = Create(path);
                Assert.True(Directory.Exists(result.FullName));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // long directory path throws PathTooLongException
        public void DirectoryLongerThanMaxLongPath_ThrowsPathTooLongException()
        {
            var paths = IOInputs.GetPathsLongerThanMaxLongPath(GetTestFilePath());
            Assert.All(paths, (path) =>
            {
                Assert.Throws<PathTooLongException>(() => Create(path));
            });
        }

        [ConditionalFact(nameof(LongPathsAreNotBlocked), nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void DirectoryLongerThanMaxLongPathWithExtendedSyntax_ThrowsException()
        {
            var paths = IOInputs.GetPathsLongerThanMaxLongPath(GetTestFilePath(), useExtendedSyntax: true);

            // Ideally this should be PathTooLongException or DirectoryNotFoundException but on some machines
            // windows gives us ERROR_INVALID_NAME, producing IOException.
            Assert.All(paths, path =>
                AssertExtensions.ThrowsAny<PathTooLongException, DirectoryNotFoundException, IOException>(() => Create(path)));
        }

        [ConditionalFact(nameof(LongPathsAreNotBlocked), nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]  // long directory path with extended syntax succeeds
        public void ExtendedDirectoryLongerThanLegacyMaxPath_Succeeds()
        {
            var paths = IOInputs.GetPathsLongerThanMaxPath(GetTestFilePath(), useExtendedSyntax: true);
            Assert.All(paths, (path) =>
            {
                Assert.True(Create(path).Exists);
            });
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [PlatformSpecific(TestPlatforms.Windows)]  // long directory path succeeds
        public void DirectoryLongerThanMaxDirectoryAsPath_Succeeds()
        {
            var paths = IOInputs.GetPathsLongerThanMaxDirectory(GetTestFilePath());
            Assert.All(paths, (path) =>
            {
                var result = Create(path);
                Assert.True(Directory.Exists(result.FullName));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // long directory path allowed
        public void UnixPathLongerThan256_Allowed()
        {
            DirectoryInfo testDir = Create(GetTestFilePath());
            string path = IOServices.GetPath(testDir.FullName, 257);
            DirectoryInfo result = Create(path);
            Assert.Equal(path, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // deeply nested directories allowed
        public void UnixPathWithDeeplyNestedDirectories()
        {
            DirectoryInfo parent = Create(GetTestFilePath());
            for (int i = 1; i <= 100; i++) // 100 == arbitrarily large number of directories
            {
                parent = Create(Path.Combine(parent.FullName, "dir" + i));
                Assert.True(Directory.Exists(parent.FullName));
            }
        }

        [Theory,
            MemberData(nameof(SimpleWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsSimpleWhiteSpaceAsPath_ThrowsArgumentException(string path)
        {
            Assert.Throws<ArgumentException>(() => Create(path));
        }

        [Theory,
            MemberData(nameof(ControlWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsWhiteSpaceAsPath_ThrowsIOException_Core(string path)
        {
            Assert.Throws<IOException>(() => Create(path));
        }


        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // whitespace as path allowed
        public void UnixWhiteSpaceAsPath_Allowed(string path)
        {
            Create(Path.Combine(TestDirectory, path));
            Assert.True(Directory.Exists(Path.Combine(TestDirectory, path)));

        }

        [Theory,
            MemberData(nameof(NonControlWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]  // trailing whitespace in path is removed on Windows
        public void TrailingWhiteSpace_NotTrimmed(string component)
        {
            // In .NET Core we don't trim anything other than space (' ')
            DirectoryInfo testDir = Create(GetTestFilePath() + component);
            string path = IOServices.RemoveTrailingSlash(testDir.FullName);
            DirectoryInfo result = Create(path);

            Assert.True(Directory.Exists(result.FullName));
            Assert.Equal(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
        }

        [Theory,
            MemberData(nameof(SimpleWhiteSpace))] //*Just Spaces*
        [PlatformSpecific(TestPlatforms.Windows)]  // trailing whitespace in path is removed on Windows
        public void TrailingSpace_NotTrimmed(string component)
        {
            DirectoryInfo testDir = Create(GetTestFilePath());
            string path = IOServices.RemoveTrailingSlash(testDir.FullName) + component;
            DirectoryInfo result = Create(path);

            Assert.True(Directory.Exists(result.FullName));
            Assert.Equal(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
        }

        [ConditionalTheory(nameof(UsingNewNormalization)),
            MemberData(nameof(SimpleWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]  // extended syntax with whitespace
        public void WindowsExtendedSyntaxWhiteSpace(string path)
        {
            string extendedPath = Path.Combine(IOInputs.ExtendedPrefix + TestDirectory, path);
            Directory.CreateDirectory(extendedPath);
            Assert.True(Directory.Exists(extendedPath), extendedPath);
        }

        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // trailing whitespace in path treated as significant on Unix
        public void UnixNonSignificantTrailingWhiteSpace(string component)
        {
            // Unix treats trailing/prename whitespace as significant and a part of the name.
            DirectoryInfo testDir = Create(GetTestFilePath());

            string path = IOServices.RemoveTrailingSlash(testDir.FullName) + component;
            DirectoryInfo result = Create(path);

            Assert.True(Directory.Exists(result.FullName));
            Assert.NotEqual(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));

        }

        [Theory,
            MemberData(nameof(PathsWithColons))]
        [PlatformSpecific(TestPlatforms.Windows)] // alternate data streams
        public void PathWithColons_ThrowsIOException_Core(string path)
        {
            if (PlatformDetection.IsInAppContainer)
            {
                AssertExtensions.ThrowsAny<DirectoryNotFoundException, IOException, UnauthorizedAccessException>(() => Create(Path.Combine(TestDirectory, path)));
            }
            else
            {
                Assert.ThrowsAny<IOException>(() => Create(Path.Combine(TestDirectory, path)));
            }
        }

        [ConditionalTheory(nameof(ReservedDeviceNamesAreBlocked))] // device name prefixes
        [MemberData(nameof(PathsWithReservedDeviceNames))]
        public void PathWithReservedDeviceNameAsPath_ThrowsDirectoryNotFoundException(string path)
        {
            // Throws DirectoryNotFoundException, when the behavior really should be an invalid path
            Assert.Throws<DirectoryNotFoundException>(() => Create(path));
        }

        [ConditionalTheory(nameof(ReservedDeviceNamesAreBlocked), nameof(UsingNewNormalization))] // device name prefixes
        [MemberData(nameof(ReservedDeviceNames))]
        public void PathWithReservedDeviceNameAsExtendedPath(string path)
        {
            Assert.True(Create(IOInputs.ExtendedPrefix + Path.Combine(TestDirectory, path)).Exists, path);
        }

        [Theory,
            MemberData(nameof(UncPathsWithoutShareName))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UncPathWithoutShareNameAsPath_ThrowsIOException_Core(string path)
        {
            Assert.ThrowsAny<IOException>(() => Create(path));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UNCPathWithOnlySlashes_Core()
        {
            Assert.ThrowsAny<IOException>(() => Create("//"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // drive labels
        public void CDriveCase()
        {
            DirectoryInfo dir = Create("c:\\");
            DirectoryInfo dir2 = Create("C:\\");
            Assert.NotEqual(dir.FullName, dir2.FullName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // drive letters
        public void DriveLetter_Windows()
        {
            // On Windows, DirectoryInfo will replace "<DriveLetter>:" with "."
            var driveLetter = Create(Directory.GetCurrentDirectory()[0] + ":");
            var current = Create(".");
            Assert.Equal(current.Name, driveLetter.Name);
            Assert.Equal(current.FullName, driveLetter.FullName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // drive letters casing
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67853", TestPlatforms.tvOS)]
        public void DriveLetter_Unix()
        {
            // On Unix, there's no special casing for drive letters.  These may or may not be valid names, depending
            // on the file system underlying the current directory.  Unix file systems typically allow these, but,
            // for example, these names are not allowed if running on a file system mounted from a Windows machine.
            DirectoryInfo driveLetter;
            try
            {
                driveLetter = Create("C:");
            }
            catch (IOException)
            {
                return;
            }
            var current = Create(".");
            Assert.Equal("C:", driveLetter.Name);
            Assert.Equal(Path.Combine(current.FullName, "C:"), driveLetter.FullName);
            try
            {
                // If this test is inherited then it's possible this call will fail due to the "C:" directory
                // being deleted in that other test before this call. What we care about testing (proper path
                // handling) is unaffected by this race condition.
                Directory.Delete("C:");
            }
            catch (DirectoryNotFoundException) { }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // testing drive labels
        public void NonExistentDriveAsPath_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                Create(IOServices.GetNonExistentDrive());
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // testing drive labels
        public void SubdirectoryOnNonExistentDriveAsPath_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                Create(Path.Combine(IOServices.GetNonExistentDrive(), "Subdirectory"));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/14378")]
        [PlatformSpecific(TestPlatforms.Windows)] // testing drive labels
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
        [PlatformSpecific(TestPlatforms.Windows)] // testing drive labels
        [ActiveIssue("https://github.com/dotnet/runtime/issues/14378")]
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
