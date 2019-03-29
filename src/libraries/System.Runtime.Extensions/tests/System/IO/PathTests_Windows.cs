// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public partial class PathTests_Windows : PathTestsBase
    {
        public void GetDirectoryName_DevicePath()
        {
            if (PathFeatures.IsUsingLegacyPathNormalization())
            {
                Assert.Equal(@"\\?\C:", Path.GetDirectoryName(@"\\?\C:\foo"));
            }
            else
            {
                Assert.Equal(@"\\?\C:\", Path.GetDirectoryName(@"\\?\C:\foo"));
            }
        }

        [Theory, MemberData(nameof(TestData_GetDirectoryName_Windows))]
        public void GetDirectoryName(string path, string expected)
        {
            Assert.Equal(expected, Path.GetDirectoryName(path));
        }

        [Theory,
            InlineData("B:", ""),
            InlineData("A:.", ".")]
        public static void GetFileName_Volume(string path, string expected)
        {
            // With a valid drive letter followed by a colon, we have a root, but only on Windows.
            Assert.Equal(expected, Path.GetFileName(path));
        }

        [ActiveIssue(27552)]
        [Theory,
            MemberData(nameof(TestData_GetPathRoot_Windows)),
            MemberData(nameof(TestData_GetPathRoot_Unc)),
            MemberData(nameof(TestData_GetPathRoot_DevicePaths))]
        public void GetPathRoot_Windows(string value, string expected)
        {
            Assert.Equal(expected, Path.GetPathRoot(value));

            if (value.Length != expected.Length)
            {
                // The string overload normalizes the separators
                Assert.Equal(expected, Path.GetPathRoot(value.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

                // UNCs and device paths will have their semantics changed if we double up separators
                if (!value.StartsWith(@"\\"))
                    Assert.Equal(expected, Path.GetPathRoot(value.Replace(@"\", @"\\")));
            }
        }

        public static IEnumerable<string[]> GetTempPath_SetEnvVar_Data()
        {
            yield return new string[] { @"C:\Users\someuser\AppData\Local\Temp\", @"C:\Users\someuser\AppData\Local\Temp" };
            yield return new string[] { @"C:\Users\someuser\AppData\Local\Temp\", @"C:\Users\someuser\AppData\Local\Temp\" };
            yield return new string[] { @"C:\", @"C:\" };
            yield return new string[] { @"C:\tmp\", @"C:\tmp" };
            yield return new string[] { @"C:\tmp\", @"C:\tmp\" };
        }

        [Fact]
        public void GetTempPath_SetEnvVar()
        {
            RemoteExecutor.Invoke(() => 
            {
                foreach (string[] tempPath in GetTempPath_SetEnvVar_Data())
                {
                    GetTempPath_SetEnvVar("TMP", tempPath[0], tempPath[1]);
                }
            }).Dispose();
        }

        [Theory, MemberData(nameof(TestData_Spaces))]
        public void GetFullPath_TrailingSpacesCut(string component)
        {
            // Windows cuts off any simple white space added to a path
            string path = "C:\\Test" + component;
            Assert.Equal("C:\\Test", Path.GetFullPath(path));
        }

        [Fact]
        public void GetFullPath_NormalizedLongPathTooLong()
        {
            // Try out a long path that normalizes down to more than MaxPath
            string curDir = Directory.GetCurrentDirectory();
            const int Iters = 260;
            var longPath = new StringBuilder(curDir, curDir.Length + (Iters * 4));
            for (int i = 0; i < Iters; i++)
            {
                longPath.Append(Path.DirectorySeparatorChar).Append('a').Append(Path.DirectorySeparatorChar).Append('.');
            }

            if (PathFeatures.AreAllLongPathsAvailable())
            {
                // Now no longer throws unless over ~32K
                Assert.NotNull(Path.GetFullPath(longPath.ToString()));
            }
            else
            {
                Assert.Throws<PathTooLongException>(() => Path.GetFullPath(longPath.ToString()));
            }
        }

        [Theory,
            InlineData(@"C:..."),
            InlineData(@"C:...\somedir"),
            InlineData(@"\.. .\"),
            InlineData(@"\. .\"),
            InlineData(@"\ .\")]
        public void GetFullPath_LegacyArgumentExceptionPaths(string path)
        {
            if (PathFeatures.IsUsingLegacyPathNormalization())
            {
                // We didn't allow these paths on < 4.6.2
                AssertExtensions.Throws<ArgumentException>(null, () => Path.GetFullPath(path));
            }
            else
            {
                // These paths are legitimate Windows paths that can be created without extended syntax.
                // We now allow them through.
                Path.GetFullPath(path);
            }
        }

        [Fact]
        public void GetFullPath_MaxPathNotTooLong()
        {
            string value = @"C:\" + new string('a', 255) + @"\";
            if (PathFeatures.AreAllLongPathsAvailable())
            {
                // Shouldn't throw anymore
                Path.GetFullPath(value);
            }
            else
            {
                Assert.Throws<PathTooLongException>(() => Path.GetFullPath(value));
            }
        }

        [Fact]
        public void GetFullPath_PathTooLong()
        {
            Assert.Throws<PathTooLongException>(() => Path.GetFullPath(@"C:\" + new string('a', short.MaxValue) + @"\"));
        }

        [Theory,
            InlineData(@"C:\", @"C:\"),
            InlineData(@"C:\.", @"C:\"),
            InlineData(@"C:\..", @"C:\"),
            InlineData(@"C:\..\..", @"C:\"),
            InlineData(@"C:\A\..", @"C:\"),
            InlineData(@"C:\..\..\A\..", @"C:\")]
        public void GetFullPath_RelativeRoot(string path, string expected)
        {
            Assert.Equal(Path.GetFullPath(path), expected);
        }

        [Fact]
        public void GetFullPath_StrangeButLegalPaths()
        {
            // These are legal and creatable without using extended syntax if you use a trailing slash
            // (such as "md ...\"). We used to filter these out, but now allow them to prevent apps from
            // being blocked when they hit these paths.
            string curDir = Directory.GetCurrentDirectory();
            if (PathFeatures.IsUsingLegacyPathNormalization())
            {
                // Legacy path Path.GetFullePath() ignores . when there is less or more that two, when there is .. in the path it returns one directory up.
                Assert.Equal(
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ". " + Path.DirectorySeparatorChar));
                Assert.Equal(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + "..." + Path.DirectorySeparatorChar));
                Assert.Equal(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ".. " + Path.DirectorySeparatorChar));
            }
            else
            {
                Assert.NotEqual(
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ". " + Path.DirectorySeparatorChar));
                Assert.NotEqual(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + "..." + Path.DirectorySeparatorChar));
                Assert.NotEqual(
                    Path.GetFullPath(Path.GetDirectoryName(curDir) + Path.DirectorySeparatorChar),
                    Path.GetFullPath(curDir + Path.DirectorySeparatorChar + ".. " + Path.DirectorySeparatorChar));
            }
        }

        [Theory,
            InlineData(@"\\?\C:\ "),
            InlineData(@"\\?\C:\ \ "),
            InlineData(@"\\?\C:\ ."),
            InlineData(@"\\?\C:\ .."),
            InlineData(@"\\?\C:\..."),
            InlineData(@"\\?\GLOBALROOT\"),
            InlineData(@"\\?\"),
            InlineData(@"\\?\."),
            InlineData(@"\\?\.."),
            InlineData(@"\\?\\"),
            InlineData(@"\\?\C:\\"),
            InlineData(@"\\?\C:\|"),
            InlineData(@"\\?\C:\."),
            InlineData(@"\\?\C:\.."),
            InlineData(@"\\?\C:\Foo1\."),
            InlineData(@"\\?\C:\Foo2\.."),
            InlineData(@"\\?\UNC\"),
            InlineData(@"\\?\UNC\server1"),
            InlineData(@"\\?\UNC\server2\"),
            InlineData(@"\\?\UNC\server3\\"),
            InlineData(@"\\?\UNC\server4\.."),
            InlineData(@"\\?\UNC\server5\share\."),
            InlineData(@"\\?\UNC\server6\share\.."),
            InlineData(@"\\?\UNC\a\b\\"),
            InlineData(@"\\.\"),
            InlineData(@"\\.\."),
            InlineData(@"\\.\.."),
            InlineData(@"\\.\\"),
            InlineData(@"\\.\C:\\"),
            InlineData(@"\\.\C:\|"),
            InlineData(@"\\.\C:\."),
            InlineData(@"\\.\C:\.."),
            InlineData(@"\\.\C:\Foo1\."),
            InlineData(@"\\.\C:\Foo2\..")]
        public void GetFullPath_ValidExtendedPaths(string path)
        {
            if (PathFeatures.IsUsingLegacyPathNormalization())
            {
                // Legacy Path doesn't support any of these paths.
                AssertExtensions.ThrowsAny<ArgumentException, NotSupportedException>(() => Path.GetFullPath(path));
                return;
            }

            // None of these should throw
            if (path.StartsWith(@"\\?\"))
            {
                Assert.Equal(path, Path.GetFullPath(path));
            }
            else
            {
                Path.GetFullPath(path);
            }
        }

        [Theory,
            InlineData(@"\\.\UNC\"),
            InlineData(@"\\.\UNC\LOCALHOST"),
            InlineData(@"\\.\UNC\localHOST\"),
            InlineData(@"\\.\UNC\LOcaLHOST\\"),
            InlineData(@"\\.\UNC\lOCALHOST\.."),
            InlineData(@"\\.\UNC\LOCALhost\share\."),
            InlineData(@"\\.\UNC\loCALHOST\share\.."),
            InlineData(@"\\.\UNC\a\b\\")]
        public static void GetFullPath_ValidLegacy_ValidExtendedPaths(string path)
        {
            // should not throw
            Path.GetFullPath(path);
        }

        [Theory,
            // https://github.com/dotnet/corefx/issues/11965
            InlineData(@"\\LOCALHOST\share\test.txt.~SS", @"\\LOCALHOST\share\test.txt.~SS"),
            InlineData(@"\\LOCALHOST\share1", @"\\LOCALHOST\share1"),
            InlineData(@"\\LOCALHOST\share3\dir", @"\\LOCALHOST\share3\dir"),
            InlineData(@"\\LOCALHOST\share4\.", @"\\LOCALHOST\share4"),
            InlineData(@"\\LOCALHOST\share5\..", @"\\LOCALHOST\share5"),
            InlineData(@"\\LOCALHOST\share6\    ", @"\\LOCALHOST\share6\"),
            InlineData(@"\\LOCALHOST\  share7\", @"\\LOCALHOST\  share7\"),
            InlineData(@"\\?\UNC\LOCALHOST\share8\test.txt.~SS", @"\\?\UNC\LOCALHOST\share8\test.txt.~SS"),
            InlineData(@"\\?\UNC\LOCALHOST\share9", @"\\?\UNC\LOCALHOST\share9"),
            InlineData(@"\\?\UNC\LOCALHOST\shareA\dir", @"\\?\UNC\LOCALHOST\shareA\dir"),
            InlineData(@"\\?\UNC\LOCALHOST\shareB\. ", @"\\?\UNC\LOCALHOST\shareB\. "),
            InlineData(@"\\?\UNC\LOCALHOST\shareC\.. ", @"\\?\UNC\LOCALHOST\shareC\.. "),
            InlineData(@"\\?\UNC\LOCALHOST\shareD\    ", @"\\?\UNC\LOCALHOST\shareD\    "),
            InlineData(@"\\.\UNC\LOCALHOST\  shareE\", @"\\.\UNC\LOCALHOST\  shareE\"),
            InlineData(@"\\.\UNC\LOCALHOST\shareF\test.txt.~SS", @"\\.\UNC\LOCALHOST\shareF\test.txt.~SS"),
            InlineData(@"\\.\UNC\LOCALHOST\shareG", @"\\.\UNC\LOCALHOST\shareG"),
            InlineData(@"\\.\UNC\LOCALHOST\shareH\dir", @"\\.\UNC\LOCALHOST\shareH\dir"),
            InlineData(@"\\.\UNC\LOCALHOST\shareK\    ", @"\\.\UNC\LOCALHOST\shareK\"),
            InlineData(@"\\.\UNC\LOCALHOST\  shareL\", @"\\.\UNC\LOCALHOST\  shareL\")]
        public void GetFullPath_UNC_Valid(string path, string expected)
        {
            if (path.StartsWith(@"\\?\") && PathFeatures.IsUsingLegacyPathNormalization())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Path.GetFullPath(path));
            }
            else
            {
                Assert.Equal(expected, Path.GetFullPath(path));
            }
        }

        [Theory,
            InlineData(@"\\.\UNC\LOCALHOST\shareI\. ", @"\\.\UNC\LOCALHOST\shareI\", @"\\.\UNC\LOCALHOST\shareI"),
            InlineData(@"\\.\UNC\LOCALHOST\shareJ\.. ", @"\\.\UNC\LOCALHOST\shareJ\", @"\\.\UNC\LOCALHOST")]
        public static void GetFullPath_Windows_UNC_Valid_LegacyPathSupport(string path, string normalExpected, string legacyExpected)
        {
            string expected = PathFeatures.IsUsingLegacyPathNormalization() ? legacyExpected : normalExpected;
            Assert.Equal(expected, Path.GetFullPath(path));
        }

        [Fact]
        public static void GetFullPath_Windows_83Paths()
        {
            // Create a temporary file name with a name longer than 8.3 such that it'll need to be shortened.
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            File.Create(tempFilePath).Dispose();
            try
            {
                // Get its short name
                var sb = new StringBuilder(260);
                if (GetShortPathName(tempFilePath, sb, sb.Capacity) > 0) // only proceed if we could successfully create the short name
                {
                    string shortName = sb.ToString();

                    // Make sure the shortened name expands back to the original one
                    // Sometimes shortening or GetFullPath is changing the casing of "temp" on some test machines: normalize both sides
                    tempFilePath = Regex.Replace(tempFilePath, @"\\temp\\", @"\TEMP\", RegexOptions.IgnoreCase);
                    shortName = Regex.Replace(Path.GetFullPath(shortName), @"\\temp\\", @"\TEMP\", RegexOptions.IgnoreCase);
                    Assert.Equal(tempFilePath, shortName);

                    // Should work with device paths that aren't well-formed extended syntax
                    if (!PathFeatures.IsUsingLegacyPathNormalization())
                    {
                        Assert.Equal(@"\\.\" + tempFilePath, Path.GetFullPath(@"\\.\" + shortName));
                        Assert.Equal(@"\\?\" + tempFilePath, Path.GetFullPath(@"//?/" + shortName));

                        // Shouldn't mess with well-formed extended syntax
                        Assert.Equal(@"\\?\" + shortName, Path.GetFullPath(@"\\?\" + shortName));
                    }

                    // Validate case where short name doesn't expand to a real file
                    string invalidShortName = @"S:\DOESNT~1\USERNA~1.RED\LOCALS~1\Temp\bg3ylpzp";
                    Assert.Equal(invalidShortName, Path.GetFullPath(invalidShortName));

                    // Same thing, but with a long path that normalizes down to a short enough one
                    const int Iters = 1000;
                    var shortLongName = new StringBuilder(invalidShortName, invalidShortName.Length + (Iters * 2));
                    for (int i = 0; i < Iters; i++)
                    {
                        shortLongName.Append(Path.DirectorySeparatorChar).Append('.');
                    }
                    Assert.Equal(invalidShortName, Path.GetFullPath(shortLongName.ToString()));
                }
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        // Windows-only P/Invoke to create 8.3 short names from long names
        [DllImport("kernel32.dll", EntryPoint = "GetShortPathNameW", CharSet = CharSet.Unicode)]
        private static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);
    }
}
