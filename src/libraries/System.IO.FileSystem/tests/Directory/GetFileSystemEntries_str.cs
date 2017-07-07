// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_GetFileSystemEntries_str : FileSystemTest
    {
        #region Utilities

        public static TheoryData WindowsInvalidUnixValid = new TheoryData<string> { "         ", " ", "\n", ">", "<", "\t" };
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

        #endregion

        #region PlatformSpecific

        [Fact]
        public void InvalidPath()
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                if (invalid == '/' || invalid == '\\')
                {
                    Assert.Throws<DirectoryNotFoundException>(() => GetEntries(Path.Combine(TestDirectory, string.Format("te{0}st", invalid.ToString()))));
                }
                else if (invalid == ':')
                {
                    if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                        Assert.Throws<NotSupportedException>(() => GetEntries(Path.Combine(TestDirectory, string.Format("te{0}st", invalid.ToString()))));
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => GetEntries(Path.Combine(TestDirectory, string.Format("te{0}st", invalid.ToString()))));
                }
            }
        }

        [Theory,
            MemberData(nameof(WindowsInvalidUnixValid))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Windows-only Invalid chars in path
        public void WindowsInvalidCharsPath(string invalid)
        {
            
            Assert.Throws<ArgumentException>(() => GetEntries(invalid));
        }

        [Theory,
            MemberData(nameof(WindowsInvalidUnixValid))]
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
            MemberData(nameof(WindowsInvalidUnixValid))]
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

    public sealed class Directory_GetEntries_CurrentDirectory : RemoteExecutorTestBase
    {
        [Fact]
        public void CurrentDirectory()
        {
            string testDir = GetTestFilePath();
            Directory.CreateDirectory(testDir);
            File.WriteAllText(Path.Combine(testDir, GetTestFileName()), "cat");
            Directory.CreateDirectory(Path.Combine(testDir, GetTestFileName()));
            RemoteInvoke((testDirectory) =>
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

                return SuccessExitCode;
            }, testDir).Dispose();
        }
    }
}
