// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    // For tests that cover examples from documentation, blog posts, etc.
    public class ExampleTests : FileSystemTest
    {
        [Fact]
        public void GetFileNamesEnumerable()
        {
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
    }
}
