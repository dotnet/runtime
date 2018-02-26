// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public partial class PathTests : PathTestsBase
    {
        [Theory,
            InlineData(null, null, null),
            InlineData(null, "exe", null),
            InlineData("", "", ""),
            InlineData("file.exe", null, "file"),
            InlineData("file.exe", "", "file."),
            InlineData("file", "exe", "file.exe"),
            InlineData("file", ".exe", "file.exe"),
            InlineData("file.txt", "exe", "file.exe"),
            InlineData("file.txt", ".exe", "file.exe"),
            InlineData("file.txt.bin", "exe", "file.txt.exe"),
            InlineData("dir/file.t", "exe", "dir/file.exe"),
            InlineData("dir/file.exe", "t", "dir/file.t"),
            InlineData("dir/file", "exe", "dir/file.exe")]
        public void ChangeExtension(string path, string newExtension, string expected)
        {
            if (expected != null)
                expected = expected.Replace('/', Path.DirectorySeparatorChar);
            if (path != null)
                path = path.Replace('/', Path.DirectorySeparatorChar);
            Assert.Equal(expected, Path.ChangeExtension(path, newExtension));
        }

        [Fact]
        public void GetDirectoryName_NullReturnsNull()
        {
            Assert.Null(Path.GetDirectoryName(null));
        }

        [Theory, MemberData(nameof(TestData_GetDirectoryName))]
        public void GetDirectoryName(string path, string expected)
        {
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Fact]
        public void GetDirectoryName_CurrentDirectory()
        {
            string curDir = Directory.GetCurrentDirectory();
            Assert.Equal(curDir, Path.GetDirectoryName(Path.Combine(curDir, "baz")));

            Assert.Equal(null, Path.GetDirectoryName(Path.GetPathRoot(curDir)));
        }

        [Fact]
        public void GetExtension_Null()
        {
            Assert.Null(Path.GetExtension(null));
        }

        [Theory, MemberData(nameof(TestData_GetExtension))]
        public void GetExtension(string path, string expected)
        {
            Assert.Equal(expected, Path.GetExtension(path));
            Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path));
        }

        [Fact]
        public void GetFileName_Null()
        {
            Assert.Null(Path.GetFileName(null));
        }

        [Fact]
        public void GetFileName_Empty()
        {
            Assert.Empty(Path.GetFileName(string.Empty));
        }

        [Theory, MemberData(nameof(TestData_GetFileName))]
        public void GetFileName(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFileName(path));
            Assert.Equal(expected, Path.GetFileName(Path.Combine("whizzle", path)));
        }

        [Fact]
        public void GetFileNameWithoutExtension_Null()
        {
            Assert.Null(Path.GetFileNameWithoutExtension(null));
        }

        [Theory, MemberData(nameof(TestData_GetFileNameWithoutExtension))]
        public void GetFileNameWithoutExtension(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFileNameWithoutExtension(path));
        }

        [Fact]
        public void GetPathRoot_Null()
        {
            Assert.Null(Path.GetPathRoot(null));
        }

        [Fact]
        public void GetPathRoot_Basic()
        {
            string cwd = Directory.GetCurrentDirectory();
            Assert.Equal(cwd.Substring(0, cwd.IndexOf(Path.DirectorySeparatorChar) + 1), Path.GetPathRoot(cwd));
            Assert.True(Path.IsPathRooted(cwd));

            Assert.Equal(string.Empty, Path.GetPathRoot(@"file.exe"));
            Assert.False(Path.IsPathRooted("file.exe"));
        }

        [Fact]
        public void GetInvalidPathChars_Invariants()
        {
            Assert.NotNull(Path.GetInvalidPathChars());
            Assert.NotSame(Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
            Assert.True(Path.GetInvalidPathChars().Length > 0);
        }

        [Fact]
        public void InvalidPathChars_MatchesGetInvalidPathChars()
        {
#pragma warning disable 0618
            Assert.NotNull(Path.InvalidPathChars);
            Assert.Equal(Path.GetInvalidPathChars(), Path.InvalidPathChars);
            Assert.Same(Path.InvalidPathChars, Path.InvalidPathChars);
#pragma warning restore 0618
        }

        [Fact]
        public void GetInvalidFileNameChars_Invariants()
        {
            Assert.NotNull(Path.GetInvalidFileNameChars());
            Assert.NotSame(Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
            Assert.Equal((IEnumerable<char>)Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
            Assert.True(Path.GetInvalidFileNameChars().Length > 0);
        }

        [Fact]
        public void GetRandomFileName()
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var fileNames = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                string s = Path.GetRandomFileName();
                Assert.Equal(s.Length, 8 + 1 + 3);
                Assert.Equal(s[8], '.');
                Assert.Equal(-1, s.IndexOfAny(invalidChars));
                Assert.True(fileNames.Add(s));
            }
        }

        [Fact]
        public void GetTempPath_Default()
        {
            string tmpPath = Path.GetTempPath();
            Assert.False(string.IsNullOrEmpty(tmpPath));
            Assert.Equal(tmpPath, Path.GetTempPath());
            Assert.Equal(Path.DirectorySeparatorChar, tmpPath[tmpPath.Length - 1]);
            Assert.True(Directory.Exists(tmpPath));
        }

        [Fact]
        public void GetTempFileName()
        {
            string tmpFile = Path.GetTempFileName();
            try
            {
                Assert.True(File.Exists(tmpFile));
                Assert.Equal(".tmp", Path.GetExtension(tmpFile), ignoreCase: true, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);
                Assert.Equal(-1, tmpFile.IndexOfAny(Path.GetInvalidPathChars()));
                using (FileStream fs = File.OpenRead(tmpFile))
                {
                    Assert.Equal(0, fs.Length);
                }
                Assert.Equal(Path.Combine(Path.GetTempPath(), Path.GetFileName(tmpFile)), tmpFile);
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public void GetFullPath_InvalidArgs()
        {
            Assert.Throws<ArgumentNullException>(() => Path.GetFullPath(null));
            AssertExtensions.Throws<ArgumentException>("path", null, () => Path.GetFullPath(string.Empty));
        }

        public static IEnumerable<object[]> GetFullPath_BasicExpansions_TestData()
        {
            string curDir = Directory.GetCurrentDirectory();
            yield return new object[] { curDir, curDir }; // Current directory => current directory
            yield return new object[] { ".", curDir }; // "." => current directory
            yield return new object[] { "..", Path.GetDirectoryName(curDir) }; // "." => up a directory
            yield return new object[] { Path.Combine(curDir, ".", ".", ".", ".", "."), curDir }; // "dir/./././." => "dir"
            yield return new object[] { curDir + new string(Path.DirectorySeparatorChar, 3) + ".", curDir }; // "dir///." => "dir"
            yield return new object[] { Path.Combine(curDir, "..", Path.GetFileName(curDir), ".", "..", Path.GetFileName(curDir)), curDir }; // "dir/../dir/./../dir" => "dir"
            yield return new object[] { Path.Combine(Path.GetPathRoot(curDir), "somedir", ".."), Path.GetPathRoot(curDir) }; // "C:\somedir\.." => "C:\"
            yield return new object[] { Path.Combine(Path.GetPathRoot(curDir), "."), Path.GetPathRoot(curDir) }; // "C:\." => "C:\"
            yield return new object[] { Path.Combine(Path.GetPathRoot(curDir), "..", "..", "..", ".."), Path.GetPathRoot(curDir) }; // "C:\..\..\..\.." => "C:\"
            yield return new object[] { Path.GetPathRoot(curDir) + new string(Path.DirectorySeparatorChar, 3), Path.GetPathRoot(curDir) }; // "C:\\\" => "C:\"

            // Path longer than MaxPath that normalizes down to less than MaxPath
            const int Iters = 10000;
            var longPath = new StringBuilder(curDir, curDir.Length + (Iters * 2));
            for (int i = 0; i < 10000; i++)
            {
                longPath.Append(Path.DirectorySeparatorChar).Append('.');
            }
            yield return new object[] { longPath.ToString(), curDir };
        }

        [Theory, MemberData(nameof(GetFullPath_BasicExpansions_TestData))]
        public void GetFullPath_BasicExpansions(string path, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path));
        }
    }
}
