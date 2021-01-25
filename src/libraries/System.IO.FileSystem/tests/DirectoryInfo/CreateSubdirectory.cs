// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void SubdirectoryOverlappingName_ThrowsArgumentException()
        {
            // What we're looking for here is trying to create C:\FooBar under C:\Foo by passing "..\FooBar"
            DirectoryInfo info = Directory.CreateDirectory(GetTestFilePath());

            string overlappingName = ".." + Path.DirectorySeparatorChar + info.Name + "overlap";

            Assert.Throws<ArgumentException>(() => info.CreateSubdirectory(overlappingName));

            // Now try with an info with a trailing separator
            info = new DirectoryInfo(info.FullName + Path.DirectorySeparatorChar);
            Assert.Throws<ArgumentException>(() => info.CreateSubdirectory(overlappingName));
        }

        [Theory,
            MemberData(nameof(ValidPathComponentNames))]
        public void ValidPathWithTrailingSlash(string component)
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string path = component + Path.DirectorySeparatorChar;
            DirectoryInfo result = new DirectoryInfo(testDir.FullName).CreateSubdirectory(path);

            Assert.Equal(Path.Combine(testDir.FullName, path), result.FullName);
            Assert.True(Directory.Exists(result.FullName));

            // Now try creating subdirectories when the directory info itself has a slash
            testDir = Directory.CreateDirectory(GetTestFilePath() + Path.DirectorySeparatorChar);

            result = new DirectoryInfo(testDir.FullName).CreateSubdirectory(path);

            Assert.Equal(Path.Combine(testDir.FullName, path), result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }

        [Theory,
            MemberData(nameof(ValidPathComponentNames))]
        public void ValidPathWithoutTrailingSlash(string component)
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string path = component;
            DirectoryInfo result = new DirectoryInfo(testDir.FullName).CreateSubdirectory(path);

            Assert.Equal(Path.Combine(testDir.FullName, path), result.FullName);
            Assert.True(Directory.Exists(result.FullName));

            // Now try creating subdirectories when the directory info itself has a slash
            testDir = Directory.CreateDirectory(GetTestFilePath() + Path.DirectorySeparatorChar);

            result = new DirectoryInfo(testDir.FullName).CreateSubdirectory(path);

            Assert.Equal(Path.Combine(testDir.FullName, path), result.FullName);
            Assert.True(Directory.Exists(result.FullName));
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

        [Theory,
            MemberData(nameof(ControlWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsControlWhiteSpace_Core(string component)
        {
            Assert.Throws<IOException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(component));
        }

        [Theory,
            MemberData(nameof(SimpleWhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsSimpleWhiteSpaceThrowsException(string component)
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(component));
        }

        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Whitespace as path allowed
        public void UnixWhiteSpaceAsPath_Allowed(string path)
        {
            new DirectoryInfo(TestDirectory).CreateSubdirectory(path);
            Assert.True(Directory.Exists(Path.Combine(TestDirectory, path)));
        }

        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Trailing whitespace in path treated as significant
        public void UnixNonSignificantTrailingWhiteSpace(string component)
        {
            // Unix treats trailing/prename whitespace as significant and a part of the name.
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string path = IOServices.RemoveTrailingSlash(testDir.Name) + component;
            DirectoryInfo result = new DirectoryInfo(TestDirectory).CreateSubdirectory(path);

            Assert.True(Directory.Exists(result.FullName));
            Assert.NotEqual(testDir.FullName, IOServices.RemoveTrailingSlash(result.FullName));
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Extended windows path
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

        [Fact]
        public void ParentDirectoryNameAsPrefixShouldThrow()
        {
            string randomName = GetTestFileName();
            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(TestDirectory, randomName));

            Assert.Throws<ArgumentException>(() => di.CreateSubdirectory(Path.Combine("..", randomName + "abc", GetTestFileName())));
        }

        #endregion
    }
}
