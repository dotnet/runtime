// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.IO.Compression.Tests
{
    public class ZipArchiveEntry_ExtractToDirectory : ZipFileTestBase
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new string[] { "AbsoluteNoRelative.zip", "System.IO.IOException" };
            yield return new string[] { "AbsoluteWithRelativeOut.zip", "System.IO.IOException" };
            yield return new string[] { "DotMarkDevicePath.zip", @"System.IO.IOException" };
            yield return new string[] { "QuestionMarkDevicePath.zip", @"System.IO.IOException" };
            yield return new string[] { "RelativeFirst.zip", @"System.IO.IOException" };
            yield return new string[] { "ValidNestedRelative.zip", @"./foo/../sample.txt" };
            yield return new string[] { "NestedDotDevicePath.zip", @"System.IO.IOException" };
            yield return new string[] { "NestedQuestionDevicePath.zip", @"System.IO.IOException" };
            yield return new string[] { "RelativeAtStartAndEnd.zip", @"System.IO.IOException" };
            yield return new string[] { "JustDotdot.zip", @"System.IO.IOException" };
            yield return new string[] { "TrailingDotDot.zip", @"System.IO.IOException" };
            yield return new string[] { "MarkedDotDot.zip", @"System.IO.IOException" };
            yield return new string[] { "JustSlash.zip", @"System.IO.IOException" };
            yield return new string[] { "DotSlashDirectory.zip", @"./sample.txt" };
            yield return new string[] { "DoubleSlash.zip", @"foo//sample.txt" };
            yield return new string[] { "CurrentInsidePath.zip", @"parent/./child" };
            yield return new string[] { "Justdot.zip", @"System.IO.IOException" };
            yield return new string[] { "SlashDotStuff.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew1.zip", @"s/../stuff" };
            yield return new string[] { "TestNew2.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew3.zip", @"s/..\t\../j/try" };
            yield return new string[] { "TestNew4.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew5.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew6.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew7.zip", @"t/h/i/s/../../../../script" };
            yield return new string[] { "TestNew8.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew9.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew10.zip", @"System.IO.IOException" };
            yield return new string[] { "TestNew11.zip", @"./././././././5" };
            yield return new string[] { "InvalidChar1.zip", "System.ArgumentException" };
            yield return new string[] { "InvalidChar2.zip", @"sample.:xt" };
            yield return new string[] { "InvalidChar3.zip", "System.ArgumentException" };
            yield return new string[] { "EncodedDotDots.zip", "lib/%2E%2E/%2E%2E/%2E%2E/bad.exe" };
            yield return new string[] { "SpacedRelative.zip", "System.IO.DirectoryNotFoundException" };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void Entry_ExtractToDirectory(string zipFile, string expectedOutPath)
        {
            using (TempDirectory destinationDirectory = new TempDirectory())
            {
                try
                {
                    using (ZipArchive archive = ZipFile.OpenRead(zipFile))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            entry.ExtractRelativeToDirectory(destinationDirectory.Path, false);
                            Assert.True(File.Exists(Path.Combine(destinationDirectory.Path, expectedOutPath)));
                        }
                    }
                }
                catch (Exception e)
                {
                    Assert.Equal(expectedOutPath, e.GetType().ToString());
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void Archive_ExtractToDirectory(string zipFile, string expectedOutPath)
        {
            using (TempDirectory destinationDirectory = new TempDirectory())
            {
                try
                {
                    ZipFile.ExtractToDirectory(zipFile, destinationDirectory.Path);
                    Assert.True(File.Exists(Path.Combine(destinationDirectory.Path, expectedOutPath)));
                }
                catch (Exception e)
                {
                    Assert.Equal(expectedOutPath, e.GetType().ToString());
                }
            }
        }
    }
}

