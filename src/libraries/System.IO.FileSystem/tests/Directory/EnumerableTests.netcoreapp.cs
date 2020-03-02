// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public partial class EnumerableTests : FileSystemTest
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void EnumerateDirectories_TrailingDot()
        {
            string prefix = @"\\?\";
            string tempPath = GetTestFilePath();
            string fileName = "Test.txt";

            string[] dirPaths = {
                 Path.Join(prefix, tempPath, "Test"),
                 Path.Join(prefix, tempPath, "TestDot."),
                 Path.Join(prefix, tempPath, "TestDotDot..")
             };

            // Create directories and their files using "\\?\C:\" paths
            foreach (string dirPath in dirPaths)
            {
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, recursive: true);
                }

                Directory.CreateDirectory(dirPath);

                // Directory.Exists should work with directories containing trailing dots and prefixed with \\?\
                Assert.True(Directory.Exists(dirPath));

                string filePath = Path.Join(dirPath, fileName);
                using FileStream fs = File.Create(filePath);

                // File.Exists should work with directories containing trailing dots and prefixed with \\?\
                Assert.True(File.Exists(filePath));
            }

            try
            {
                // Enumerate directories and their files using "C:\" paths
                DirectoryInfo sourceInfo = new DirectoryInfo(tempPath);
                foreach (DirectoryInfo dirInfo in sourceInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    // DirectoryInfo.Exists should work with or without \\?\ for folders with trailing dots
                    Assert.True(dirInfo.Exists);

                    if (dirInfo.FullName.EndsWith("."))
                    {
                        // Directory.Exists is not expected to work with directories containing trailing dots and not prefixed with \\?\
                        Assert.False(Directory.Exists(dirInfo.FullName));
                    }

                    foreach (FileInfo fileInfo in dirInfo.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly))
                    {
                        // FileInfo.Exists should work with or without \\?\ for folders with trailing dots
                        Assert.True(fileInfo.Exists);

                        if (fileInfo.Directory.FullName.EndsWith("."))
                        {
                            // File.Exists is not expected to work with directories containing trailing dots and not prefixed with \\?\
                            Assert.False(File.Exists(fileInfo.FullName));
                        }
                    }
                }
            }
            finally
            {
                foreach (string dirPath in dirPaths)
                {
                    Directory.Delete(dirPath, recursive: true);
                }
            }
        }
    }
}
