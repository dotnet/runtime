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
    static FileVersionInfoTest()
    {
        InitializeExpectedValues();
    }

    private static MyFVI s_fviNativeConsoleApp;
    private static MyFVI s_fviNativeLibrary;
    private static MyFVI s_fviSecondNativeLibrary;
    private static MyFVI s_fviAssembly1;
    private static MyFVI s_fviAssembly1_cs;

    [Fact]
    public static void FileVersionInfo_Normal()
    {
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), "NativeConsoleApp.exe"), s_fviNativeConsoleApp);
    }

    [Fact]
    public static void FileVersionInfo_Chinese()
    {
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), "NativeLibrary.dll"), s_fviNativeLibrary);
    }

    [Fact]
    public static void FileVersionInfo_DifferentFileVersionAndProductVersion()
    {
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), "SecondNativeLibrary.dll"), s_fviSecondNativeLibrary);
    }

    [Fact]
    public static void FileVersionInfo_CustomManagedAssembly()
    {
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), "Assembly1.dll"), s_fviAssembly1);
    }

    [Fact]
    public static void FileVersionInfo_EmptyFVI()
    {
        VerifyVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), "Assembly1.cs"), s_fviAssembly1_cs);
    }

    [Fact]
    public static void FileVersionInfo_FileNotFound()
    {
        VerifyVersionInfoException<System.IO.FileNotFoundException>(Path.Combine(Directory.GetCurrentDirectory(), "notfound.dll"));
    }

    // Additional Tests Wanted:
    // [] File exists but we don't have permission to read it
    // [] DLL has unknown codepage info
    // [] DLL language/codepage is 8-hex-digits (locale > 0x999) (different codepath)

    private static void VerifyVersionInfo(String filePath, MyFVI expected)
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

    private static void VerifyVersionInfoException<T>(String filePath) where T : Exception
    {
        Assert.Throws<T>(() => FileVersionInfo.GetVersionInfo(filePath));
    }

    private static void TestStringProperty(String propertyName, String actual, String expected)
    {
        TestStringProperty(propertyName, actual, expected, true);
    }

    private static void TestStringProperty(String propertyName, String actual, String expected, bool testOnNonEnglishPlatform)
    {
        TestStringProperty(propertyName, actual, expected, null, true);
    }

    private static void TestStringProperty(String propertyName, String actual, String expected, String alternate, bool testOnNonEnglishPlatform)
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

    private static void TestProperty<T>(String propertyName, T actual, T expected)
    {
        Assert.Equal(expected, actual);
    }

    private static void InitializeExpectedValues()
    {
        // NativeConsoleApp (English)
        s_fviNativeConsoleApp = new MyFVI();
        s_fviNativeConsoleApp.Comments = "";
        s_fviNativeConsoleApp.CompanyName = "This is not a real company name.";
        s_fviNativeConsoleApp.FileBuildPart = 3;
        s_fviNativeConsoleApp.FileDescription = "This is the description for the native console application.";
        s_fviNativeConsoleApp.FileMajorPart = 5;
        s_fviNativeConsoleApp.FileMinorPart = 4;
        s_fviNativeConsoleApp.FileName = Path.Combine(Directory.GetCurrentDirectory(), "NativeConsoleApp.exe");
        s_fviNativeConsoleApp.FilePrivatePart = 2;
        s_fviNativeConsoleApp.FileVersion = "5.4.3.2";
        s_fviNativeConsoleApp.InternalName = "NativeConsoleApp.exe";
        s_fviNativeConsoleApp.IsDebug = false;
        s_fviNativeConsoleApp.IsPatched = false;
        s_fviNativeConsoleApp.IsPrivateBuild = false;
        s_fviNativeConsoleApp.IsPreRelease = true;
        s_fviNativeConsoleApp.IsSpecialBuild = true;
        s_fviNativeConsoleApp.Language = GetFileVersionLanguage(0x0409);//English (United States)
        s_fviNativeConsoleApp.LegalCopyright = "Copyright (C) 2050";
        s_fviNativeConsoleApp.LegalTrademarks = "";
        s_fviNativeConsoleApp.OriginalFilename = "NativeConsoleApp.exe";
        s_fviNativeConsoleApp.PrivateBuild = "";
        s_fviNativeConsoleApp.ProductBuildPart = 3;
        s_fviNativeConsoleApp.ProductMajorPart = 5;
        s_fviNativeConsoleApp.ProductMinorPart = 4;
        s_fviNativeConsoleApp.ProductName = "NativeConsoleApp";
        s_fviNativeConsoleApp.ProductPrivatePart = 2;
        s_fviNativeConsoleApp.ProductVersion = "5.4.3.2";
        s_fviNativeConsoleApp.SpecialBuild = "";

        // NativeLibrary.dll (Chinese)
        s_fviNativeLibrary = new MyFVI();
        s_fviNativeLibrary.Comments = "";
        s_fviNativeLibrary.CompanyName = "A non-existent company";
        s_fviNativeLibrary.FileBuildPart = 3;
        s_fviNativeLibrary.FileDescription = "Here is the description of the native library.";
        s_fviNativeLibrary.FileMajorPart = 9;
        s_fviNativeLibrary.FileMinorPart = 9;
        s_fviNativeLibrary.FileName = Path.Combine(Directory.GetCurrentDirectory(), "NativeLibrary.dll");
        s_fviNativeLibrary.FilePrivatePart = 3;
        s_fviNativeLibrary.FileVersion = "9.9.3.3";
        s_fviNativeLibrary.InternalName = "NativeLi.dll";
        s_fviNativeLibrary.IsDebug = false;
        s_fviNativeLibrary.IsPatched = true;
        s_fviNativeLibrary.IsPrivateBuild = false;
        s_fviNativeLibrary.IsPreRelease = true;
        s_fviNativeLibrary.IsSpecialBuild = false;
        s_fviNativeLibrary.Language = GetFileVersionLanguage(0x0004);//Chinese (Simplified)
        s_fviNativeLibrary.Language2 = GetFileVersionLanguage(0x0804);//Chinese (Simplified, PRC) - changed, but not yet on all platforms
        s_fviNativeLibrary.LegalCopyright = "None";
        s_fviNativeLibrary.LegalTrademarks = "";
        s_fviNativeLibrary.OriginalFilename = "NativeLi.dll";
        s_fviNativeLibrary.PrivateBuild = "";
        s_fviNativeLibrary.ProductBuildPart = 40;
        s_fviNativeLibrary.ProductMajorPart = 20;
        s_fviNativeLibrary.ProductMinorPart = 30;
        s_fviNativeLibrary.ProductName = "I was never given a name.";
        s_fviNativeLibrary.ProductPrivatePart = 50;
        s_fviNativeLibrary.ProductVersion = "20.30.40.50";
        s_fviNativeLibrary.SpecialBuild = "";

        // Mtxex.dll
        s_fviSecondNativeLibrary = new MyFVI();
        s_fviSecondNativeLibrary.Comments = "";
        s_fviSecondNativeLibrary.CompanyName = "";
        s_fviSecondNativeLibrary.FileBuildPart = 0;
        s_fviSecondNativeLibrary.FileDescription = "";
        s_fviSecondNativeLibrary.FileMajorPart = 0;
        s_fviSecondNativeLibrary.FileMinorPart = 65535;
        s_fviSecondNativeLibrary.FileName = Path.Combine(Directory.GetCurrentDirectory(), "SecondNativeLibrary.dll");
        s_fviSecondNativeLibrary.FilePrivatePart = 2;
        s_fviSecondNativeLibrary.FileVersion = "0.65535.0.2";
        s_fviSecondNativeLibrary.InternalName = "SecondNa.dll";
        s_fviSecondNativeLibrary.IsDebug = false;
        s_fviSecondNativeLibrary.IsPatched = false;
        s_fviSecondNativeLibrary.IsPrivateBuild = false;
        s_fviSecondNativeLibrary.IsPreRelease = false;
        s_fviSecondNativeLibrary.IsSpecialBuild = false;
        s_fviSecondNativeLibrary.Language = GetFileVersionLanguage(0x0400);//Process Default Language
        s_fviSecondNativeLibrary.LegalCopyright = "Copyright (C) 1 - 2014";
        s_fviSecondNativeLibrary.LegalTrademarks = "";
        s_fviSecondNativeLibrary.OriginalFilename = "SecondNa.dll";
        s_fviSecondNativeLibrary.PrivateBuild = "";
        s_fviSecondNativeLibrary.ProductBuildPart = 0;
        s_fviSecondNativeLibrary.ProductMajorPart = 1;
        s_fviSecondNativeLibrary.ProductMinorPart = 0;
        s_fviSecondNativeLibrary.ProductName = "Unkown_Product_Name";
        s_fviSecondNativeLibrary.ProductPrivatePart = 1;
        s_fviSecondNativeLibrary.ProductVersion = "1.0.0.1";
        s_fviSecondNativeLibrary.SpecialBuild = "";

        // Assembly1.dll
        s_fviAssembly1 = new MyFVI();
        s_fviAssembly1.Comments = "Have you played a Contoso amusement device today?";
        s_fviAssembly1.CompanyName = "The name of the company.";
        s_fviAssembly1.FileBuildPart = 2;
        s_fviAssembly1.FileDescription = "My File";
        s_fviAssembly1.FileMajorPart = 4;
        s_fviAssembly1.FileMinorPart = 3;
        s_fviAssembly1.FileName = Path.Combine(Directory.GetCurrentDirectory(), "Assembly1.dll");
        s_fviAssembly1.FilePrivatePart = 1;
        s_fviAssembly1.FileVersion = "4.3.2.1";
        s_fviAssembly1.InternalName = "Assembly1.dll";
        s_fviAssembly1.IsDebug = false;
        s_fviAssembly1.IsPatched = false;
        s_fviAssembly1.IsPrivateBuild = false;
        s_fviAssembly1.IsPreRelease = false;
        s_fviAssembly1.IsSpecialBuild = false;
        s_fviAssembly1.Language = GetFileVersionLanguage(0x0000);//Language Neutral
        s_fviAssembly1.LegalCopyright = "Copyright, you betcha!";
        s_fviAssembly1.LegalTrademarks = "TM";
        s_fviAssembly1.OriginalFilename = "Assembly1.dll";
        s_fviAssembly1.PrivateBuild = "";
        s_fviAssembly1.ProductBuildPart = 2;
        s_fviAssembly1.ProductMajorPart = 4;
        s_fviAssembly1.ProductMinorPart = 3;
        s_fviAssembly1.ProductName = "The greatest product EVER";
        s_fviAssembly1.ProductPrivatePart = 1;
        s_fviAssembly1.ProductVersion = "4.3.2.1";
        s_fviAssembly1.SpecialBuild = "";

        // Assembly1.cs
        s_fviAssembly1_cs = new MyFVI();
        s_fviAssembly1_cs.Comments = null;
        s_fviAssembly1_cs.CompanyName = null;
        s_fviAssembly1_cs.FileBuildPart = 0;
        s_fviAssembly1_cs.FileDescription = null;
        s_fviAssembly1_cs.FileMajorPart = 0;
        s_fviAssembly1_cs.FileMinorPart = 0;
        s_fviAssembly1_cs.FileName = Path.Combine(Directory.GetCurrentDirectory(), "Assembly1.cs");
        s_fviAssembly1_cs.FilePrivatePart = 0;
        s_fviAssembly1_cs.FileVersion = null;
        s_fviAssembly1_cs.InternalName = null;
        s_fviAssembly1_cs.IsDebug = false;
        s_fviAssembly1_cs.IsPatched = false;
        s_fviAssembly1_cs.IsPrivateBuild = false;
        s_fviAssembly1_cs.IsPreRelease = false;
        s_fviAssembly1_cs.IsSpecialBuild = false;
        s_fviAssembly1_cs.Language = null;
        s_fviAssembly1_cs.LegalCopyright = null;
        s_fviAssembly1_cs.LegalTrademarks = null;
        s_fviAssembly1_cs.OriginalFilename = null;
        s_fviAssembly1_cs.PrivateBuild = null;
        s_fviAssembly1_cs.ProductBuildPart = 0;
        s_fviAssembly1_cs.ProductMajorPart = 0;
        s_fviAssembly1_cs.ProductMinorPart = 0;
        s_fviAssembly1_cs.ProductName = null;
        s_fviAssembly1_cs.ProductPrivatePart = 0;
        s_fviAssembly1_cs.ProductVersion = null;
        s_fviAssembly1_cs.SpecialBuild = null;
    }

    internal struct MyFVI
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
