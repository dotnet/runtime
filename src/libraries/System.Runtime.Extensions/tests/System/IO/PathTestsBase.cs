// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    public partial class PathTestsBase
    {
        protected static string Sep = Path.DirectorySeparatorChar.ToString();
        protected static string AltSep = Path.AltDirectorySeparatorChar.ToString();

        public static TheoryData<string> TestData_EmbeddedNull => new TheoryData<string>
        {
            "a\0b"
        };

        public static TheoryData<string> TestData_EmptyString => new TheoryData<string>
        {
            ""
        };

        public static TheoryData<string> TestData_ControlChars => new TheoryData<string>
        {
            "\t",
            "\r\n",
            "\b",
            "\v",
            "\n"
        };

        public static TheoryData<string> TestData_NonDriveColonPaths => new TheoryData<string>
        {
            @"bad:path",
            @"C:\some\bad:path",
            @"http://www.microsoft.com",
            @"file://www.microsoft.com",
            @"bad::$DATA",
            @"C  :",
            @"C  :\somedir"
        };

        public static TheoryData<string> TestData_Spaces => new TheoryData<string>
        {
            " ",
            "   "
        };

        public static TheoryData<string> TestData_Periods => new TheoryData<string>
        {
            // One and two periods have special meaning (current and parent dir)
            "...",
            "...."
        };

        public static TheoryData<string> TestData_Wildcards => new TheoryData<string>
        {
            "*",
            "?"
        };

        public static TheoryData<string> TestData_ExtendedWildcards => new TheoryData<string>
        {
            // These are supported by Windows although .NET blocked them historically
            "\"",
            "<",
            ">"
        };

        public static TheoryData<string> TestData_UnicodeWhiteSpace => new TheoryData<string>
        {
            "\u00A0", // Non-breaking Space
            "\u2028", // Line separator
            "\u2029", // Paragraph separator
        };

        public static TheoryData<string> TestData_InvalidUnc => new TheoryData<string>
        {
            // .NET used to validate properly formed UNCs
            @"\\",
            @"\\LOCALHOST",
            @"\\LOCALHOST\",
            @"\\LOCALHOST\\",
            @"\\LOCALHOST\.."
        };

        public static TheoryData<string> TestData_InvalidDriveLetters => new TheoryData<string>
        {
            { @"@:\foo" },  // 064 = @     065 = A
            { @"[:\\" },    // 091 = [     090 = Z
            { @"`:\foo "},  // 096 = `     097 = a
            { @"{:\\" },    // 123 = {     122 = z
            { @"@:/foo" },
            { @"[://" },
            { @"`:/foo "},
            { @"{:/" },
            { @"]:" }
        };

        public static TheoryData<string> TestData_ValidDriveLetters => new TheoryData<string>
        {
            { @"A:\foo" },  // 064 = @     065 = A
            { @"Z:\\" },    // 091 = [     090 = Z
            { @"a:\foo "},  // 096 = `     097 = a
            { @"z:\\" },    // 123 = {     122 = z
            { @"B:/foo" },
            { @"D://" },
            { @"E:/foo "},
            { @"F:/" },
            { @"G:" }
        };

        public static TheoryData<string, string> TestData_GetDirectoryName => new TheoryData<string, string>
        {
            { ".", "" },
            { "..", "" },
            { "baz", "" },
            { Path.Combine("dir", "baz"), "dir" },
            { "dir.foo" + Path.AltDirectorySeparatorChar + "baz.txt", "dir.foo" },
            { Path.Combine("dir", "baz", "bar"), Path.Combine("dir", "baz") },
            { Path.Combine("..", "..", "files.txt"), Path.Combine("..", "..") },
            { Path.DirectorySeparatorChar + "foo", Path.DirectorySeparatorChar.ToString() },
            { Path.DirectorySeparatorChar.ToString(), null }
        };

        public static TheoryData<string, string> TestData_GetDirectoryName_Windows => new TheoryData<string, string>
        {
            { @"C:\", null },
            { @"C:/", null },
            { @"C:", null },
            { @"dir\\baz", "dir" },
            { @"dir//baz", "dir" },
            { @"C:\foo", @"C:\" },
            { @"C:foo", "C:" }
        };

        public static TheoryData<string, string> TestData_GetExtension => new TheoryData<string, string>
        {
            { @"file.exe", ".exe" },
            { @"file", "" },
            { @"file.", "" },
            { @"file.s", ".s" },
            { @"test/file", "" },
            { @"test/file.extension", ".extension" },
            { @"test\file", "" },
            { @"test\file.extension", ".extension" },
            { "file.e xe", ".e xe"},
            { "file. ", ". "},
            { " file. ", ". "},
            { " file.extension", ".extension"}
        };

        public static TheoryData<string, string> TestData_GetFileName => new TheoryData<string, string>
        {
            { ".", "." },
            { "..", ".." },
            { "file", "file" },
            { "file.", "file." },
            { "file.exe", "file.exe" },
            { " . ", " . " },
            { " .. ", " .. " },
            { "fi le", "fi le" },
            { Path.Combine("baz", "file.exe"), "file.exe" },
            { Path.Combine("baz", "file.exe") + Path.AltDirectorySeparatorChar, "" },
            { Path.Combine("bar", "baz", "file.exe"), "file.exe" },
            { Path.Combine("bar", "baz", "file.exe") + Path.DirectorySeparatorChar, "" }
        };

        public static TheoryData<string, string> TestData_GetFileNameWithoutExtension => new TheoryData<string, string>
        {
            { "", "" },
            { "file", "file" },
            { "file.exe", "file" },
            { Path.Combine("bar", "baz", "file.exe"), "file" },
            { Path.Combine("bar", "baz") + Path.DirectorySeparatorChar, "" }
        };

        public static TheoryData<string, string> TestData_GetPathRoot_Unc => new TheoryData<string, string>
        {
            { @"\\test\unc\path\to\something", @"\\test\unc" },
            { @"\\a\b\c\d\e", @"\\a\b" },
            { @"\\a\b\", @"\\a\b" },
            { @"\\a\b", @"\\a\b" },
            { @"\\test\unc", @"\\test\unc" },
        };

        // TODO: Include \\.\ as well
        public static TheoryData<string, string> TestData_GetPathRoot_DevicePaths => new TheoryData<string, string>
        {
            { @"\\?\UNC\test\unc\path\to\something", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\test\unc" },
            { @"\\?\UNC\test\unc", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\test\unc" },
            { @"\\?\UNC\a\b1", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\a\b1" },
            { @"\\?\UNC\a\b2\", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\a\b2" },
            { @"\\?\C:\foo\bar.txt", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\C:" : @"\\?\C:\" }
        };

        public static TheoryData<string, string> TestData_GetPathRoot_Windows => new TheoryData<string, string>
        {
            { @"C:", @"C:" },
            { @"C:\", @"C:\" },
            { @"C:\\", @"C:\" },
            { @"C:\foo1", @"C:\" },
            { @"C:\\foo2", @"C:\" },
        };

        public static TheoryData<string,string> TestData_TrimEndingDirectorySeparator => new TheoryData<string, string>
        {
            { @"C:\folder\", @"C:\folder" },
            { @"C:/folder/", @"C:/folder" },
            { @"/folder/", @"/folder" },
            { @"\folder\", @"\folder" },
            { @"folder\", @"folder" },
            { @"folder/", @"folder" },
            { @"C:\", @"C:\" },
            { @"C:/", @"C:/" },
            { @"", @"" },
            { @"/", @"/" },
            { @"\", @"\" },
            { @"\\server\share\", @"\\server\share" },
            { @"\\server\share\folder\", @"\\server\share\folder" },
            { @"\\?\C:\", @"\\?\C:\" },
            { @"\\?\C:\folder\", @"\\?\C:\folder" },
            { @"\\?\UNC\", @"\\?\UNC\" },
            { @"\\?\UNC\a\", @"\\?\UNC\a\" },
            { @"\\?\UNC\a\folder\", @"\\?\UNC\a\folder" },
            { null, null }
        };

        public static TheoryData<string, bool> TestData_EndsInDirectorySeparator => new TheoryData<string, bool>
        {
            { @"\", true },
            { @"/", true },
            { @"C:\folder\", true },
            { @"C:/folder/", true },
            { @"C:\", true },
            { @"C:/", true },
            { @"\\", true },
            { @"//", true },
            { @"\\server\share\", true },
            { @"\\?\UNC\a\", true },
            { @"\\?\C:\", true },
            { @"\\?\UNC\", true },
            { @"folder\", true },
            { @"folder", false },
            { @"", false },
            { null, false }
        };

        protected static void GetTempPath_SetEnvVar(string envVar, string expected, string newTempPath)
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
                Environment.SetEnvironmentVariable(envVar, original);
                Assert.Equal(original, Path.GetTempPath());
            }
        }
    }
}
