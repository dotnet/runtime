// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public static class PathTests
{
    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, null, "exe")]
    [InlineData("", "", "")]
    [InlineData("file", "file.exe", null)]
    [InlineData("file.", "file.exe", "")]
    [InlineData("file.exe", "file", "exe")]
    [InlineData("file.exe", "file", ".exe")]
    [InlineData("file.exe", "file.txt", "exe")]
    [InlineData("file.exe", "file.txt", ".exe")]
    [InlineData("file.txt.exe", "file.txt.bin", "exe")]
    [InlineData("dir/file.exe", "dir/file.t", "exe")]
    [InlineData("dir/file.t", "dir/file.exe", "t")]
    [InlineData("dir/file.exe", "dir/file", "exe")]
    public static void ChangeExtension(string expected, string path, string newExtension)
    {
        if (expected != null)
            expected = expected.Replace('/', Path.DirectorySeparatorChar);
        if (path != null)
            path = path.Replace('/', Path.DirectorySeparatorChar);
        Assert.Equal(expected, Path.ChangeExtension(path, newExtension));
    }

    [Fact]
    public static void GetDirectoryName()
    {
        Assert.Null(Path.GetDirectoryName(null));
        Assert.Equal("dir", Path.GetDirectoryName(Path.Combine("dir", "baz")));
        Assert.Equal(Path.GetDirectoryName("."), Path.GetDirectoryName("dir"));
        Assert.Equal(null, Path.GetDirectoryName(Path.GetPathRoot(Directory.GetCurrentDirectory())));
    }

    [Theory]
    [InlineData(".exe", "file.exe")]
    [InlineData("", "file")]
    [InlineData(null, null)]
    [InlineData("", "file.")]
    [InlineData(".s", "file.s")]
    [InlineData("", "test/file")]
    [InlineData(".extension", "test/file.extension")]
    public static void GetExtension(string expected, string path)
    {
        if (path != null)
            path = path.Replace('/', Path.DirectorySeparatorChar);
        Assert.Equal(expected, Path.GetExtension(path));
        Assert.Equal(!string.IsNullOrEmpty(expected), Path.HasExtension(path));
    }

    [Fact]
    public static void GetFileName()
    {
        Assert.Equal("file.exe", Path.GetFileName(Path.Combine("bar", "baz", "file.exe")));
        Assert.Equal(string.Empty, Path.GetFileName(Path.Combine("bar", "baz") + Path.DirectorySeparatorChar));
    }

    [Fact]
    public static void GetFileNameWithoutExtension()
    {
        Assert.Equal("file", Path.GetFileNameWithoutExtension(Path.Combine("bar","baz","file.exe")));
        Assert.Equal(string.Empty, Path.GetFileNameWithoutExtension(Path.Combine("bar","baz") + Path.DirectorySeparatorChar));
        Assert.Null(Path.GetFileNameWithoutExtension(null));
    }

    [Fact]
    public static void GetPathRoot()
    {
        Assert.Null(Path.GetPathRoot(null));

        string cwd = Directory.GetCurrentDirectory();
        Assert.Equal(cwd.Substring(0, cwd.IndexOf(Path.DirectorySeparatorChar) + 1), Path.GetPathRoot(cwd));
        Assert.True(Path.IsPathRooted(cwd));

        Assert.Equal(string.Empty, Path.GetPathRoot(@"file.exe"));
        Assert.False(Path.IsPathRooted("file.exe"));

        if (Interop.IsWindows) // UNC paths
        {
            Assert.Equal(@"\\test\unc", Path.GetPathRoot(@"\\test\unc\path\to\something"));
            Assert.True(Path.IsPathRooted(@"\\test\unc\path\to\something"));
        }
    }

    [Fact]
    public static void GetRandomFileName()
    {
        var fileNames = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            string s = Path.GetRandomFileName();
            Assert.Equal(s.Length, 8 + 1 + 3);
            Assert.Equal(s[8], '.');
            Assert.True(fileNames.Add(s));
        }
    }

    [Fact]
    public static void GetInvalidPathChars()
    {
        Assert.NotNull(Path.GetInvalidPathChars());
        Assert.NotSame(Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
        Assert.Equal((IEnumerable<char>)Path.GetInvalidPathChars(), (IEnumerable<char>)Path.GetInvalidPathChars());
        Assert.True(Path.GetInvalidPathChars().Length > 0);
        Assert.All(Path.GetInvalidPathChars(), c =>
        {
            string bad = c.ToString();
            Assert.Throws<ArgumentException>(() => Path.ChangeExtension(bad, "ok"));
            Assert.Throws<ArgumentException>(() => Path.Combine(bad, "ok"));
            Assert.Throws<ArgumentException>(() => Path.Combine("ok", "ok", bad));
            Assert.Throws<ArgumentException>(() => Path.Combine("ok", "ok", bad, "ok"));
            Assert.Throws<ArgumentException>(() => Path.Combine(bad, bad, bad, bad, bad));
            Assert.Throws<ArgumentException>(() => Path.GetDirectoryName(bad));
            Assert.Throws<ArgumentException>(() => Path.GetExtension(bad));
            Assert.Throws<ArgumentException>(() => Path.GetFileName(bad));
            Assert.Throws<ArgumentException>(() => Path.GetFileNameWithoutExtension(bad));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(bad));
            Assert.Throws<ArgumentException>(() => Path.GetPathRoot(bad));
            Assert.Throws<ArgumentException>(() => Path.IsPathRooted(bad));
        });
    }

    [Fact]
    public static void GetInvalidFileNameChars()
    {
        Assert.NotNull(Path.GetInvalidFileNameChars());
        Assert.NotSame(Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
        Assert.Equal((IEnumerable<char>)Path.GetInvalidFileNameChars(), (IEnumerable<char>)Path.GetInvalidFileNameChars());
        Assert.True(Path.GetInvalidFileNameChars().Length > 0);
    }

    [Fact]
    public static void GetTempPath()
    {
        string tmpPath = Path.GetTempPath();
        Assert.False(string.IsNullOrEmpty(tmpPath));
        Assert.Equal(tmpPath, Path.GetTempPath());
        Assert.Equal(Path.DirectorySeparatorChar, tmpPath[tmpPath.Length - 1]);
        Assert.True(Directory.Exists(tmpPath));
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Theory]
    [InlineData(@"C:\Users\someuser\AppData\Local\Temp\", @"C:\Users\someuser\AppData\Local\Temp")]
    [InlineData(@"C:\Users\someuser\AppData\Local\Temp\", @"C:\Users\someuser\AppData\Local\Temp\")]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"C:\tmp\", @"C:\tmp")]
    [InlineData(@"C:\tmp\", @"C:\tmp\")]
    public static void GetTempPath_SetEnvVar_Windows(string expected, string newTempPath)
    {
        GetTempPath_SetEnvVar("TMP", expected, newTempPath);
    }

    [PlatformSpecific(PlatformID.AnyUnix)]
    [Theory]
    [InlineData("/tmp/", "/tmp")]
    [InlineData("/tmp/", "/tmp/")]
    [InlineData("/", "/")]
    [InlineData("/var/tmp/", "/var/tmp")]
    [InlineData("/var/tmp/", "/var/tmp/")]
    [InlineData("~/", "~")]
    [InlineData("~/", "~/")]
    [InlineData(".tmp/", ".tmp")]
    [InlineData("./tmp/", "./tmp")]
    [InlineData("/home/someuser/sometempdir/", "/home/someuser/sometempdir/")]
    public static void GetTempPath_SetEnvVar_Unix(string expected, string newTempPath)
    {
        GetTempPath_SetEnvVar("TMPDIR", expected, newTempPath);
    }

    private static void GetTempPath_SetEnvVar(string envVar, string expected, string newTempPath)
    {
        string original = Path.GetTempPath();
        Assert.NotNull(original);
        try
        {
            Environment.SetEnvironmentVariable(envVar, newTempPath);
            Assert.Equal(
                Path.GetFullPath(expected), 
                Path.GetFullPath(Path.GetTempPath()));
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, null);
            Assert.Equal(original, Path.GetTempPath());
        }
    }

    [Fact]
    public static void GetTempFileName()
    {
        string tmpFile = Path.GetTempFileName();
        try
        {
            Assert.True(File.Exists(tmpFile));
            Assert.Equal(".tmp", Path.GetExtension(tmpFile), ignoreCase: true);
            using (FileStream fs = File.OpenRead(tmpFile))
                Assert.Equal(0, fs.Length);
            Assert.Equal(Path.Combine(Path.GetTempPath(), Path.GetFileName(tmpFile)), tmpFile);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public static void GetFullPath()
    {
        // Basic invalid arg checks
        Assert.Throws<ArgumentNullException>(() => Path.GetFullPath(null));
        Assert.Throws<ArgumentException>(() => Path.GetFullPath(""));
        Assert.Throws<ArgumentException>(() => Path.GetFullPath("http://www.microsoft.com"));
        Assert.Throws<ArgumentException>(() => Path.GetFullPath("file://www.microsoft.com"));

        // Basic expansions (e.g. self to self, period to self, normalization of lots of periods, etc.)
        string curDir = Directory.GetCurrentDirectory();
        Assert.Equal(curDir, Path.GetFullPath(curDir));
        Assert.Equal(curDir, Path.GetFullPath("."));
        Assert.Equal(curDir, Path.GetFullPath(Path.Combine(curDir, ".", ".", ".", ".", ".")));
        Assert.Equal(curDir, Path.GetFullPath(curDir + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + "."));
        Assert.Equal(curDir, Path.GetFullPath(Path.Combine(curDir, "..", Path.GetFileName(curDir), ".", "..", Path.GetFileName(curDir))));
        Assert.Equal(Path.GetPathRoot(curDir), Path.GetFullPath(Path.Combine(Path.GetPathRoot(curDir), "somedir", "..")));
        Assert.Equal(Path.GetPathRoot(curDir), Path.GetFullPath(Path.Combine(Path.GetPathRoot(curDir), ".")));

        // Try out a long path that normalizes down to less than MaxPath
        var longPath = new StringBuilder(curDir);
        for (int i = 0; i < 1000; i++)
            longPath.Append(Path.DirectorySeparatorChar).Append('.');
        Assert.Equal(curDir, Path.GetFullPath(longPath.ToString()));

        // Some Windows-only checks
        if (Interop.IsWindows) 
        {
            // Try out a long path that normalizes down to more than MaxPath
            for (int i = 0; i < 500; i++)
                longPath.Append(Path.DirectorySeparatorChar).Append('a').Append(Path.DirectorySeparatorChar).Append('.');
            Assert.Throws<PathTooLongException>(() => Path.GetFullPath(longPath.ToString()));

            // alternate data streams aren't supported
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath(@"C:\some\bad:path"));
            Assert.Throws<NotSupportedException>(() => Path.GetFullPath(@"bad:path"));

            // Some Windows-specific bad paths
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(Path.DirectorySeparatorChar + ".. ." + Path.DirectorySeparatorChar));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(Path.DirectorySeparatorChar + ". ." + Path.DirectorySeparatorChar));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(Path.DirectorySeparatorChar + " ." + Path.DirectorySeparatorChar));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath("C:..."));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"C:...\somedir"));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"C  :"));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"C  :\somedir"));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"bad::$DATA"));
            Assert.Throws<PathTooLongException>(() => Path.GetFullPath(@"C:\" + new string('a', 255) + @"\"));

            // Some Windows-specific strange but legal paths
            Assert.Equal(
                Path.GetFullPath(curDir + Path.DirectorySeparatorChar),
                Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ". " + Path.DirectorySeparatorChar));
            Assert.Equal(
                Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                Path.GetFullPath(curDir + Path.DirectorySeparatorChar + "..." + Path.DirectorySeparatorChar));
            Assert.Equal(
                Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ".. " + Path.DirectorySeparatorChar));

            // Windows-specific UNC paths
            Assert.Equal(@"\\server\share", Path.GetFullPath(@"\\server\share"));
            Assert.Equal(@"\\server\share", Path.GetFullPath(@" \\server\share"));
            Assert.Equal(@"\\server\share\dir", Path.GetFullPath(@"\\server\share\dir"));
            Assert.Equal(@"\\server\share", Path.GetFullPath(@"\\server\share\."));
            Assert.Equal(@"\\server\share", Path.GetFullPath(@"\\server\share\.."));
            Assert.Equal(@"\\server\share\", Path.GetFullPath(@"\\server\share\    "));
            Assert.Equal(@"\\server\  share\", Path.GetFullPath(@"\\server\  share\"));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"\\"));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"\\server"));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"\\server\"));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"\\server\.."));
            Assert.Throws<ArgumentException>(() => Path.GetFullPath(@"\\?\GLOBALROOT\"));

            // Windows short paths
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            File.Create(tempFilePath).Dispose();
            try
            {
                // Validate a short name can be expanded
                var sb = new StringBuilder(260);
                if (GetShortPathName(tempFilePath, sb, sb.Capacity) > 0) // only proceed if we could successfully create the short name
                {
                    Assert.Equal(tempFilePath, Path.GetFullPath(sb.ToString()));

                    // Validate case where short name doesn't expand to a real file
                    string invalidShortName = @"S:\DOESNT~1\USERNA~1.RED\LOCALS~1\Temp\bg3ylpzp";
                    Assert.Equal(invalidShortName, Path.GetFullPath(invalidShortName));

                    // Same thing, but with a long path that normalizes down to a short enough one
                    var shortLongName = new StringBuilder(invalidShortName);
                    for (int i = 0; i < 1000; i++)
                        shortLongName.Append(Path.DirectorySeparatorChar).Append('.');
                    Assert.Equal(invalidShortName, Path.GetFullPath(shortLongName.ToString()));
                }
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
    }

    // Windows-only P/Invoke to create 8.3 short names from long names
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);
}
