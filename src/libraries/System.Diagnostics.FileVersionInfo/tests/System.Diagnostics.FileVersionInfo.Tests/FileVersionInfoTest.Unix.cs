// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class FileVersionInfoTest
    {
        [PlatformSpecific(TestPlatforms.AnyUnix & ~(TestPlatforms.iOS | TestPlatforms.tvOS))]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks mkfifo")]
        [Fact]
        public void NonRegularFile_Throws()
        {
            string pipePath = GetTestFilePath();
            Assert.Equal(0, mkfifo(pipePath, 0));
            Assert.Throws<FileNotFoundException>(() => FileVersionInfo.GetVersionInfo(pipePath));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSymLinkSupported))]
        public void Symlink_ValidFile_Succeeds()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), TestAssemblyFileName);
            string linkPath = GetRandomLinkPath();

            Assert.Equal(0, symlink(filePath, linkPath));

            // Assembly1.dll
            VerifyVersionInfo(linkPath, new MyFVI()
            {
                Comments = "Have you played a Contoso amusement device today?",
                CompanyName = "The name of the company.",
                FileBuildPart = 2,
                FileDescription = "My File",
                FileMajorPart = 4,
                FileMinorPart = 3,
                FileName = linkPath,
                FilePrivatePart = 1,
                FileVersion = "4.3.2.1",
                InternalName = OriginalTestAssemblyInternalName,
                IsDebug = false,
                IsPatched = false,
                IsPrivateBuild = false,
                IsPreRelease = false,
                IsSpecialBuild = false,
                Language = GetFileVersionLanguage(0x0000),
                Language2 = null,
                LegalCopyright = "Copyright, you betcha!",
                LegalTrademarks = "TM",
                OriginalFilename = OriginalTestAssemblyInternalName,
                PrivateBuild = "",
                ProductBuildPart = 3,
                ProductMajorPart = 1,
                ProductMinorPart = 2,
                ProductName = "The greatest product EVER",
                ProductPrivatePart = 0,
                ProductVersion = "1.2.3-beta.4",
                SpecialBuild = "",
            });
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSymLinkSupported))]
        public void Symlink_InvalidFile_Throws()
        {
            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), TestAssemblyFileName);
            string filePath = GetTestFilePath();
            File.Copy(sourcePath, filePath);
            string linkPath = GetRandomLinkPath();
            Assert.Equal(0, symlink(filePath, linkPath));
            File.Delete(filePath);
            Assert.Throws<FileNotFoundException>(() => FileVersionInfo.GetVersionInfo(linkPath));
        }

        private static string GetFileVersionLanguage(uint langid) => "Language Neutral";

        [DllImport("libc", SetLastError = true)]
        private static extern int mkfifo(string path, int mode);

        [DllImport("libc", SetLastError = true)]
        private static extern int symlink(string target, string linkpath);
    }
}
