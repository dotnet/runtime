// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    // For tests that cover examples from documentation, blog posts, etc. While these overlap with
    // existing tests, having explicit coverage here is extra insurance we are covering the
    // examples we've given out publicly.
    public class ExampleTests : FileSystemTest
    {
        [Fact]
        public void GetFileNamesEnumerable()
        {
            // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            File.Create(Path.Join(testDirectory.FullName, "one")).Dispose();
            File.Create(Path.Join(testDirectory.FullName, "two")).Dispose();
            Directory.CreateDirectory(Path.Join(testDirectory.FullName, "three"));

            IEnumerable<string> fileNames =
                new FileSystemEnumerable<string>(
                    testDirectory.FullName,
                    (ref FileSystemEntry entry) => entry.FileName.ToString())
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
                };

            FSAssert.EqualWhenOrdered(new string[] { "one", "two" }, fileNames);
        }

        private static IEnumerable<FileInfo> GetFilesWithExtensions(string directory,
            bool recursive, params string[] extensions)
        {
            return new FileSystemEnumerable<FileInfo>(
                directory,
                (ref FileSystemEntry entry) => (FileInfo)entry.ToFileSystemInfo(),
                new EnumerationOptions() { RecurseSubdirectories = recursive })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                {
                    if (entry.IsDirectory)
                        return false;
                    foreach (string extension in extensions)
                    {
                        if (Path.GetExtension(entry.FileName).SequenceEqual(extension))
                            return true;
                    }
                    return false;
                }
            };
        }

        [Fact]
        public void TestGetFilesWithExtensions()
        {
            // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            File.Create(Path.Join(testDirectory.FullName, "file.one")).Dispose();
            File.Create(Path.Join(testDirectory.FullName, "file.two")).Dispose();
            File.Create(Path.Join(testDirectory.FullName, "file.three")).Dispose();
            DirectoryInfo subDirectory = testDirectory.CreateSubdirectory("three.one");
            File.Create(Path.Join(subDirectory.FullName, "subfile.one")).Dispose();

            FSAssert.EqualWhenOrdered(
                new string[] { "file.one", "file.three" },
                GetFilesWithExtensions(testDirectory.FullName, false, ".one", ".three").Select(f => f.Name));

            FSAssert.EqualWhenOrdered(
                new string[] { "file.one", "file.three", "subfile.one" },
                GetFilesWithExtensions(testDirectory.FullName, true, ".one", ".three").Select(f => f.Name));
        }

        private static int CountFiles(string directory, bool recursive)
        {
            return (new FileSystemEnumerable<int>(
                directory,
                (ref FileSystemEntry entry) => 1,
                new EnumerationOptions() { RecurseSubdirectories = recursive })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
            }).Count();
        }

        [Fact]
        public void TestCountFiles()
        {
            // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            File.Create(Path.Join(testDirectory.FullName, "file.one")).Dispose();
            File.Create(Path.Join(testDirectory.FullName, "file.two")).Dispose();
            File.Create(Path.Join(testDirectory.FullName, "file.three")).Dispose();
            DirectoryInfo subDirectory = testDirectory.CreateSubdirectory("three.one");
            File.Create(Path.Join(subDirectory.FullName, "subfile.one")).Dispose();

            Assert.Equal(3, CountFiles(testDirectory.FullName, false));

            Assert.Equal(4, CountFiles(testDirectory.FullName, true));
        }

        private static long CountFileBytes(string directory, bool recursive)
        {
            return (new FileSystemEnumerable<long>(
                directory,
                (ref FileSystemEntry entry) => entry.Length,
                new EnumerationOptions() { RecurseSubdirectories = recursive })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
            }).Sum();
        }

        [Fact]
        public void TestCountFileBytes()
        {
            // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo firstFile = new FileInfo(Path.Join(testDirectory.FullName, "file.one"));
            using (var writer = firstFile.CreateText())
            {
                for (int i = 0; i < 100; i++)
                    writer.WriteLine("The quick brown fox jumped over the lazy dog.");
            }

            firstFile.CopyTo(Path.Join(testDirectory.FullName, "file.two"));
            firstFile.CopyTo(Path.Join(testDirectory.FullName, "file.three"));
            DirectoryInfo subDirectory = testDirectory.CreateSubdirectory("three.one");
            firstFile.CopyTo(Path.Join(subDirectory.FullName, "subfile.one"));

            firstFile.Refresh();
            Assert.True(firstFile.Length > 0, "The file we wrote should have a length.");
            Assert.Equal(firstFile.Length * 3, CountFileBytes(testDirectory.FullName, false));

            Assert.Equal(firstFile.Length * 4, CountFileBytes(testDirectory.FullName, true));
        }
    }
}
