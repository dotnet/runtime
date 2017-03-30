// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class FileVersionInfoTest
    {
        private const string NativeConsoleAppFileName = "NativeConsoleApp.exe";
        private const string NativeLibraryFileName = "NativeLibrary.dll";
        private const string SecondNativeLibraryFileName = "SecondNativeLibrary.dll";
        private const string TestAssemblyFileName = "System.Diagnostics.FileVersionInfo.TestAssembly.dll";
        private const string TestCsFileName = "Assembly1.cs";
        private const string TestNotFoundFileName = "notfound.dll";

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
                InternalName = "NativeLi.dll",
                IsDebug = false,
                IsPatched = true,
                IsPrivateBuild = false,
                IsPreRelease = true,
                IsSpecialBuild = false,
                Language = GetFileVersionLanguage(0x0004),//Chinese (Simplified)
                Language2 = GetFileVersionLanguage(0x0804),//Chinese (Simplified, PRC) - changed, but not yet on all platforms
                LegalCopyright = "None",
                LegalTrademarks = "",
                OriginalFilename = "NativeLi.dll",
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
                InternalName = "SecondNa.dll",
                IsDebug = false,
                IsPatched = false,
                IsPrivateBuild = false,
                IsPreRelease = false,
                IsSpecialBuild = false,
                Language = GetFileVersionLanguage(0x0400),//Process Default Language
                LegalCopyright = "Copyright (C) 1 - 2014",
                LegalTrademarks = "",
                OriginalFilename = "SecondNa.dll",
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

        [Fact]
        public void FileVersionInfo_CustomManagedAssembly()
        {
            // Assembly1.dll
            VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), TestAssemblyFileName), new MyFVI()
            {
                Comments = "Have you played a Contoso amusement device today?",
                CompanyName = "The name of the company.",
                FileBuildPart = 2,
                FileDescription = "My File",
                FileMajorPart = 4,
                FileMinorPart = 3,
                FileName = Path.Combine(Directory.GetCurrentDirectory(), TestAssemblyFileName),
                FilePrivatePart = 1,
                FileVersion = "4.3.2.1",
                InternalName = TestAssemblyFileName,
                IsDebug = false,
                IsPatched = false,
                IsPrivateBuild = false,
                IsPreRelease = false,
                IsSpecialBuild = false,
                Language = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? GetFileVersionLanguage(0x0000) : "Language Neutral",
                LegalCopyright = "Copyright, you betcha!",
                LegalTrademarks = "TM",
                OriginalFilename = TestAssemblyFileName,
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

        [Fact]
        public void FileVersionInfo_EmptyFVI()
        {
            // Assembly1.cs
            VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), TestCsFileName), new MyFVI()
            {
                Comments = null,
                CompanyName = null,
                FileBuildPart = 0,
                FileDescription = null,
                FileMajorPart = 0,
                FileMinorPart = 0,
                FileName = Path.Combine(Directory.GetCurrentDirectory(), TestCsFileName),
                FilePrivatePart = 0,
                FileVersion = null,
                InternalName = null,
                IsDebug = false,
                IsPatched = false,
                IsPrivateBuild = false,
                IsPreRelease = false,
                IsSpecialBuild = false,
                Language = null,
                LegalCopyright = null,
                LegalTrademarks = null,
                OriginalFilename = null,
                PrivateBuild = null,
                ProductBuildPart = 0,
                ProductMajorPart = 0,
                ProductMinorPart = 0,
                ProductName = null,
                ProductPrivatePart = 0,
                ProductVersion = null,
                SpecialBuild = null,
            });
        }

        [Fact]
        public void FileVersionInfo_FileNotFound()
        {
            Assert.Throws<FileNotFoundException>(() =>
                FileVersionInfo.GetVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), TestNotFoundFileName)));
        }

        // Additional Tests Wanted:
        // [] File exists but we don't have permission to read it
        // [] DLL has unknown codepage info
        // [] DLL language/codepage is 8-hex-digits (locale > 0x999) (different codepath)

        private void VerifyVersionInfo(String filePath, MyFVI expected)
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);
            Assert.Equal(expected.Comments, fvi.Comments);
            Assert.Equal(expected.CompanyName, fvi.CompanyName);
            Assert.Equal(expected.FileBuildPart, fvi.FileBuildPart);
            Assert.Equal(expected.FileDescription, fvi.FileDescription);
            Assert.Equal(expected.FileMajorPart, fvi.FileMajorPart);
            Assert.Equal(expected.FileMinorPart, fvi.FileMinorPart);
            Assert.Equal(expected.FileName, fvi.FileName);
            Assert.Equal(expected.FilePrivatePart, fvi.FilePrivatePart);
            Assert.Equal(expected.FileVersion, fvi.FileVersion);
            Assert.Equal(expected.InternalName, fvi.InternalName);
            Assert.Equal(expected.IsDebug, fvi.IsDebug);
            Assert.Equal(expected.IsPatched, fvi.IsPatched);
            Assert.Equal(expected.IsPrivateBuild, fvi.IsPrivateBuild);
            Assert.Equal(expected.IsPreRelease, fvi.IsPreRelease);
            Assert.Equal(expected.IsSpecialBuild, fvi.IsSpecialBuild);
            Assert.Contains(fvi.Language, new[] { expected.Language, expected.Language2 });
            Assert.Equal(expected.LegalCopyright, fvi.LegalCopyright);
            Assert.Equal(expected.LegalTrademarks, fvi.LegalTrademarks);
            Assert.Equal(expected.OriginalFilename, fvi.OriginalFilename);
            Assert.Equal(expected.PrivateBuild, fvi.PrivateBuild);
            Assert.Equal(expected.ProductBuildPart, fvi.ProductBuildPart);
            Assert.Equal(expected.ProductMajorPart, fvi.ProductMajorPart);
            Assert.Equal(expected.ProductMinorPart, fvi.ProductMinorPart);
            Assert.Equal(expected.ProductName, fvi.ProductName);
            Assert.Equal(expected.ProductPrivatePart, fvi.ProductPrivatePart);
            Assert.Equal(expected.ProductVersion, fvi.ProductVersion);
            Assert.Equal(expected.SpecialBuild, fvi.SpecialBuild);

            //ToString
            String nl = Environment.NewLine;
            Assert.Equal("File:             " + fvi.FileName + nl +
                      "InternalName:     " + fvi.InternalName + nl +
                      "OriginalFilename: " + fvi.OriginalFilename + nl +
                      "FileVersion:      " + fvi.FileVersion + nl +
                      "FileDescription:  " + fvi.FileDescription + nl +
                      "Product:          " + fvi.ProductName + nl +
                      "ProductVersion:   " + fvi.ProductVersion + nl +
                      "Debug:            " + fvi.IsDebug.ToString() + nl +
                      "Patched:          " + fvi.IsPatched.ToString() + nl +
                      "PreRelease:       " + fvi.IsPreRelease.ToString() + nl +
                      "PrivateBuild:     " + fvi.IsPrivateBuild.ToString() + nl +
                      "SpecialBuild:     " + fvi.IsSpecialBuild.ToString() + nl +
                      "Language:         " + fvi.Language + nl,
                        fvi.ToString());
        }

        internal class MyFVI
        {
            public string Comments;
            public string CompanyName;
            public int FileBuildPart;
            public string FileDescription;
            public int FileMajorPart;
            public int FileMinorPart;
            public string FileName;
            public int FilePrivatePart;
            public string FileVersion;
            public string InternalName;
            public bool IsDebug;
            public bool IsPatched;
            public bool IsPrivateBuild;
            public bool IsPreRelease;
            public bool IsSpecialBuild;
            public string Language;
            public string Language2;
            public string LegalCopyright;
            public string LegalTrademarks;
            public string OriginalFilename;
            public string PrivateBuild;
            public int ProductBuildPart;
            public int ProductMajorPart;
            public int ProductMinorPart;
            public string ProductName;
            public int ProductPrivatePart;
            public string ProductVersion;
            public string SpecialBuild;
        }

        static string GetUnicodeString(String str)
        {
            if (str == null)
                return "<null>";

            StringBuilder buffer = new StringBuilder();
            buffer.Append("\"");
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                if (ch == '\r')
                {
                    buffer.Append("\\r");
                }
                else if (ch == '\n')
                {
                    buffer.Append("\\n");
                }
                else if (ch == '\\')
                {
                    buffer.Append("\\");
                }
                else if (ch == '\"')
                {
                    buffer.Append("\\\"");
                }
                else if (ch == '\'')
                {
                    buffer.Append("\\\'");
                }
                else if (ch < 0x20 || ch >= 0x7f)
                {
                    buffer.Append("\\u");
                    buffer.Append(((int)ch).ToString("x4"));
                }
                else
                {
                    buffer.Append(ch);
                }
            }
            buffer.Append("\"");
            return (buffer.ToString());
        }

        private static string GetFileVersionLanguage(uint langid)
        {
            var lang = new StringBuilder(256);
            Interop.Kernel32.VerLanguageName(langid, lang, (uint)lang.Capacity);
            return lang.ToString();
        }
    }
}
