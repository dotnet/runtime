// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public abstract class IncludePredicateTests : FileSystemTest
    {
        public static IEnumerable<string> GetFileFullPathsWithExtension(string directory,
            bool recursive, params string[] extensions)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                new EnumerationOptions() { RecurseSubdirectories = recursive })
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    {
                        if (entry.IsDirectory) return false;
                        foreach (string extension in extensions)
                        {
                            if (Path.GetExtension(entry.FileName).EndsWith(extension))
                                return true;
                        }
                        return false;
                    }
                };
        }

        [Fact]
        public void CustomExtensionMatch()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo testSubdirectory = Directory.CreateDirectory(Path.Combine(testDirectory.FullName, "Subdirectory"));
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "fileone.htm"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "filetwo.html"));
            FileInfo fileThree = new FileInfo(Path.Combine(testSubdirectory.FullName, "filethree.doc"));
            FileInfo fileFour = new FileInfo(Path.Combine(testSubdirectory.FullName, "filefour.docx"));

            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            fileThree.Create().Dispose();
            fileFour.Create().Dispose();

            string[] paths = GetFileFullPathsWithExtension(testDirectory.FullName, true, ".htm", ".doc").ToArray();

            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileThree.FullName }, paths);
        }
    }
}
