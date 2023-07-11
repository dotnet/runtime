// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace System.IO.Tests
{
    public partial class File_Copy_str_str : FileSystemTest
    {
        protected virtual void Copy(string source, string dest)
        {
            File.Copy(source, dest);
        }

        #region UniversalTests

        [Fact]
        public void NullFileName()
        {
            Assert.Throws<ArgumentNullException>(() => Copy(null, "."));
            Assert.Throws<ArgumentNullException>(() => Copy(".", null));
        }

        [Fact]
        public void EmptyFileName()
        {
            Assert.Throws<ArgumentException>(() => Copy(string.Empty, "."));
            Assert.Throws<ArgumentException>(() => Copy(".", string.Empty));
        }

        [Fact]
        public void CopyOntoDirectory()
        {
            string testFile = GetTestFilePath();
            string targetTestDirectory = Directory.CreateDirectory(GetTestFilePath()).FullName;
            File.Create(testFile).Dispose();
            Assert.Throws<IOException>(() => Copy(testFile, targetTestDirectory));
        }

        [Fact]
        public void CopyOntoSelf()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();
            Assert.Throws<IOException>(() => Copy(testFile, testFile));
        }

        [Fact]
        public void NonExistentPath()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            Assert.Throws<FileNotFoundException>(() => Copy(GetTestFilePath(), testFile.FullName));
            Assert.Throws<DirectoryNotFoundException>(() => Copy(testFile.FullName, Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName())));
            Assert.Throws<DirectoryNotFoundException>(() => Copy(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), testFile.FullName));
        }

        [Fact]
        public void CopyValid()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            File.Create(testFileSource).Dispose();
            Copy(testFileSource, testFileDest);
            Assert.True(File.Exists(testFileDest));
            Assert.True(File.Exists(testFileSource));
        }

        [Fact]
        public void ShortenLongPath()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = Path.GetDirectoryName(testFileSource) + string.Concat(Enumerable.Repeat(Path.DirectorySeparatorChar + ".", 90).ToArray()) + Path.DirectorySeparatorChar + Path.GetFileName(testFileSource);
            File.Create(testFileSource).Dispose();
            Assert.Throws<IOException>(() => Copy(testFileSource, testFileDest));
        }

        [Fact]
        public void InvalidFileNames()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();
            Assert.Throws<ArgumentException>(() => Copy(testFile, "\0"));
            Assert.Throws<ArgumentException>(() => Copy(testFile, "*\0*"));
            Assert.Throws<ArgumentException>(() => Copy("*\0*", testFile));
            Assert.Throws<ArgumentException>(() => Copy("\0", testFile));
        }

        public static IEnumerable<object[]> CopyFileWithData_MemberData()
        {
            var rand = new Random();
            foreach (bool readOnly in new[] { true, false })
            {
                foreach (int length in new[] { 0, 1, 3, 4096, 1024 * 80, 1024 * 1024 * 10 })
                {
                    char[] data = new char[length];
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = (char)rand.Next(0, 256);
                    }
                    yield return new object[] { data, readOnly};
                }
            }
        }

        [Theory]
        [MemberData(nameof(CopyFileWithData_MemberData))]
        public void CopyFileWithData(char[] data, bool readOnly)
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();

            // Write and copy file
            using (StreamWriter stream = new StreamWriter(File.Create(testFileSource)))
            {
                stream.Write(data, 0, data.Length);
            }

            // Set the last write time of the source file to something a while ago
            DateTime lastWriteTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));
            File.SetLastWriteTime(testFileSource, lastWriteTime);

            if (readOnly)
            {
                File.SetAttributes(testFileSource, FileAttributes.ReadOnly);
            }

            // Copy over the data
            Copy(testFileSource, testFileDest);

            // Ensure copy transferred written data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[data.Length];
                stream.Read(readData, 0, data.Length);
                AssertExtensions.Equal(data, readData);
            }

            // Ensure last write/access time on the new file is appropriate
            //
            // For browser, there is technically only 1 time.  It's the max
            // of LastWrite and LastAccess.  On browser, File.SetLastWriteTime
            // overwrites LastWrite and LastAccess, and File.Copy
            // overwrites LastWrite , so this check doesn't apply.
            if (PlatformDetection.IsNotBrowser)
            {
                Assert.InRange(File.GetLastWriteTimeUtc(testFileDest), lastWriteTime.AddSeconds(-1), lastWriteTime.AddSeconds(1));
            }

            Assert.Equal(readOnly, (File.GetAttributes(testFileDest) & FileAttributes.ReadOnly) != 0);
            if (readOnly)
            {
                File.SetAttributes(testFileSource, FileAttributes.Normal);
                File.SetAttributes(testFileDest, FileAttributes.Normal);
            }
        }

        #endregion

        #region PlatformSpecific

        [Theory,
            InlineData("         "),
            InlineData(" ")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsAllSpacePath(string invalid)
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();

            Assert.Throws<ArgumentException>(() => Copy(testFile, invalid));
            Assert.Throws<ArgumentException>(() => Copy(invalid, testFile));
        }

        [Theory,
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsInvalidCharsPath_Core(string invalid)
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();

            Assert.Throws<IOException>(() => Copy(testFile, invalid));
            Assert.Throws<IOException>(() => Copy(invalid, testFile));
        }

        [Theory,
            InlineData("         "),
            InlineData(" "),
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UnixInvalidWindowsPaths(string valid)
        {
            // Unix allows whitespaces paths that aren't valid on Windows
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();

            Copy(testFile, Path.Combine(TestDirectory, valid));
            Assert.True(File.Exists(testFile));
            Assert.True(File.Exists(Path.Combine(TestDirectory, valid)));
        }

        [Theory,
            InlineData("", ":bar"),
            InlineData("", ":bar:$DATA"),
            InlineData("::$DATA", ":bar"),
            InlineData("::$DATA", ":bar:$DATA")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsAlternateDataStream(string defaultStream, string alternateStream)
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDirectory.FullName, GetTestFileName());
            string testFileDefaultStream = testFile + defaultStream;
            string testFileAlternateStream = testFile + alternateStream;

            // Copy the default stream into an alternate stream
            File.WriteAllText(testFileDefaultStream, "Foo");
            Copy(testFileDefaultStream, testFileAlternateStream);
            Assert.Equal(testFile, testDirectory.GetFiles().Single().FullName);
            Assert.Equal("Foo", File.ReadAllText(testFileDefaultStream));
            Assert.Equal("Foo", File.ReadAllText(testFileAlternateStream));

            // Copy another file over the alternate stream
            string testFile2 = Path.Combine(testDirectory.FullName, GetTestFileName());
            string testFile2DefaultStream = testFile2 + defaultStream;
            File.WriteAllText(testFile2DefaultStream, "Bar");
            Assert.Throws<IOException>(() => Copy(testFile2DefaultStream, testFileAlternateStream));

            // This always throws as you can't copy an alternate stream out (oddly)
            Assert.Throws<IOException>(() => Copy(testFileAlternateStream, testFile2));
            Assert.Throws<IOException>(() => Copy(testFileAlternateStream, testFile2 + alternateStream));
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData("/proc/cmdline")]
        [InlineData("/proc/version")]
        [InlineData("/proc/filesystems")]
        public void Linux_CopyFromProcfsToFile(string path)
        {
            string testFile = GetTestFilePath();
            File.Copy(path, testFile);
            Assert.Equal(File.ReadAllText(path), File.ReadAllText(testFile)); // assumes chosen files won't change between reads
        }
        #endregion
    }

    public class File_Copy_str_str_b : File_Copy_str_str
    {
        protected override void Copy(string source, string dest)
        {
            File.Copy(source, dest, false);
        }

        protected virtual void Copy(string source, string dest, bool overwrite)
        {
            File.Copy(source, dest, overwrite);
        }

        [Fact]
        public void OverwriteTrue()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            char[] sourceData = { 'a', 'A', 'b' };
            char[] destData = { 'x', 'X', 'y' };

            // Write and copy file
            using (StreamWriter sourceStream = new StreamWriter(File.Create(testFileSource)))
            using (StreamWriter destStream = new StreamWriter(File.Create(testFileDest)))
            {
                sourceStream.Write(sourceData, 0, sourceData.Length);
                destStream.Write(destData, 0, destData.Length);
            }
            Copy(testFileSource, testFileDest, true);

            // Ensure copy transferred written data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[sourceData.Length];
                stream.Read(readData, 0, sourceData.Length);
                AssertExtensions.Equal(sourceData, readData);
            }
        }

        [Fact]
        public void OverwriteFalse()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            char[] sourceData = { 'a', 'A', 'b' };
            char[] destData = { 'x', 'X', 'y' };

            // Write and copy file
            using (StreamWriter sourceStream = new StreamWriter(File.Create(testFileSource)))
            using (StreamWriter destStream = new StreamWriter(File.Create(testFileDest)))
            {
                sourceStream.Write(sourceData, 0, sourceData.Length);
                destStream.Write(destData, 0, destData.Length);
            }
            Assert.Throws<IOException>(() => Copy(testFileSource, testFileDest, false));

            // Ensure copy didn't overwrite existing data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[sourceData.Length];
                stream.Read(readData, 0, sourceData.Length);
                AssertExtensions.Equal(destData, readData);
            }
        }

        [Theory,
            InlineData("", ":bar"),
            InlineData("", ":bar:$DATA"),
            InlineData("::$DATA", ":bar"),
            InlineData("::$DATA", ":bar:$DATA")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsAlternateDataStreamOverwrite(string defaultStream, string alternateStream)
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            string testFile = Path.Combine(testDirectory.FullName, GetTestFileName());
            string testFileDefaultStream = testFile + defaultStream;
            string testFileAlternateStream = testFile + alternateStream;

            // Copy the default stream into an alternate stream
            File.WriteAllText(testFileDefaultStream, "Foo");
            Copy(testFileDefaultStream, testFileAlternateStream);
            Assert.Equal(testFile, testDirectory.GetFiles().Single().FullName);
            Assert.Equal("Foo", File.ReadAllText(testFileDefaultStream));
            Assert.Equal("Foo", File.ReadAllText(testFileAlternateStream));

            // Copy another file over the alternate stream
            string testFile2 = Path.Combine(testDirectory.FullName, GetTestFileName());
            string testFile2DefaultStream = testFile2 + defaultStream;
            File.WriteAllText(testFile2DefaultStream, "Bar");
            Copy(testFile2DefaultStream, testFileAlternateStream, overwrite: true);
            Assert.Equal("Foo", File.ReadAllText(testFileDefaultStream));
            Assert.Equal("Bar", File.ReadAllText(testFileAlternateStream));

            // This always throws as you can't copy an alternate stream out (oddly)
            Assert.Throws<IOException>(() => Copy(testFileAlternateStream, testFile2, overwrite: true));
            Assert.Throws<IOException>(() => Copy(testFileAlternateStream, testFile2 + alternateStream, overwrite: true));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled))]
        public void CopyOntoLockedFile()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            File.Create(testFileSource).Dispose();
            File.Create(testFileDest).Dispose();
            using (var stream = new FileStream(testFileDest, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Throws<IOException>(() => Copy(testFileSource, testFileDest, overwrite: true));
            }
        }

        [Fact]
        public void DestinationFileIsTruncatedWhenItsLargerThanSourceFile()
        {
            string sourcePath = GetTestFilePath();
            string destPath = GetTestFilePath();

            byte[] content = RandomNumberGenerator.GetBytes(1000);
            File.WriteAllBytes(sourcePath, content);
            File.WriteAllBytes(destPath, RandomNumberGenerator.GetBytes(content.Length * 2));

            Copy(sourcePath, destPath, overwrite: true);

            Assert.Equal(content, File.ReadAllBytes(destPath));
        }
    }

    /// <summary>
    /// Single tests that shouldn't be duplicated by inheritance.
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled))]
    public sealed class File_Copy_Single : FileSystemTest
    {
        [Fact]
        public void EnsureThrowWhenCopyToNonSharedFile()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            string file1 = Path.Combine(testDirectory.FullName, GetTestFileName());
            string file2 = Path.Combine(testDirectory.FullName, GetTestFileName());

            File.WriteAllText(file1, "foo");
            File.WriteAllText(file2, "bar");

            using var stream = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.Throws<IOException>(() => File.Copy(file2, file1, overwrite: true));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks)),
            InlineData("", ""),
            /*InlineData(":a", ""),*/
            InlineData("", ":a")/*,
            InlineData(":a", ":a")*/]
        //todo: is copying from an ADS meant to fail?
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsAlternateDataStreamSymlinkTest(string stream1, string stream2)
        {
            // This test checks copying all combinations of alternate data streams with all combinations of symlinks referencing them.
            // This test exists to check we don't cause a BSOD when using ReFS block copy operation on alternative data streams (pending? rolled out fix from Windows team), and that it has the correct behaviour.

            string sourceFile = GetTestFilePath();
            string destFile = GetTestFilePath();

            void Test(string src, string dst)
            {
                try
                {
                    File.WriteAllText(sourceFile, "abc");
                    File.WriteAllText(destFile, "def");
                    File.WriteAllText(sourceFile + stream1, "ghi");
                    File.WriteAllText(destFile + stream2, "jkl");

                    File.Copy(src, dst, true);

                    if (stream1 != "") Assert.Equal("abc", File.ReadAllText(sourceFile));
                    if (stream2 != "") Assert.Equal("def", File.ReadAllText(destFile));
                    Assert.Equal("ghi", File.ReadAllText(sourceFile + stream1));
                    Assert.Equal("ghi", File.ReadAllText(destFile + stream2));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed with src={src}, dst={dst}.", ex);
                }
            }

            File.CreateSymbolicLink(sourceFile + ".link", sourceFile + stream1);
            File.CreateSymbolicLink(destFile + ".link", destFile + stream2);

            Test(sourceFile + stream1, destFile + stream2);
            Test(sourceFile + stream1, destFile + ".link");
            Test(sourceFile + ".link", destFile + stream2);
            Test(sourceFile + ".link", destFile + ".link");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public unsafe void WindowsCheckSparseness()
        {
            string sourceFile = GetTestFilePath();
            string destFile = GetTestFilePath();

            File.WriteAllText(sourceFile, "abc");
            File.WriteAllText(destFile, "def");

            Assert.True((File.GetAttributes(sourceFile) & FileAttributes.SparseFile) == 0);
            File.Copy(sourceFile, destFile, true);
            Assert.True((File.GetAttributes(destFile) & FileAttributes.SparseFile) == 0);
            Assert.Equal("abc", File.ReadAllText(sourceFile));

            using (FileStream file = File.Open(sourceFile, FileMode.Open))
            {
                DeviceIoControl(file.SafeFileHandle.DangerousGetHandle(), /*FSCTL_SET_SPARSE*/ 0x000900c4, null, 0, null, 0, out _, 0);
            }
            File.WriteAllText(destFile, "def");

            Assert.True((File.GetAttributes(sourceFile) & FileAttributes.SparseFile) != 0);
            File.Copy(sourceFile, destFile, true);
            Assert.True((File.GetAttributes(destFile) & FileAttributes.SparseFile) != 0);
            Assert.Equal("abc", File.ReadAllText(sourceFile));

            [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static unsafe extern bool DeviceIoControl(
                IntPtr hDevice,
                uint dwIoControlCode,
                void* lpInBuffer,
                uint nInBufferSize,
                void* lpOutBuffer,
                uint nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);
        }

        // Todo: add a way to run all these on ReFS, and a test to check we actually cloned the reference, not just the data on ReFS.
    }
}
