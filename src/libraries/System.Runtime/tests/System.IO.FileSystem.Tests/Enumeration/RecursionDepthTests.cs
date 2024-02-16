// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class RecursionDepthTests : FileSystemTest
    {
        public static IEnumerable<string> GetEntryNames(string directory, int depth)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.FileName.ToString(),
                new EnumerationOptions() { RecurseSubdirectories = true, MaxRecursionDepth = depth });
        }

        [Theory,
            InlineData(0, 2),
            InlineData(1, 4),
            InlineData(2, 5),
            InlineData(3, 5),
            InlineData(int.MaxValue, 5)
        ]
        public void EnumerateDirectory_WithSpecifedRecursionDepth(int depth, int expectedCount)
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo testSubdirectory1 = Directory.CreateDirectory(Path.Combine(testDirectory.FullName, "Subdirectory1"));
            DirectoryInfo testSubdirectory2 = Directory.CreateDirectory(Path.Combine(testSubdirectory1.FullName, "Subdirectory2"));
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "fileone.htm"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testSubdirectory1.FullName, "filetwo.html"));
            FileInfo fileThree = new FileInfo(Path.Combine(testSubdirectory2.FullName, "filethree.doc"));

            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            fileThree.Create().Dispose();

            string[] results = GetEntryNames(testDirectory.FullName, depth).ToArray();
            Assert.Equal(expectedCount, results.Length);
        }

        [Fact]
        public void NegativeRecursionDepth_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnumerationOptions() { MaxRecursionDepth = -1 });
        }
    }
}
