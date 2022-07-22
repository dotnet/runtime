// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class Directory_CreateTempSubdirectory : FileSystemTest
    {
        public static TheoryData<string> CreateTempSubdirectoryData
        {
            get
            {
                var result = new TheoryData<string>() { null, "", "myDir", "my.Dir" };
                if (!OperatingSystem.IsWindows())
                {
                    // ensure we can use backslashes on Unix since that isn't a directory separator
                    result.Add(@"my\File");
                    result.Add(@"\");
                }
                return result;
            }
        }

        [Theory]
        [MemberData(nameof(CreateTempSubdirectoryData))]
        public void CreateTempSubdirectory(string prefix)
        {
            DirectoryInfo tmpDir = Directory.CreateTempSubdirectory(prefix);
            try
            {
                Assert.True(tmpDir.Exists);
                Assert.Equal(-1, tmpDir.FullName.IndexOfAny(Path.GetInvalidPathChars()));
                Assert.Empty(Directory.GetFiles(tmpDir.FullName));
                Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetTempPath()), tmpDir.Parent.FullName);

                if (!string.IsNullOrEmpty(prefix))
                {
                    Assert.StartsWith(prefix, tmpDir.Name);
                }

                // Ensure a file can be written to the directory
                string tempFile = Path.Combine(tmpDir.FullName, "newFile");
                using (FileStream fs = File.Create(tempFile, bufferSize: 1024, FileOptions.DeleteOnClose))
                {
                    Assert.Equal(0, fs.Length);
                }
            }
            finally
            {
                tmpDir.Delete(recursive: true);
            }
        }

        public static TheoryData<string> InvalidPrefixData
        {
            get
            {
                var result = new TheoryData<string>() { "/", "myDir/", "my/Dir" };
                if (OperatingSystem.IsWindows())
                {
                    result.Add(@"\");
                    result.Add(@"myDir\");
                    result.Add(@"my\Dir");
                }
                return result;
            }
        }

        [Theory]
        [MemberData(nameof(InvalidPrefixData))]
        public void CreateTempSubdirectoryThrowsWithPrefixContainingDirectorySeparator(string prefix)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => Directory.CreateTempSubdirectory(prefix));
            Assert.Equal("prefix", e.ParamName);
        }
    }
}
