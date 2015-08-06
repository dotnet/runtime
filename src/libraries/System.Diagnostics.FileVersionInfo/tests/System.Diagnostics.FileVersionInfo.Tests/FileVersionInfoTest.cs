// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public class FileVersionInfoTest
{
    private const string NativeConsoleAppFileName = "NativeConsoleApp.exe";
    private const string NativeLibraryFileName = "NativeLibrary.dll";
    private const string SecondNativeLibraryFileName = "SecondNativeLibrary.dll";
    private const string TestAssemblyFileName = "System.Diagnostics.FileVersionInfo.TestAssembly.dll";
    private const string TestCsFileName = "Assembly1.cs";
    private const string TestNotFoundFileName = "notfound.dll";

    [Fact]
    [PlatformSpecific(PlatformID.Windows)] // native PE files only supported on Windows
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
    [PlatformSpecific(PlatformID.Windows)] // native PE files only supported on Windows
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
    [PlatformSpecific(PlatformID.Windows)] // native PE files only supported on Windows
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
            ProductName = "Unkown_Product_Name",
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
            ProductBuildPart = 2,
            ProductMajorPart = 4,
            ProductMinorPart = 3,
            ProductName = "The greatest product EVER",
            ProductPrivatePart = 1,
            ProductVersion = "4.3.2.1",
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
        TestStringProperty("Comments", fvi.Comments, expected.Comments);
        TestStringProperty("CompanyName", fvi.CompanyName, expected.CompanyName);
        TestProperty<int>("FileBuildPart", fvi.FileBuildPart, expected.FileBuildPart);
        TestStringProperty("FileDescription", fvi.FileDescription, expected.FileDescription);
        TestProperty<int>("FileMajorPart", fvi.FileMajorPart, expected.FileMajorPart);
        TestProperty<int>("FileMinorPart", fvi.FileMinorPart, expected.FileMinorPart);
        TestStringProperty("FileName", fvi.FileName, expected.FileName);
        TestProperty<int>("FilePrivatePart", fvi.FilePrivatePart, expected.FilePrivatePart);
        TestStringProperty("FileVersion", fvi.FileVersion, expected.FileVersion);
        TestStringProperty("InternalName", fvi.InternalName, expected.InternalName);
        TestProperty<bool>("IsDebug", fvi.IsDebug, expected.IsDebug);
        TestProperty<bool>("IsPatched", fvi.IsPatched, expected.IsPatched);
        TestProperty<bool>("IsPrivateBuild", fvi.IsPrivateBuild, expected.IsPrivateBuild);
        TestProperty<bool>("IsPreRelease", fvi.IsPreRelease, expected.IsPreRelease);
        TestProperty<bool>("IsSpecialBuild", fvi.IsSpecialBuild, expected.IsSpecialBuild);
        TestStringProperty("Language", fvi.Language, expected.Language, expected.Language2, false);
        TestStringProperty("LegalCopyright", fvi.LegalCopyright, expected.LegalCopyright, false);
        TestStringProperty("LegalTrademarks", fvi.LegalTrademarks, expected.LegalTrademarks);
        TestStringProperty("OriginalFilename", fvi.OriginalFilename, expected.OriginalFilename);
        TestStringProperty("PrivateBuild", fvi.PrivateBuild, expected.PrivateBuild);
        TestProperty<int>("ProductBuildPart", fvi.ProductBuildPart, expected.ProductBuildPart);
        TestProperty<int>("ProductMajorPart", fvi.ProductMajorPart, expected.ProductMajorPart);
        TestProperty<int>("ProductMinorPart", fvi.ProductMinorPart, expected.ProductMinorPart);
        TestStringProperty("ProductName", fvi.ProductName, expected.ProductName, false);
        TestProperty<int>("ProductPrivatePart", fvi.ProductPrivatePart, expected.ProductPrivatePart);
        TestStringProperty("ProductVersion", fvi.ProductVersion, expected.ProductVersion, false);
        TestStringProperty("SpecialBuild", fvi.SpecialBuild, expected.SpecialBuild);

        //ToString
        String nl = Environment.NewLine;
        TestStringProperty("ToString()", fvi.ToString(),
                 "File:             " + fvi.FileName + nl +
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
                               false);
    }

    private void TestStringProperty(String propertyName, String actual, String expected)
    {
        TestStringProperty(propertyName, actual, expected, true);
    }

    private void TestStringProperty(String propertyName, String actual, String expected, bool testOnNonEnglishPlatform)
    {
        TestStringProperty(propertyName, actual, expected, null, true);
    }

    private void TestStringProperty(String propertyName, String actual, String expected, String alternate, bool testOnNonEnglishPlatform)
    {
        if (testOnNonEnglishPlatform || CultureInfo.CurrentCulture.Name == "en-US")
        {
            if ((actual == null && expected != null) ||
                (actual != null && !actual.Equals(expected) && !actual.Equals(alternate)))
            {
                Assert.True(false, string.Format("Error - Property '{0}' incorrect.  Expected == {1}, Actual == {2}, Alternate == {3}",
                    propertyName, GetUnicodeString(expected), GetUnicodeString(actual), GetUnicodeString(alternate)));
            }
        }
    }

    private void TestProperty<T>(String propertyName, T actual, T expected)
    {
        Assert.Equal(expected, actual);
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
        Interop.mincore.VerLanguageName(langid, lang, (uint)lang.Capacity);
        return lang.ToString();
    }
}
