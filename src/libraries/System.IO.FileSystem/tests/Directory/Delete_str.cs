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

public class Directory_Delete_str
{
    public static String s_strDtTmVer = "2000/04/26 20:33";
    public static String s_strClassMethod = "Directory.Delete(String)";
    public static String s_strTFName = "Delete_str.cs";
    public static String s_strTFPath = TestInfo.CurrentDirectory;
    public static String s_dirName = "Delete_str_test_TestDir";

#if !TEST_WINRT // TODO: renable once attributes are implemented.
    [Fact]
    public static void ShouldBeAbleToDeleteHiddenDirectory()
    {
        string dirPath = Path.Combine(TestInfo.CurrentDirectory, s_dirName);
        // [] Hidden Property set
        //-----------------------------------------------------------------

        DirectoryInfo dir2 = Directory.CreateDirectory(dirPath);
        dir2.Attributes = FileAttributes.Hidden;

        Directory.Delete(dir2.FullName);

        if (Directory.Exists(dirPath))
        {
            printerr("Error_948ug Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }
    }

    [Fact]
    public static void ShouldThrowIOExceptionDeletingReadOnlyDirectory()
    {
        string dirPath = Path.Combine(TestInfo.CurrentDirectory, s_dirName);
        // [] Readonly property set
        //-----------------------------------------------------------------
        DirectoryInfo dir2 = Directory.CreateDirectory(dirPath);
        dir2.Attributes = FileAttributes.ReadOnly;

        try
        {
            Directory.Delete(dir2.FullName);
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

        dir2.Attributes = new FileAttributes();
        dir2.Delete(true);
    }
#endif

    [Fact]
    public static void ShouldThrowIOExceptionIfContainedFileInUse()
    {
        string dirPath = Path.Combine(TestInfo.CurrentDirectory, s_dirName);
        // [] Delete directory with a file that is in use
        //-----------------------------------------------------------------
        Directory.CreateDirectory(dirPath);
        FileStream fs = new FileStream(Path.Combine(dirPath, Path.GetRandomFileName()), FileMode.Create);
        DirectoryInfo dir2 = new DirectoryInfo(dirPath);

        try
        {
            Directory.Delete(dir2.FullName);
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
    public static void ShouldThrowIOExceptionForDirectoryWithFiles()
    {
        string dirPath = Path.Combine(TestInfo.CurrentDirectory, s_dirName);
        // [] Delete directory with subdirectories containing files
        // [] String should be case insensitive
        //-----------------------------------------------------------------
        DirectoryInfo di1 = Directory.CreateDirectory(Path.Combine(dirPath, Path.GetRandomFileName()));
        Directory.CreateDirectory(Path.Combine(dirPath, Path.GetRandomFileName()));
        new FileStream(Path.Combine(di1.FullName, Path.GetRandomFileName()), FileMode.Create).Dispose();

        DirectoryInfo dir2 = new DirectoryInfo(dirPath);
        try
        {
            Directory.Delete(dir2.FullName);
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

        Directory.Delete(dir2.FullName.ToLower(), true);
        if (Directory.Exists(dirPath))
        {
            printerr("Error_26y7b! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }
    }

    [Fact]
    public static void ShouldThrowIOExceptionForDirectoryWithSubdirectories()
    {
        string dirPath = Path.Combine(TestInfo.CurrentDirectory, s_dirName);
        // [] Directory with subdirectories should fail
        //-----------------------------------------------------------------
        Directory.CreateDirectory(Path.Combine(dirPath, Path.GetRandomFileName()));
        DirectoryInfo dir2 = Directory.CreateDirectory(Path.Combine(dirPath, Path.GetRandomFileName()));
        Directory.Delete(dir2.FullName.ToUpper());

        if (Directory.Exists(dir2.FullName))
        {
            printerr("Error_49928! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }

        // Trying to delete one with subdirs
        Directory.CreateDirectory(Path.Combine(dirPath, Path.GetRandomFileName()));
        dir2 = new DirectoryInfo(dirPath);
        try
        {
            Directory.Delete(dir2.FullName);
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
        if (Directory.Exists(dirPath))
        {
            printerr("Error_917ct! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }
    }

    [Fact]
    public static void PositiveTest()
    {
        string dirPath = Path.Combine(TestInfo.CurrentDirectory, s_dirName);
        // [] Delete a directory without subdirectories or files
        //-----------------------------------------------------------------

        DirectoryInfo dir2 = Directory.CreateDirectory(dirPath);
        Directory.Delete(dir2.FullName);
        if (Directory.Exists(dirPath))
        {
            printerr("Error_987g7! Directory not deleted");
            Assert.True(false, "Directory not deleted");
        }
    }

    [Fact]
    public static void ShouldThrowDirectoryNotFoundExceptionForNonexistentDirectory()
    {
        // [] Exception when trying to delete a directory that does not exist
        //-----------------------------------------------------------------
        DirectoryInfo dir2 = new DirectoryInfo("ThisDoesNotExist");
        try
        {
            Directory.Delete("ThisDoesNotExist");
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
    }

    [Fact]
    public static void ShouldThrowIOExceptionDeletingCurrentDirectory()
    {
        // [] Exception when trying to delete current directory
        //-----------------------------------------------------------------

        DirectoryInfo dir2 = new DirectoryInfo(".");
        try
        {
            Directory.Delete(".");
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


