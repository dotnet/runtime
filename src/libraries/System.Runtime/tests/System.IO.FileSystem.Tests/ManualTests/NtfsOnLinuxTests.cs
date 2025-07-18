// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.ManualTests
{
    public class NtfsOnLinuxTests : IClassFixture<NtfsOnLinuxSetup>
    {
        internal static bool IsManualTestsEnabledAndElevated => FileSystemManualTests.ManualTestsEnabled && AdminHelpers.IsProcessElevated();

        [ConditionalTheory(nameof(IsManualTestsEnabledAndElevated))]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData("Ω", 255)]
        [InlineData("あ", 255)]
        [InlineData("😀", 127)]
        public void NtfsOnLinux_FilenamesLongerThan255Bytes_FileEnumerationSucceeds(string codePoint, int maxAllowedLength)
        {
            string filename = string.Concat(Enumerable.Repeat(codePoint, maxAllowedLength));
            Assert.True(Encoding.UTF8.GetByteCount(filename) > 255);

            string filePath = $"/mnt/ntfs/{filename}";
            File.Create(filePath).Dispose();
            Assert.Contains(filePath, Directory.GetFiles("/mnt/ntfs"));
        }
    }
}
