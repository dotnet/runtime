// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class File_Create_str : FileSystemTest
    {
        #region Utilities

        public virtual FileStream Create(string path)
        {
            return File.Create(path);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullPath()
        {
            Assert.Throws<ArgumentNullException>(() => Create(null));
        }

        [Fact]
        public void EmptyPath()
        {
            Assert.Throws<ArgumentException>(() => Create(string.Empty));
        }

        [Fact]
        public void NonExistentPath()
        {
            Assert.Throws<DirectoryNotFoundException>(() => Create(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName())));
        }

        [Fact]
        public void CreateCurrentDirectory()
        {
            Assert.Throws<UnauthorizedAccessException>(() => Create("."));
        }

        [Fact]
        public void ValidCreation()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            using (FileStream stream = Create(testFile))
            {
                Assert.True(File.Exists(testFile));
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
            }
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(Tfm.BelowNet462 | Tfm.Core50, "dos device path support added in 4.6.2")]
        public void ValidCreation_ExtendedSyntax()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(IOInputs.ExtendedPrefix + GetTestFilePath());
            Assert.StartsWith(IOInputs.ExtendedPrefix, testDir.FullName);
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            using (FileStream stream = Create(testFile))
            {
                Assert.True(File.Exists(testFile));
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
            }
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(Tfm.BelowNet462 | Tfm.Core50, "long path support added in 4.6.2")]
        public void ValidCreation_LongPathExtendedSyntax()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(IOServices.GetPath(IOInputs.ExtendedPrefix + TestDirectory, characterCount: 500).FullPath);
            Assert.StartsWith(IOInputs.ExtendedPrefix, testDir.FullName);
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            using (FileStream stream = Create(testFile))
            {
                Assert.True(File.Exists(testFile));
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public void CreateInParentDirectory()
        {
            string testFile = GetTestFileName();
            using (FileStream stream = Create(Path.Combine(TestDirectory, "DoesntExists", "..", testFile)))
            {
                Assert.True(File.Exists(Path.Combine(TestDirectory, testFile)));
            }
        }

        [Fact]
        public void LegalSymbols()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName() + "!@#$%^&");
            using (FileStream stream = Create(testFile))
            {
                Assert.True(File.Exists(testFile));
            }
        }

        [Fact]
        public void InvalidDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName(), GetTestFileName());
            Assert.Throws<DirectoryNotFoundException>(() => Create(testFile));
        }

        [Fact]
        public void FileInUse()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            using (FileStream stream = Create(testFile))
            {
                Assert.True(File.Exists(testFile));
                Assert.Throws<IOException>(() => Create(testFile));
            }
        }

        [Fact]
        public void FileAlreadyExists()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            Create(testFile).Dispose();
            Assert.True(File.Exists(testFile));
            Create(testFile).Dispose();
            Assert.True(File.Exists(testFile));
        }

        [Fact]
        public void OverwriteReadOnly()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            Create(testFile).Dispose();
            Assert.True(File.Exists(testFile));
            Create(testFile).Dispose();
            Assert.True(File.Exists(testFile));
        }

        [Fact]
        public void LongPath()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<PathTooLongException>(() => Create(Path.Combine(testDir.FullName, new string('a', 300))));

            //TODO #645: File creation does not yet have long path support on Unix or Windows
            //using (Create(Path.Combine(testDir.FullName, new string('k', 257))))
            //{
            //    Assert.True(File.Exists(Path.Combine(testDir.FullName, new string('k', 257))));
            //}
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(CaseSensitivePlatforms)]
        public void CaseSensitive()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            using (File.Create(testFile + "AAAA"))
            using (File.Create(testFile + "aAAa"))
            {
                Assert.False(File.Exists(testFile + "AaAa"));
                Assert.True(File.Exists(testFile + "AAAA"));
                Assert.True(File.Exists(testFile + "aAAa"));
                Assert.Equal(2, Directory.GetFiles(testDir.FullName).Length);
            }
            Assert.Throws<DirectoryNotFoundException>(() => File.Create(testFile.ToLowerInvariant()));
        }

        [Fact]
        [PlatformSpecific(CaseInsensitivePlatforms)]
        public void CaseInsensitive()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDir.FullName, GetTestFileName());
            File.Create(testFile + "AAAA").Dispose();
            File.Create(testFile.ToLowerInvariant() + "aAAa").Dispose();
            Assert.Equal(1, Directory.GetFiles(testDir.FullName).Length);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsWildCharacterPath()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<ArgumentException>(() => Create(Path.Combine(testDir.FullName, "dls;d", "442349-0", "v443094(*)(+*$#$*", new string(Path.DirectorySeparatorChar, 3))));
            Assert.Throws<ArgumentException>(() => Create(Path.Combine(testDir.FullName, "*")));
            Assert.Throws<ArgumentException>(() => Create(Path.Combine(testDir.FullName, "Test*t")));
            Assert.Throws<ArgumentException>(() => Create(Path.Combine(testDir.FullName, "*Tes*t")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsWhitespacePath()
        {
            Assert.Throws<ArgumentException>(() => Create("         "));
            Assert.Throws<ArgumentException>(() => Create(" "));
            Assert.Throws<ArgumentException>(() => Create("\n"));
            Assert.Throws<ArgumentException>(() => Create(">"));
            Assert.Throws<ArgumentException>(() => Create("<"));
            Assert.Throws<ArgumentException>(() => Create("\0"));
            Assert.Throws<ArgumentException>(() => Create("\t"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UnixWhitespacePath()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<ArgumentException>(() => Create("\0"));
            using (Create(Path.Combine(testDir.FullName, "          ")))
            using (Create(Path.Combine(testDir.FullName, " ")))
            using (Create(Path.Combine(testDir.FullName, "\n")))
            using (Create(Path.Combine(testDir.FullName, ">")))
            using (Create(Path.Combine(testDir.FullName, "<")))
            using (Create(Path.Combine(testDir.FullName, "\t")))
            {
                Assert.True(File.Exists(Path.Combine(testDir.FullName, "          ")));
                Assert.True(File.Exists(Path.Combine(testDir.FullName, " ")));
                Assert.True(File.Exists(Path.Combine(testDir.FullName, "\n")));
                Assert.True(File.Exists(Path.Combine(testDir.FullName, ">")));
                Assert.True(File.Exists(Path.Combine(testDir.FullName, "<")));
                Assert.True(File.Exists(Path.Combine(testDir.FullName, "\t")));
            }
        }

        #endregion
    }

    public class File_Create_str_i : File_Create_str
    {
        public override FileStream Create(string path)
        {
            return File.Create(path, 4096); // Default buffer size
        }

        public virtual FileStream Create(string path, int bufferSize)
        {
            return File.Create(path, bufferSize);
        }

        [Fact]
        public void NegativeBuffer()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(GetTestFilePath(), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(GetTestFilePath(), -100));
        }
    }

    public class File_Create_str_i_fo : File_Create_str_i
    {
        public override FileStream Create(string path)
        {
            return File.Create(path, 4096, FileOptions.Asynchronous);
        }

        public override FileStream Create(string path, int bufferSize)
        {
            return File.Create(path, bufferSize, FileOptions.Asynchronous);
        }
    }
}
