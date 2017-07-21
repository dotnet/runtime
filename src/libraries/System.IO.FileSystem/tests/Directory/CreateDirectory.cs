// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
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

        [Theory, MemberData(nameof(PathsWithInvalidCharacters))]
        public void PathWithInvalidCharactersAsPath_ThrowsArgumentException(string invalidPath)
        {
            if (invalidPath.Equals(@"\\?\") && !PathFeatures.IsUsingLegacyPathNormalization())
                AssertExtensions.ThrowsAny<IOException, UnauthorizedAccessException>(() => Create(invalidPath));
            else if (invalidPath.Contains(@"\\?\") && !PathFeatures.IsUsingLegacyPathNormalization())
                Assert.Throws<DirectoryNotFoundException>(() => Create(invalidPath));
            else
                Assert.Throws<ArgumentException>(() => Create(invalidPath));
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
            FileAttributes original = testDir.Attributes;

            try
            {
                testDir.Attributes = attributes;
                Assert.Equal(testDir.FullName, Create(testDir.FullName).FullName);
            }
            finally
            {
                testDir.Attributes = original;
            }
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
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
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

        [Theory,
            MemberData(nameof(PathsWithComponentLongerThanMaxComponent))]
        public void DirectoryWithComponentLongerThanMaxComponentAsPath_ThrowsException(string path)
        {
            // While paths themselves can be up to 260 characters including trailing null, file systems
            // limit each components of the path to a total of 255 characters on Desktop.
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Throws<PathTooLongException>(() => Create(path));
            }
            else
            {
                AssertExtensions.ThrowsAny<IOException, DirectoryNotFoundException, PathTooLongException>(() => Create(path));
            }
        }

        #endregion

        #region PlatformSpecific

        [Theory, MemberData(nameof(PathsWithInvalidColons))]
        [PlatformSpecific(TestPlatforms.Windows)]  // invalid colons throws ArgumentException
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Versions of netfx older than 4.6.2 throw an ArgumentException instead of NotSupportedException. Until all of our machines run netfx against the actual latest version, these will fail.")]
        public void PathWithInvalidColons_ThrowsNotSupportedException(string invalidPath)
        {
            Assert.Throws<NotSupportedException>(() => Create(invalidPath));
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
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
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void DirectoryLongerThanMaxLongPathWithExtendedSyntax_ThrowsException()
        {
            var paths = IOInputs.GetPathsLongerThanMaxLongPath(GetTestFilePath(), useExtendedSyntax: true);

            // Long directory path with extended syntax throws PathTooLongException on Desktop.
            // Everywhere else, it may be either PathTooLongException or DirectoryNotFoundException
            if (PlatformDetection.IsFullFramework)
            {
                Assert.All(paths, path => { Assert.Throws<PathTooLongException>(() => Create(path)); });
            }
            else
            {
                Assert.All(paths,
                    path =>
                    {
                        AssertExtensions
                            .ThrowsAny<PathTooLongException, DirectoryNotFoundException>(
                                () => Create(path));
                    });
            }
        }

        [ConditionalFact(nameof(LongPathsAreNotBlocked), nameof(UsingNewNormalization))]
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
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
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
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
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]  // whitespace as path throws ArgumentException on Windows
        public void WindowsWhiteSpaceAsPath_ThrowsArgumentException(string path)
        {
            Assert.Throws<ArgumentException>(() => Create(path));
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
            MemberData(nameof(ControlWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]  // trailing whitespace in path is removed on Windows
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)] // e.g. NetFX only
        public void TrailingWhiteSpace_Trimmed(string component)
        {
            // On desktop, we trim a number of whitespace characters 
            DirectoryInfo testDir = Create(GetTestFilePath());
            string path = IOServices.RemoveTrailingSlash(testDir.FullName) + component;
            DirectoryInfo result = Create(path);

            Assert.True(Directory.Exists(result.FullName));
            Assert.Equal(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
        }

        [Theory,
            MemberData(nameof(NonControlWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]  // trailing whitespace in path is removed on Windows
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Not NetFX
        public void TrailingWhiteSpace_NotTrimmed(string component)
        {
            // In CoreFX we don't trim anything other than space (' ')
            DirectoryInfo testDir = Create(GetTestFilePath() + component);
            string path = IOServices.RemoveTrailingSlash(testDir.FullName);
            DirectoryInfo result = Create(path);

            Assert.True(Directory.Exists(result.FullName));
            Assert.Equal(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
        }

        [Theory,
            MemberData(nameof(SimpleWhiteSpace))] //*Just Spaces*
        [PlatformSpecific(TestPlatforms.Windows)]  // trailing whitespace in path is removed on Windows
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Not NetFX
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
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
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
            MemberData(nameof(PathsWithAlternativeDataStreams))]
        [PlatformSpecific(TestPlatforms.Windows)] // alternate data streams
        public void PathWithAlternateDataStreams_ThrowsNotSupportedException(string path)
        {
            Assert.Throws<NotSupportedException>(() => Create(path));
        }

        [Theory,
            MemberData(nameof(PathsWithReservedDeviceNames))]
        [PlatformSpecific(TestPlatforms.Windows)] // device name prefixes
        public void PathWithReservedDeviceNameAsPath_ThrowsDirectoryNotFoundException(string path)
        {
            // Throws DirectoryNotFoundException, when the behavior really should be an invalid path
            Assert.Throws<DirectoryNotFoundException>(() => Create(path));
        }

        [ConditionalTheory(nameof(UsingNewNormalization)),
            MemberData(nameof(ReservedDeviceNames))]
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
        [PlatformSpecific(TestPlatforms.Windows)] // device name prefixes
        public void PathWithReservedDeviceNameAsExtendedPath(string path)
        {
            Assert.True(Create(IOInputs.ExtendedPrefix + Path.Combine(TestDirectory, path)).Exists, path);
        }

        [Theory,
            MemberData(nameof(UncPathsWithoutShareName))]
        [PlatformSpecific(TestPlatforms.Windows)] // UNC shares
        public void UncPathWithoutShareNameAsPath_ThrowsArgumentException(string path)
        {
            Assert.Throws<ArgumentException>(() => Create(path));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // UNC shares
        public void UNCPathWithOnlySlashes()
        {
            Assert.Throws<ArgumentException>(() => Create("//"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // drive labels
        [ActiveIssue(20117, TargetFrameworkMonikers.Uap)]
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
        [ActiveIssue(1221)]
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

