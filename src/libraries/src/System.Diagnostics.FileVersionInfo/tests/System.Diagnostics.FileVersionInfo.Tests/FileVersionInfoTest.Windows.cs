// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class FileVersionInfoTest
    {
        private const string NativeConsoleAppFileName = "NativeConsoleApp.exe";
        private const string NativeLibraryFileName = "NativeLibrary.dll";
        private const string SecondNativeLibraryFileName = "SecondNativeLibrary.dll";

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // native PE files only supported on Windows
        public void FileVersionInfo_Normal()
        {
            // NativeConsoleApp (English)
            VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), NativeConsoleAppFileName), new MyFVI()
            {
                Comments = "",
                CompanyName = "Microsoft Corporation",
                FileBuildPart = 3,
                FileDescription = "This is the description for the native console application.",
                FileMajorPart = 5,
                FileMinorPart = 4,
                FileName = Path.Combine(Directory.GetCurrentDirectory(), NativeConsoleAppFileName),
                FilePrivatePart = 2,
                FileVersion = "5.4.3.2",
                InternalName = NativeConsoleAppFileName,
                IsDebug = false,
                IsPatched = false,
                IsPrivateBuild = false,
                IsPreRelease = true,
                IsSpecialBuild = true,
                Language = GetFileVersionLanguage(0x0409), //English (United States)
                LegalCopyright = "Copyright (C) 2050",
                LegalTrademarks = "",
                OriginalFilename = NativeConsoleAppFileName,
                PrivateBuild = "",
                ProductBuildPart = 3,
                ProductMajorPart = 5,
                ProductMinorPart = 4,
                ProductName = Path.GetFileNameWithoutExtension(NativeConsoleAppFileName),
                ProductPrivatePart = 2,
                ProductVersion = "5.4.3.2",
                SpecialBuild = ""
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // native PE files only supported on Windows
        public void FileVersionInfo_Chinese()
        {
            // NativeLibrary.dll (Chinese)
            VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), NativeLibraryFileName), new MyFVI()
            {
                Comments = "",
                CompanyName = "A non-existent company",
                FileBuildPart = 3,
                FileDescription = "Here is the description of the native library.",
                FileMajorPart = 9,
                FileMinorPart = 9,
                FileName = Path.Combine(Directory.GetCurrentDirectory(), NativeLibraryFileName),
                FilePrivatePart = 3,
                FileVersion = "9.9.3.3",
                InternalName = "NativeLibrary.dll",
                IsDebug = false,
                IsPatched = true,
                IsPrivateBuild = false,
                IsPreRelease = true,
                IsSpecialBuild = false,
                Language = GetFileVersionLanguage(0x0004),//Chinese (Simplified)
                Language2 = GetFileVersionLanguage(0x0804),//Chinese (Simplified, PRC) - changed, but not yet on all platforms
                LegalCopyright = "None",
                LegalTrademarks = "",
                OriginalFilename = "NativeLibrary.dll",
                PrivateBuild = "",
                ProductBuildPart = 40,
                ProductMajorPart = 20,
                ProductMinorPart = 30,
                ProductName = "I was never given a name.",
                ProductPrivatePart = 50,
                ProductVersion = "20.30.40.50",
                SpecialBuild = "",
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // native PE files only supported on Windows
        public void FileVersionInfo_DifferentFileVersionAndProductVersion()
        {
            // Mtxex.dll
            VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), SecondNativeLibraryFileName), new MyFVI()
            {
                Comments = "",
                CompanyName = "",
                FileBuildPart = 0,
                FileDescription = "",
                FileMajorPart = 0,
                FileMinorPart = 65535,
                FileName = Path.Combine(Directory.GetCurrentDirectory(), SecondNativeLibraryFileName),
                FilePrivatePart = 2,
                FileVersion = "0.65535.0.2",
                InternalName = "SecondNativeLibrary.dll",
                IsDebug = false,
                IsPatched = false,
                IsPrivateBuild = false,
                IsPreRelease = false,
                IsSpecialBuild = false,
                Language = GetFileVersionLanguage(0x0400),//Process Default Language
                LegalCopyright = "Copyright (C) 1 - 2014",
                LegalTrademarks = "",
                OriginalFilename = "SecondNativeLibrary.dll",
                PrivateBuild = "",
                ProductBuildPart = 0,
                ProductMajorPart = 1,
                ProductMinorPart = 0,
                ProductName = "Unknown_Product_Name",
                ProductPrivatePart = 1,
                ProductVersion = "1.0.0.1",
                SpecialBuild = "",
            });
        }

        private static string GetFileVersionLanguage(uint langid)
        {
            var lang = new StringBuilder(256);
            Interop.Kernel32.VerLanguageName(langid, lang, (uint)lang.Capacity);
            return lang.ToString();
        }
    }
}
