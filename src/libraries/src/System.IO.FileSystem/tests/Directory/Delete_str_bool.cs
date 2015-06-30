// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_Delete_str_bool
{
    public static String s_strDtTmVer = "2000/04/26 20:33";
    public static String s_strClassMethod = "Directory.Delete(String,Boolean)";
    public static String s_strTFName = "Delete_str_bool.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    public static string s_dirName = Path.GetRandomFileName();

#if !TEST_WINRT // TODO: renable once attributes are implemented.
    [Fact]
    public static void DirectoryWithHiddenAttribute()
    {
        // [] Deleting directory with Hidden property set should pass
        //-----------------------------------------------------------------
        string dirName = Path.Combine(TestInfo.CurrentDirectory, s_dirName + "_hidden");
        DirectoryInfo dir2 = Directory.CreateDirectory(dirName);
        dir2.Attributes = FileAttributes.Hidden;

        Directory.Delete(dir2.FullName, true);

        if (Directory.Exists(dirName))
        {
            printerr("Error_948ug Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }

        if (Directory.Exists(dirName))
        {
            new DirectoryInfo(dirName).Attributes = new FileAttributes();
            Directory.Delete(dirName, true);
        }
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)] // readonly directories can be deleted on Unix
    public static void DirectoryWithReadOnlyAttribute()
    {
        // [] Deleting directory with Readonly property set should always fail 
        //-----------------------------------------------------------------
        string dirName = Path.Combine(TestInfo.CurrentDirectory, s_dirName + "_readOnly");
        DirectoryInfo dir2 = Directory.CreateDirectory(dirName);
        dir2.Attributes = FileAttributes.ReadOnly;

        try
        {
            Directory.Delete(dir2.FullName, false);

            printerr("Error_t01uv! Expected exception not thrown");
            Assert.True(false, "Expected IOException not thrown");
        }
        catch (IOException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_198yv! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }

        try
        {
            Directory.Delete(dir2.FullName, true);

            printerr("Error_948ug Expected exception not thrown");
            Assert.True(false, "Expected IOException not thrown");
        }
        catch (IOException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_y7853! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }
        dir2.Attributes = new FileAttributes();
        dir2.Delete(true);
    }
#endif

    [Fact]
    [PlatformSpecific(PlatformID.Windows)] // directories with in-use files can be deleted on Unix
    public static void DirectoryWithFileInUse()
    {
        // [] Delete directory with a file that is in use
        //-----------------------------------------------------------------
        string dirName = Path.Combine(TestInfo.CurrentDirectory, s_dirName + "_inUse");
        Directory.CreateDirectory(dirName);
        FileStream fs = new FileStream(Path.Combine(dirName, Path.GetRandomFileName()), FileMode.Create);
        DirectoryInfo dir2 = new DirectoryInfo(dirName);

        try
        {
            Directory.Delete(dir2.FullName, true);

            printerr("Error_ty7b7! Expected exception not thrown");
            Assert.True(false, "Expected IOException not thrown");
        }
        catch (IOException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_4919o! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }
        fs.Dispose();
        Directory.Delete(dir2.FullName, true);
    }

    [Fact]
    public static void DirectoryContainingFiles()
    {
        // [] Delete directory with subdirectories containing files should fail when false and pass when true
        // [] String should be case insensitive
        //-----------------------------------------------------------------
        string dirName = Path.Combine(TestInfo.CurrentDirectory, s_dirName + "_withFiles");
        Directory.CreateDirectory(Path.Combine(dirName, "Test1"));
        Directory.CreateDirectory(Path.Combine(dirName, "Test2"));
        new FileStream(Path.Combine(dirName, "Test1", "Hello.tmp"), FileMode.Create).Dispose();

        DirectoryInfo dir2 = new DirectoryInfo(dirName);
        try
        {
            Directory.Delete(dir2.FullName, false);

            printerr("Error_241y7! Expected exception not thrown");
            Assert.True(false, "Expected IOException not thrown");
        }
        catch (IOException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_07509! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }

        Directory.Delete(dir2.FullName, true);
        if (Directory.Exists(dirName))
        {
            printerr("Error_26y7b! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }
    }

    [Fact]
    public static void DirectoryWithSubdirectories()
    {
        // [] Directory with subdirectories should fail when passed false and pass when true
        string dirName = Path.Combine(TestInfo.CurrentDirectory, s_dirName + "_withSubDirs");
        Directory.CreateDirectory(Path.Combine(dirName, "Test1"));
        DirectoryInfo dir2 = Directory.CreateDirectory(Path.Combine(dirName, "Test2"));
        Directory.Delete(dir2.FullName, false);
        if (Directory.Exists(Path.Combine(dirName, "Test2")))
        {
            printerr("Error_49928! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }

        // Trying to delete one with subdirs
        Directory.CreateDirectory(Path.Combine(dirName, "Test2"));
        dir2 = new DirectoryInfo(dirName);
        try
        {
            Directory.Delete(dir2.FullName, false);

            printerr("Error_5y78b! Expected exception not thrown");
            Assert.True(false, "Expected IOException not thrown");
        }
        catch (IOException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_98yg7! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }

        // Now delete it
        Directory.Delete(dir2.FullName, true);
        if (Directory.Exists(dirName))
        {
            printerr("Error_917ct! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }
    }

    [Fact]
    public static void PositiveTest()
    {
        // [] Delete a directory without subdirectories or files should work for recursive and non-recursive
        string dirName = Path.Combine(TestInfo.CurrentDirectory, s_dirName + "_empty");
        DirectoryInfo dir2 = Directory.CreateDirectory(dirName);
        Directory.Delete(dir2.FullName, false);
        if (Directory.Exists(dirName))
        {
            printerr("Error_987g7! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }

        dir2 = Directory.CreateDirectory(dirName);
        Directory.Delete(dir2.FullName, true);
        if (Directory.Exists(dirName))
        {
            printerr("Error_0919c! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }
    }

    [Fact]
    public static void ShouldGetDirectoryNotFoundExceptionDeletingNonexistantDirectory()
    {
        // [] Exception when trying to delete a directory that does not exist
        DirectoryInfo dir2 = new DirectoryInfo("ThisDoesNotExist");
        try
        {
            Directory.Delete("ThisDoesNotExist", false);

            printerr("Error_9138v! Expected exception not thrown");
            Assert.True(false, "Expected DirectoryNotFoundException not thrown");
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_799tb! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }

        try
        {
            Directory.Delete("ThisDoesNotExist", true);

            printerr("Error_7958x! Expected exception not thrown");
            Assert.True(false, "Expected DirectoryNotFoundException not thrown");
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_1t859! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }
    }

    [Fact]
    public static void ShouldGetIOExceptionDeletingCurrentDirectory()
    {
        // [] Exception when trying to delete a Directory in use and 
        //-----------------------------------------------------------------
        DirectoryInfo dir2 = new DirectoryInfo(".");
        try
        {
            Directory.Delete(".", false);

            printerr("Error_48y7b! Expected exception not thrown");
            Assert.True(false, "Expected IOException not thrown");
        }
        catch (IOException)
        {
        }
        catch (Exception exc)
        {
            printerr("Error_2019c! Incorrect exception thrown, exc==" + exc.ToString());
            throw;
        }
    }


    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}


