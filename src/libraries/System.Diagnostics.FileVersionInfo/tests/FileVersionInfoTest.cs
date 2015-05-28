// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using Xunit;

//
// System.Diagnostics.FileVersionInfo Test
//
public class FileVersionInfoTest
{
    private const string NativeConsoleAppFileName = "NativeConsoleApp.exe";
    private const string NativeLibraryFileName = "NativeLibrary.dll";
    private const string SecondNativeLibraryFileName = "SecondNativeLibrary.dll";
    private const string TestAssemblyFileName = "System.Diagnostics.FileVersionInfo.TestAssembly.dll";
    private const string TestCsFileName = "Assembly1.cs";
    private const string TestNotFoundFileName = "notfound.dll";

    private MyFVI _fviNativeConsoleApp;
    private MyFVI _fviNativeLibrary;
    private MyFVI _fviSecondNativeLibrary;
    private MyFVI _fviAssembly1;
    private MyFVI _fviAssembly1_cs;

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void FileVersionInfo_Normal()
    {
        EnsureExpectedValuesInitialized();
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), NativeConsoleAppFileName), _fviNativeConsoleApp);
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void FileVersionInfo_Chinese()
    {
        EnsureExpectedValuesInitialized();
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), NativeLibraryFileName), _fviNativeLibrary);
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void FileVersionInfo_DifferentFileVersionAndProductVersion()
    {
        EnsureExpectedValuesInitialized();
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), SecondNativeLibraryFileName), _fviSecondNativeLibrary);
    }

    [Fact]
    public void FileVersionInfo_CustomManagedAssembly()
    {
        EnsureExpectedValuesInitialized();
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), TestAssemblyFileName), _fviAssembly1);
    }

    [Fact]
    public void FileVersionInfo_EmptyFVI()
    {
        EnsureExpectedValuesInitialized();
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), TestCsFileName), _fviAssembly1_cs);
    }

    [Fact]
    public void FileVersionInfo_FileNotFound()
    {
        VerifyVersionInfoException<FileNotFoundException>(Path.Combine(Directory.GetCurrentDirectory(), TestNotFoundFileName));
    }

    // Additional Tests Wanted:
    // [] File exists but we don't have permission to read it
    // [] DLL has unknown codepage info
    // [] DLL language/codepage is 8-hex-digits (locale > 0x999) (different codepath)

    private void VerifyVersionInfo(String filePath, MyFVI expected)
    {
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);
        TestStringProperty("Comments", fvi.Comments, expected.Comments);
        TestStringProperty("CompanyName", fvi.Comments, expected.Comments);
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

    private void VerifyVersionInfoException<T>(String filePath) where T : Exception
    {
        Assert.Throws<T>(() => FileVersionInfo.GetVersionInfo(filePath));
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

    private void EnsureExpectedValuesInitialized()
    {
        if (_fviNativeConsoleApp != null)
        {
            return;
        }

        // NativeConsoleApp (English)
        _fviNativeConsoleApp = new MyFVI();
        _fviNativeConsoleApp.Comments = "";
        _fviNativeConsoleApp.CompanyName = "This is not a real company name.";
        _fviNativeConsoleApp.FileBuildPart = 3;
        _fviNativeConsoleApp.FileDescription = "This is the description for the native console application.";
        _fviNativeConsoleApp.FileMajorPart = 5;
        _fviNativeConsoleApp.FileMinorPart = 4;
        _fviNativeConsoleApp.FileName = Path.Combine(Directory.GetCurrentDirectory(), NativeConsoleAppFileName);
        _fviNativeConsoleApp.FilePrivatePart = 2;
        _fviNativeConsoleApp.FileVersion = "5.4.3.2";
        _fviNativeConsoleApp.InternalName = NativeConsoleAppFileName;
        _fviNativeConsoleApp.IsDebug = false;
        _fviNativeConsoleApp.IsPatched = false;
        _fviNativeConsoleApp.IsPrivateBuild = false;
        _fviNativeConsoleApp.IsPreRelease = true;
        _fviNativeConsoleApp.IsSpecialBuild = true;
        _fviNativeConsoleApp.Language = GetFileVersionLanguage(0x0409);//English (United States)
        _fviNativeConsoleApp.LegalCopyright = "Copyright (C) 2050";
        _fviNativeConsoleApp.LegalTrademarks = "";
        _fviNativeConsoleApp.OriginalFilename = NativeConsoleAppFileName;
        _fviNativeConsoleApp.PrivateBuild = "";
        _fviNativeConsoleApp.ProductBuildPart = 3;
        _fviNativeConsoleApp.ProductMajorPart = 5;
        _fviNativeConsoleApp.ProductMinorPart = 4;
        _fviNativeConsoleApp.ProductName = Path.GetFileNameWithoutExtension(NativeConsoleAppFileName);
        _fviNativeConsoleApp.ProductPrivatePart = 2;
        _fviNativeConsoleApp.ProductVersion = "5.4.3.2";
        _fviNativeConsoleApp.SpecialBuild = "";

        // NativeLibrary.dll (Chinese)
        _fviNativeLibrary = new MyFVI();
        _fviNativeLibrary.Comments = "";
        _fviNativeLibrary.CompanyName = "A non-existent company";
        _fviNativeLibrary.FileBuildPart = 3;
        _fviNativeLibrary.FileDescription = "Here is the description of the native library.";
        _fviNativeLibrary.FileMajorPart = 9;
        _fviNativeLibrary.FileMinorPart = 9;
        _fviNativeLibrary.FileName = Path.Combine(Directory.GetCurrentDirectory(), NativeLibraryFileName);
        _fviNativeLibrary.FilePrivatePart = 3;
        _fviNativeLibrary.FileVersion = "9.9.3.3";
        _fviNativeLibrary.InternalName = "NativeLi.dll";
        _fviNativeLibrary.IsDebug = false;
        _fviNativeLibrary.IsPatched = true;
        _fviNativeLibrary.IsPrivateBuild = false;
        _fviNativeLibrary.IsPreRelease = true;
        _fviNativeLibrary.IsSpecialBuild = false;
        _fviNativeLibrary.Language = GetFileVersionLanguage(0x0004);//Chinese (Simplified)
        _fviNativeLibrary.Language2 = GetFileVersionLanguage(0x0804);//Chinese (Simplified, PRC) - changed, but not yet on all platforms
        _fviNativeLibrary.LegalCopyright = "None";
        _fviNativeLibrary.LegalTrademarks = "";
        _fviNativeLibrary.OriginalFilename = "NativeLi.dll";
        _fviNativeLibrary.PrivateBuild = "";
        _fviNativeLibrary.ProductBuildPart = 40;
        _fviNativeLibrary.ProductMajorPart = 20;
        _fviNativeLibrary.ProductMinorPart = 30;
        _fviNativeLibrary.ProductName = "I was never given a name.";
        _fviNativeLibrary.ProductPrivatePart = 50;
        _fviNativeLibrary.ProductVersion = "20.30.40.50";
        _fviNativeLibrary.SpecialBuild = "";

        // Mtxex.dll
        _fviSecondNativeLibrary = new MyFVI();
        _fviSecondNativeLibrary.Comments = "";
        _fviSecondNativeLibrary.CompanyName = "";
        _fviSecondNativeLibrary.FileBuildPart = 0;
        _fviSecondNativeLibrary.FileDescription = "";
        _fviSecondNativeLibrary.FileMajorPart = 0;
        _fviSecondNativeLibrary.FileMinorPart = 65535;
        _fviSecondNativeLibrary.FileName = Path.Combine(Directory.GetCurrentDirectory(), SecondNativeLibraryFileName);
        _fviSecondNativeLibrary.FilePrivatePart = 2;
        _fviSecondNativeLibrary.FileVersion = "0.65535.0.2";
        _fviSecondNativeLibrary.InternalName = "SecondNa.dll";
        _fviSecondNativeLibrary.IsDebug = false;
        _fviSecondNativeLibrary.IsPatched = false;
        _fviSecondNativeLibrary.IsPrivateBuild = false;
        _fviSecondNativeLibrary.IsPreRelease = false;
        _fviSecondNativeLibrary.IsSpecialBuild = false;
        _fviSecondNativeLibrary.Language = GetFileVersionLanguage(0x0400);//Process Default Language
        _fviSecondNativeLibrary.LegalCopyright = "Copyright (C) 1 - 2014";
        _fviSecondNativeLibrary.LegalTrademarks = "";
        _fviSecondNativeLibrary.OriginalFilename = "SecondNa.dll";
        _fviSecondNativeLibrary.PrivateBuild = "";
        _fviSecondNativeLibrary.ProductBuildPart = 0;
        _fviSecondNativeLibrary.ProductMajorPart = 1;
        _fviSecondNativeLibrary.ProductMinorPart = 0;
        _fviSecondNativeLibrary.ProductName = "Unkown_Product_Name";
        _fviSecondNativeLibrary.ProductPrivatePart = 1;
        _fviSecondNativeLibrary.ProductVersion = "1.0.0.1";
        _fviSecondNativeLibrary.SpecialBuild = "";

        // Assembly1.dll
        _fviAssembly1 = new MyFVI();
        _fviAssembly1.Comments = "Have you played a Contoso amusement device today?";
        _fviAssembly1.CompanyName = "The name of the company.";
        _fviAssembly1.FileBuildPart = 2;
        _fviAssembly1.FileDescription = "My File";
        _fviAssembly1.FileMajorPart = 4;
        _fviAssembly1.FileMinorPart = 3;
        _fviAssembly1.FileName = Path.Combine(Directory.GetCurrentDirectory(), TestAssemblyFileName);
        _fviAssembly1.FilePrivatePart = 1;
        _fviAssembly1.FileVersion = "4.3.2.1";
        _fviAssembly1.InternalName = TestAssemblyFileName;
        _fviAssembly1.IsDebug = false;
        _fviAssembly1.IsPatched = false;
        _fviAssembly1.IsPrivateBuild = false;
        _fviAssembly1.IsPreRelease = false;
        _fviAssembly1.IsSpecialBuild = false;
        _fviAssembly1.Language = GetFileVersionLanguage(0x0000);//Language Neutral
        _fviAssembly1.LegalCopyright = "Copyright, you betcha!";
        _fviAssembly1.LegalTrademarks = "TM";
        _fviAssembly1.OriginalFilename = TestAssemblyFileName;
        _fviAssembly1.PrivateBuild = "";
        _fviAssembly1.ProductBuildPart = 2;
        _fviAssembly1.ProductMajorPart = 4;
        _fviAssembly1.ProductMinorPart = 3;
        _fviAssembly1.ProductName = "The greatest product EVER";
        _fviAssembly1.ProductPrivatePart = 1;
        _fviAssembly1.ProductVersion = "4.3.2.1";
        _fviAssembly1.SpecialBuild = "";

        // Assembly1.cs
        _fviAssembly1_cs = new MyFVI();
        _fviAssembly1_cs.Comments = null;
        _fviAssembly1_cs.CompanyName = null;
        _fviAssembly1_cs.FileBuildPart = 0;
        _fviAssembly1_cs.FileDescription = null;
        _fviAssembly1_cs.FileMajorPart = 0;
        _fviAssembly1_cs.FileMinorPart = 0;
        _fviAssembly1_cs.FileName = Path.Combine(Directory.GetCurrentDirectory(), TestCsFileName);
        _fviAssembly1_cs.FilePrivatePart = 0;
        _fviAssembly1_cs.FileVersion = null;
        _fviAssembly1_cs.InternalName = null;
        _fviAssembly1_cs.IsDebug = false;
        _fviAssembly1_cs.IsPatched = false;
        _fviAssembly1_cs.IsPrivateBuild = false;
        _fviAssembly1_cs.IsPreRelease = false;
        _fviAssembly1_cs.IsSpecialBuild = false;
        _fviAssembly1_cs.Language = null;
        _fviAssembly1_cs.LegalCopyright = null;
        _fviAssembly1_cs.LegalTrademarks = null;
        _fviAssembly1_cs.OriginalFilename = null;
        _fviAssembly1_cs.PrivateBuild = null;
        _fviAssembly1_cs.ProductBuildPart = 0;
        _fviAssembly1_cs.ProductMajorPart = 0;
        _fviAssembly1_cs.ProductMinorPart = 0;
        _fviAssembly1_cs.ProductName = null;
        _fviAssembly1_cs.ProductPrivatePart = 0;
        _fviAssembly1_cs.ProductVersion = null;
        _fviAssembly1_cs.SpecialBuild = null;
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

    static String GetUnicodeString(String str)
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
