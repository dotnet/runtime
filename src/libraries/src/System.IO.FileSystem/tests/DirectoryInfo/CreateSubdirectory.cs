// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_CreateSubDirectory : FileSystemTest
    {
        #region UniversalTests

        [Fact]
        public void NullAsPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(null));
        }

        [Fact]
        public void EmptyAsPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(string.Empty));
        }

        [Fact]
        public void PathAlreadyExistsAsFile()
        {
            string path = GetTestFileName();
            File.Create(Path.Combine(TestDirectory, path)).Dispose();

            Assert.Throws<IOException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(path));
            Assert.Throws<IOException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(IOServices.AddTrailingSlashIfNeeded(path)));
            Assert.Throws<IOException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(IOServices.RemoveTrailingSlash(path)));
        }

        [Theory]
        [InlineData(FileAttributes.Hidden)]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Normal)]
        public void PathAlreadyExistsAsDirectory(FileAttributes attributes)
        {
            string path = GetTestFileName();
            DirectoryInfo testDir = Directory.CreateDirectory(Path.Combine(TestDirectory, path));
            FileAttributes original = testDir.Attributes;

            try
            {
                testDir.Attributes = attributes;
                Assert.Equal(testDir.FullName, new DirectoryInfo(TestDirectory).CreateSubdirectory(path).FullName);
            }
            finally
            {
                testDir.Attributes = original;
            }
        }

        [Fact]
        public void DotIsCurrentDirectory()
        {
            string path = GetTestFileName();
            DirectoryInfo result = new DirectoryInfo(TestDirectory).CreateSubdirectory(Path.Combine(path, "."));
            Assert.Equal(IOServices.RemoveTrailingSlash(Path.Combine(TestDirectory, path)), result.FullName);

            result = new DirectoryInfo(TestDirectory).CreateSubdirectory(Path.Combine(path, ".") + Path.DirectorySeparatorChar);
            Assert.Equal(IOServices.AddTrailingSlashIfNeeded(Path.Combine(TestDirectory, path)), result.FullName);
        }

        [Fact]
        public void Conflicting_Parent_Directory()
        {
            string path = Path.Combine(TestDirectory, GetTestFileName(), "c");
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(path));
        }

        [Fact]
        public void DotDotIsParentDirectory()
        {
            DirectoryInfo result = new DirectoryInfo(TestDirectory).CreateSubdirectory(Path.Combine(GetTestFileName(), ".."));
            Assert.Equal(IOServices.RemoveTrailingSlash(TestDirectory), result.FullName);

            result = new DirectoryInfo(TestDirectory).CreateSubdirectory(Path.Combine(GetTestFileName(), "..") + Path.DirectorySeparatorChar);
            Assert.Equal(IOServices.AddTrailingSlashIfNeeded(TestDirectory), result.FullName);
        }

        [Fact]
        public void SubDirectoryIsParentDirectory_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(Path.Combine(TestDirectory, "..")));
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory + "/path").CreateSubdirectory("../../path2"));
        }

        [Fact]
        public void ValidPathWithTrailingSlash()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            var components = IOInputs.GetValidPathComponentNames();
            Assert.All(components, (component) =>
            {
                string path = IOServices.AddTrailingSlashIfNeeded(component);
                DirectoryInfo result = new DirectoryInfo(testDir.FullName).CreateSubdirectory(path);

                Assert.Equal(Path.Combine(testDir.FullName, path), result.FullName);
                Assert.True(Directory.Exists(result.FullName));
            });
        }

        [Fact]
        public void ValidPathWithoutTrailingSlash()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            var components = IOInputs.GetValidPathComponentNames();
            Assert.All(components, (component) =>
            {
                string path = component;
                DirectoryInfo result = new DirectoryInfo(testDir.FullName).CreateSubdirectory(path);

                Assert.Equal(Path.Combine(testDir.FullName, path), result.FullName);
                Assert.True(Directory.Exists(result.FullName));
            });
        }

        [Fact]
        public void ValidPathWithMultipleSubdirectories()
        {
            string dirName = Path.Combine(GetTestFileName(), "Test", "Test", "Test");
            DirectoryInfo dir = new DirectoryInfo(TestDirectory).CreateSubdirectory(dirName);

            Assert.Equal(dir.FullName, Path.Combine(TestDirectory, dirName));
        }

        [Fact]
        public void AllowedSymbols()
        {
            string dirName = Path.GetRandomFileName() + "!@#$%^&";
            DirectoryInfo dir = new DirectoryInfo(TestDirectory).CreateSubdirectory(dirName);

            Assert.Equal(dir.FullName, Path.Combine(TestDirectory, dirName));
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsControWhiteSpace()
        {
            // CreateSubdirectory will throw when passed a path with control whitespace e.g. "\t"
            var components = IOInputs.GetControlWhiteSpace();
            Assert.All(components, (component) =>
            {
                string path = IOServices.RemoveTrailingSlash(GetTestFileName());
                Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(component));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsSimpleWhiteSpace()
        {
            // CreateSubdirectory trims all simple whitespace, returning us the parent directory
            // that called CreateSubdirectory
            var components = IOInputs.GetSimpleWhiteSpace();

            Assert.All(components, (component) =>
            {
                string path = IOServices.RemoveTrailingSlash(GetTestFileName());
                DirectoryInfo result = new DirectoryInfo(TestDirectory).CreateSubdirectory(component);

                Assert.True(Directory.Exists(result.FullName));
                Assert.Equal(TestDirectory, IOServices.RemoveTrailingSlash(result.FullName));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UnixWhiteSpaceAsPath_Allowed()
        {
            var paths = IOInputs.GetWhiteSpace();
            Assert.All(paths, (path) =>
            {
                new DirectoryInfo(TestDirectory).CreateSubdirectory(path);
                Assert.True(Directory.Exists(Path.Combine(TestDirectory, path)));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UnixNonSignificantTrailingWhiteSpace()
        {
            // Unix treats trailing/prename whitespace as significant and a part of the name.
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            var components = IOInputs.GetWhiteSpace();

            Assert.All(components, (component) =>
            {
                string path = IOServices.RemoveTrailingSlash(testDir.Name) + component;
                DirectoryInfo result = new DirectoryInfo(TestDirectory).CreateSubdirectory(path);

                Assert.True(Directory.Exists(result.FullName));
                Assert.NotEqual(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
            });
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [SkipOnTargetFramework(Tfm.BelowNet462 | Tfm.Core50, "long path support added in 4.6.2")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ExtendedPathSubdirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(IOInputs.ExtendedPrefix + GetTestFilePath());
            Assert.True(testDir.Exists);
            DirectoryInfo subDir = testDir.CreateSubdirectory("Foo");
            Assert.True(subDir.Exists);
            Assert.StartsWith(IOInputs.ExtendedPrefix, subDir.FullName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // UNC shares
        public void UNCPathWithOnlySlashes()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<ArgumentException>(() => testDir.CreateSubdirectory("//"));
        }

        #endregion
    }
}
