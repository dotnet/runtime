// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_GetFileSystemEntries_str : FileSystemTest
    {
        #region Utilities


        protected virtual bool TestFiles { get { return true; } }       // True if the virtual GetEntries mmethod returns files
        protected virtual bool TestDirectories { get { return true; } } // True if the virtual GetEntries mmethod returns Directories

        public virtual string[] GetEntries(string dirName)
        {
            return Directory.GetFileSystemEntries(dirName);
        }

        /// <summary>
        /// Create a file at the given path or directory if GetEntries doesn't return files
        /// </summary>
        protected void CreateItem(string path)
        {
            if (TestFiles)
                File.WriteAllText(path, path);
            else
                Directory.CreateDirectory(path);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullFileName()
        {
            Assert.Throws<ArgumentNullException>(() => GetEntries(null));
        }

        [Fact]
        public void EmptyFileName()
        {
            Assert.Throws<ArgumentException>(() => GetEntries(string.Empty));
        }

        [Fact]
        public void InvalidFileNames()
        {
            Assert.Throws<DirectoryNotFoundException>(() => GetEntries("DoesNotExist"));
            Assert.Throws<ArgumentException>(() => GetEntries("\0"));
        }

        [Fact]
        public void EmptyDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Empty(GetEntries(testDir.FullName));
        }

        [Fact]
        public void GetEntriesThenDelete()
        {
            string testDirPath = GetTestFilePath();
            DirectoryInfo testDirInfo = new DirectoryInfo(testDirPath);
            testDirInfo.Create();
            string testDir1 = GetTestFileName();
            string testDir2 = GetTestFileName();
            string testFile1 = GetTestFileName();
            string testFile2 = GetTestFileName();
            string testFile3 = GetTestFileName();
            string testFile4 = GetTestFileName();
            string testFile5 = GetTestFileName();
            testDirInfo.CreateSubdirectory(testDir1);
            testDirInfo.CreateSubdirectory(testDir2);
            using (File.Create(Path.Combine(testDirPath, testFile1)))
            using (File.Create(Path.Combine(testDirPath, testFile2)))
            using (File.Create(Path.Combine(testDirPath, testFile3)))
            {
                string[] results;
                using (File.Create(Path.Combine(testDirPath, testFile4)))
                using (File.Create(Path.Combine(testDirPath, testFile5)))
                {
                    results = GetEntries(testDirPath);
                    Assert.NotNull(results);
                    Assert.NotEmpty(results);
                    if (TestFiles)
                    {
                        Assert.Contains(Path.Combine(testDirPath, testFile1), results);
                        Assert.Contains(Path.Combine(testDirPath, testFile2), results);
                        Assert.Contains(Path.Combine(testDirPath, testFile3), results);
                        Assert.Contains(Path.Combine(testDirPath, testFile4), results);
                        Assert.Contains(Path.Combine(testDirPath, testFile5), results);
                    }
                    if (TestDirectories)
                    {
                        Assert.Contains(Path.Combine(testDirPath, testDir1), results);
                        Assert.Contains(Path.Combine(testDirPath, testDir2), results);
                    }
                }

                File.Delete(Path.Combine(testDirPath, testFile4));
                File.Delete(Path.Combine(testDirPath, testFile5));
                FailSafeDirectoryOperations.DeleteDirectory(testDir1, true);

                results = GetEntries(testDirPath);
                Assert.NotNull(results);
                Assert.NotEmpty(results);
                if (TestFiles)
                {
                    Assert.Contains(Path.Combine(testDirPath, testFile1), results);
                    Assert.Contains(Path.Combine(testDirPath, testFile2), results);
                    Assert.Contains(Path.Combine(testDirPath, testFile3), results);
                }
                if (TestDirectories)
                {
                    Assert.Contains(Path.Combine(testDirPath, testDir2), results);
                }
            }
        }

        [Fact]
        public virtual void IgnoreSubDirectoryFiles()
        {
            string subDir = GetTestFileName();
            Directory.CreateDirectory(Path.Combine(TestDirectory, subDir));

            string testFile = Path.Combine(TestDirectory, GetTestFileName());
            string testFileInSub = Path.Combine(TestDirectory, subDir, GetTestFileName());
            string testDir = Path.Combine(TestDirectory, GetTestFileName());
            string testDirInSub = Path.Combine(TestDirectory, subDir, GetTestFileName());

            Directory.CreateDirectory(testDir);
            Directory.CreateDirectory(testDirInSub);
            using (File.Create(testFile))
            using (File.Create(testFileInSub))
            {
                string[] results = GetEntries(TestDirectory);
                if (TestFiles)
                    Assert.Contains(testFile, results);
                if (TestDirectories)
                    Assert.Contains(testDir, results);
                Assert.DoesNotContain(testFileInSub, results);
                Assert.DoesNotContain(testDirInSub, results);
            }
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingFile_ThrowsDirectoryNotFound(char trailingChar)
        {
            string path = GetTestFilePath() + trailingChar;
            Assert.Throws<DirectoryNotFoundException>(() => GetEntries(path));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingDirectory_ThrowsDirectoryNotFound(char trailingChar)
        {
            string path = Path.Combine(GetTestFilePath(), "file" + trailingChar);
            Assert.Throws<DirectoryNotFoundException>(() => GetEntries(path));
        }

        [Fact]
        public void TrailingSlashes()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Directory.CreateDirectory(Path.Combine(testDir.FullName, GetTestFileName()));
            using (File.Create(Path.Combine(testDir.FullName, GetTestFileName())))
            {
                string[] strArr = GetEntries(testDir.FullName + new string(Path.DirectorySeparatorChar, 5));
                Assert.NotNull(strArr);
                Assert.NotEmpty(strArr);
            }
        }

        [Fact]
        public void HiddenFilesAreReturned()
        {
            // Note that APIs that take EnumerationOptions do NOT find hidden files by default

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, GetTestFileName()));

            // Put a period in front to make it hidden on Unix
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "." + GetTestFileName()));
            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            if (PlatformDetection.IsWindows)
                fileTwo.Attributes = fileTwo.Attributes | FileAttributes.Hidden;

            if (TestFiles)
            {
                FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName }, GetEntries(testDirectory.FullName));
            }
            else
            {
                Assert.Empty(GetEntries(testDirectory.FullName));
            }
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void InvalidPath_Desktop()
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                string badPath = string.Format($"{TestDirectory}{Path.DirectorySeparatorChar}te{invalid}st");
                switch (invalid)
                {
                    case '/':
                    case '\\':
                        Assert.Throws<DirectoryNotFoundException>(() => GetEntries(badPath));
                        break;
                    case ':':
                        Assert.Throws<NotSupportedException>(() => GetEntries(badPath));
                        break;
                    case '\0':
                        Assert.Throws<ArgumentException>(() => GetEntries(badPath));
                        break;
                    default:
                        Assert.Throws<ArgumentException>(() => GetEntries(badPath));
                        break;
                }
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InvalidPath_Core()
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                string badPath = string.Format($"{TestDirectory}{Path.DirectorySeparatorChar}te{invalid}st");
                switch (invalid)
                {
                    case '/':
                    case '\\':
                    case ':':
                        Assert.Throws<DirectoryNotFoundException>(() => GetEntries(badPath));
                        break;
                    case '\0':
                        Assert.Throws<ArgumentException>(() => GetEntries(badPath));
                        break;
                    default:
                        Assert.Throws<IOException>(() => GetEntries(badPath));
                        break;
                }
            }
        }

        [Theory,
            InlineData("         "),
            InlineData(" ")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsWhitespaceOnlyPath(string invalid)
        {
            Assert.Throws<ArgumentException>(() => GetEntries(invalid));
        }

        [Theory,
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void WindowsInvalidCharsPath_Desktop(string invalid)
        {
            Assert.Throws<ArgumentException>(() => GetEntries(invalid));
        }

        [Theory,
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void WindowsInvalidCharsPath_Core(string invalid)
        {
            Assert.Throws<IOException>(() => GetEntries(invalid));
        }

        [Theory,
            InlineData("         "),
            InlineData(" "),
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix-only valid chars in file path
        public void UnixValidCharsFilePath(string valid)
        {
            if (TestFiles)
            {
                DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

                File.Create(Path.Combine(testDir.FullName, valid)).Dispose();

                string[] results = GetEntries(testDir.FullName);
                Assert.Contains(Path.Combine(testDir.FullName, valid), results);
            }
        }

        [Theory,
            InlineData("         "),
            InlineData(" "),
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Windows-only invalid chars in directory path
        public void UnixValidCharsDirectoryPath(string valid)
        {
            if (TestDirectories)
            {
                DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
                
                testDir.CreateSubdirectory(valid);

                string[] results = GetEntries(testDir.FullName);
                 
                Assert.Contains(Path.Combine(testDir.FullName, valid), results);
            }
        }

        #endregion
    }

    public sealed class Directory_GetEntries_CurrentDirectory : FileCleanupTestBase
    {
        [Fact]
        public void CurrentDirectory()
        {
            string testDir = GetTestFilePath();
            Directory.CreateDirectory(testDir);
            File.WriteAllText(Path.Combine(testDir, GetTestFileName()), "cat");
            Directory.CreateDirectory(Path.Combine(testDir, GetTestFileName()));
            RemoteExecutor.Invoke((testDirectory) =>
            {
                Directory.SetCurrentDirectory(testDirectory);

                Assert.NotEmpty(Directory.GetFileSystemEntries(Directory.GetCurrentDirectory()));
                Assert.NotEmpty(Directory.GetFileSystemEntries(Directory.GetCurrentDirectory(), "*"));
                Assert.NotEmpty(Directory.GetFileSystemEntries(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
                Assert.NotEmpty(Directory.GetFileSystemEntries(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly));

                Assert.NotEmpty(Directory.GetDirectories(Directory.GetCurrentDirectory()));
                Assert.NotEmpty(Directory.GetDirectories(Directory.GetCurrentDirectory(), "*"));
                Assert.NotEmpty(Directory.GetDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
                Assert.NotEmpty(Directory.GetDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly));

                Assert.NotEmpty(Directory.GetFiles(Directory.GetCurrentDirectory()));
                Assert.NotEmpty(Directory.GetFiles(Directory.GetCurrentDirectory(), "*"));
                Assert.NotEmpty(Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
                Assert.NotEmpty(Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly));

                Assert.NotEmpty(Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory()));
                Assert.NotEmpty(Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory(), "*"));
                Assert.NotEmpty(Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
                Assert.NotEmpty(Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly));

                Assert.NotEmpty(Directory.EnumerateDirectories(Directory.GetCurrentDirectory()));
                Assert.NotEmpty(Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "*"));
                Assert.NotEmpty(Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
                Assert.NotEmpty(Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly));

                Assert.NotEmpty(Directory.EnumerateFiles(Directory.GetCurrentDirectory()));
                Assert.NotEmpty(Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*"));
                Assert.NotEmpty(Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
                Assert.NotEmpty(Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly));

                return RemoteExecutor.SuccessExitCode;
            }, testDir).Dispose();
        }
    }
}
