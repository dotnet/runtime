// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public class Directory_Modify_FailSafe
{
    private delegate void ExceptionCode();
    private static bool s_pass = true;

    // Don't change the files array - it's used both by the 
    // FileManipulationTest and the FileEnumeratorTest (in two different
    // directories, of course).
    private static String[] s_files = new String[5];

    static Directory_Modify_FailSafe()
    {
        s_files[0] = "a.1";
        s_files[1] = "b.1";
        s_files[2] = "c.txt";
        s_files[3] = "d.blah";
        s_files[4] = "e.blah";
    }

    [Fact]
    public static void DirectoryInfoTest()
    {
        const String dirName = "DirectoryInfoTestDir";
        const String altDirName = "DirectoryInfoTestDir2";

        // Clean up from any failed test run
        if (Directory.Exists(dirName))
            Directory.Delete(dirName, true);
        if (Directory.Exists(altDirName))
            Directory.Delete(altDirName, true);

        DirectoryInfo di = new DirectoryInfo(dirName);
        if (di.Exists)
            throw new Exception("Directory exists at beginning of test!");
        di.Create();
        Stream s = File.Create(Path.Combine(di.Name, "foo"));
        s.Dispose();
        di.CreateSubdirectory("bar");

        // Attributes test
        di.Refresh();  // Reload attributes information!
        FileAttributes attr = di.Attributes;
        if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
            throw new Exception("Unexpected attributes on the directory - the directory bit wasn't set!  Got: " + attr);
        // BLORF TODO: blorf write set the system attribute?

        // Rename directory via the MoveTo method, the move it back.
        di = FailSafeDirectoryOperations.MoveDirectoryInfo(di, altDirName);
        if (Directory.Exists(dirName))
            throw new Exception("Old directory still exists after MoveTo!");
        if (!Directory.Exists(altDirName))
            throw new Exception("New directory doesn't exists after MoveTo!");
        if (!di.Exists)
            throw new Exception("DirectoryInfo says the directory doesn't exist after first MoveTo!");
        di = FailSafeDirectoryOperations.MoveDirectoryInfo(di, dirName);
        if (!di.Exists)
            throw new Exception("DirectoryInfo says the directory doesn't exist after second MoveTo!");

        // Get files and directories now.
        FileInfo[] files = di.GetFiles();
        if (files.Length != 1)
            throw new Exception("GetFiles should have returned just one file!  got: " + files.Length);
        if (!"foo".Equals(files[0].Name))
            throw new Exception("FileInfo's Name should have been foo, but was: " + files[0].Name);

        DirectoryInfo[] dirs = di.GetDirectories();
        if (dirs.Length != 1)
            throw new Exception("GetDirectories should have returned just one dir!  got: " + dirs.Length);
        if (!"bar".Equals(dirs[0].Name))
            throw new Exception("DirectoryInfo's Name should have been bar, but was: " + dirs[0].Name);

        FileSystemInfo[] infos = di.GetFileSystemInfos();
        if (infos.Length != 2)
            throw new Exception("GetFileSystemInfos should have returned 2!  got: " + infos.Length);
        FileInfo tempFi = infos[0] as FileInfo;
        DirectoryInfo tempDi = null;
        if (tempFi == null)
        {
            tempFi = infos[1] as FileInfo;
            tempDi = infos[0] as DirectoryInfo;
        }
        else
        {
            tempDi = infos[1] as DirectoryInfo;
        }
        if (!tempFi.Name.Equals("foo"))
            throw new Exception("GetFileSystemInfo returned FileInfo with wrong name!  got: " + tempFi.Name);
        if (!tempDi.Name.Equals("bar"))
            throw new Exception("GetFileSystemInfo returned DirectoryInfo with wrong name!  got: " + tempDi.Name);


        // Test DirectoryInfo.Name on something like "c:\bar\"
        DirectoryInfo subDir = new DirectoryInfo(Path.Combine(di.Name, "bar") + Path.DirectorySeparatorChar);
        if (!subDir.Name.Equals("bar"))
            throw new Exception("Subdirectory name was wrong.  Expected bar, Got: " + subDir.Name);

        DirectoryInfo parent = subDir.Parent;
        if (!DirNameEquals(parent.FullName, di.FullName))
            throw new Exception("DI.FullName != SubDir.Parent.FullName!  subdir full name: " + parent.FullName);

        // Check more info about the DirectoryInfo
        String rootName = Path.GetPathRoot(Directory.GetCurrentDirectory());
        DirectoryInfo root = di.Root;
        if (!rootName.Equals(root.Name))
            throw new Exception(String.Format("Root directory name was wrong!  rootName: {0}  DI.Root.Name: {1}", rootName, root.Name));

        // Test DirectoryInfo behavior for the root
        string rootPath = root.FullName;
        DirectoryInfo c = new DirectoryInfo(rootPath);
        if (!rootPath.Equals(c.Name))
            throw new Exception("DirectoryInfo name for root was wrong!  got: " + c.Name);
        if (!rootPath.Equals(c.FullName))
            throw new Exception("DirectoryInfo FullName for root was wrong!  got: " + c.FullName);
        if (null != c.Parent)
            throw new Exception("DirectoryInfo::Parent for root is not null!");

        FailSafeDirectoryOperations.DeleteDirectoryInfo(di, true);
        di.Refresh();
        if (di.Exists)
            throw new Exception("Directory still exists at end of test!");

        Assert.True(s_pass);
    }

    private static bool DirNameEquals(String a, String b)
    {
        if (a.Length > 3 && a[a.Length - 1] == Path.DirectorySeparatorChar)
            a = a.Substring(0, a.Length - 1);
        if (b.Length > 3 && b[b.Length - 1] == Path.DirectorySeparatorChar)
            b = b.Substring(0, b.Length - 1);
        return a.Equals(b);
    }

    [Fact]
    [ActiveIssue(1220)] // SetCurrentDirectory
    public static void FileManipulationTest()
    {
        String dirName = "FileManipulationTest";
        try
        {
            Directory.CreateDirectory(dirName);
        }
        catch (IOException)
        {
            /*
            if (io.ErrorCode != 183) { // Path exists
                Console.WriteLine("Error when creating directory "+dir);
                Console.WriteLine(io.ErrorCode);
                throw io;
            }
            */
            // assume the path exists.
            // Clean out the directory
            /*
            FileEnumerator cleaner = new FileEnumerator(dir+"\\*");
            while(cleaner.MoveNext())
                if (!cleaner.Name.Equals(".") && !cleaner.Name.Equals(".."))
                    cleaner.Remove();
            cleaner.Close();
            */
            Directory.Delete(dirName, true);
            Directory.CreateDirectory(dirName);
        }

        String origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(dirName);
        CreateTestFiles();
        Directory.SetCurrentDirectory(origDir);
        DirectoryInfo dir = new DirectoryInfo(dirName);

        // Try getting all files.
        FileInfo[] found = dir.GetFiles("*");
        if (found.Length != s_files.Length)
            throw new Exception("After creating files, num found != num created. expected: " + s_files.Length + "  got: " + found.Length);
        for (int i = 0; i < s_files.Length; i++)
        {
            if (!s_files[i].Equals(found[i].Name))
                throw new Exception("Couldn't find a file in the directory!  thought I'd get: " + s_files[i] + "  got: " + found[i]);
        }

        // Try getting a file that isn't there.
        found = dir.GetFiles("this_file_doesnt_exist.nope");
        if (found.Length != 0)
            throw new Exception("Ack!  Tried to do GetFiles(non-existant file) and got something!");

        // Try getting a wildcard pattern that isn't there.
        found = dir.GetFiles("*.nope");
        if (found.Length != 0)
            throw new Exception("Ack!  Tried to do GetFiles(*.nope) and got something!");

        // Try listing all .blah files.
        found = dir.GetFiles("*.blah");
        if (found.Length != 2)
            throw new Exception("When looking for *.blah, found wrong number!  expected: 2  got: " + found.Length);
        if (!found[0].Name.Equals(s_files[3]))
            throw new Exception("Found[0] wasn't files[3] when listing *.blah!  got: " + found[0] + "  expected: " + s_files[3]);
        if (!found[1].Name.Equals(s_files[4]))
            throw new Exception("Found[1] wasn't files[4] when listing *.blah!  got: " + found[1]);

        // Try listing all .txt files.
        found = dir.GetFiles("*.txt");
        if (found.Length != 1)
            throw new Exception("When looking for *.txt, found wrong number!  expected: 1  got: " + found.Length);
        if (!found[0].Name.Equals(s_files[2]))
            throw new Exception("Found[0] wasn't files[2] when listing *.txt!  got: " + found[0]);

        // Try listing all .1 files.
        found = dir.GetFiles("*.1");
        if (found.Length != 2)
            throw new Exception("When looking for *.1, found wrong number!  expected: 2  got: " + found.Length);
        if (!found[0].Name.Equals(s_files[0]))
            throw new Exception("Found[0] wasn't files[0] when listing *.1!  got: " + found[0]);
        if (!found[1].Name.Equals(s_files[1]))
            throw new Exception("Found[1] wasn't files[1] when listing *.1!  got: " + found[1]);

        // Try listing all c* files.
        found = dir.GetFiles("c*");
        if (found.Length != 1)
            throw new Exception("When looking for c*, found wrong number!  expected: 1  got: " + found.Length);
        if (!found[0].Name.Equals(s_files[2]))
            throw new Exception("Found[0] wasn't files[2] when listing c*!  got: " + found[0]);

        // Copy then delete a file to make sure it's gone.
        File.Copy(Path.Combine(dir.ToString(), s_files[0]), Path.Combine(dir.FullName, "newfile.new"));
        found = dir.GetFiles("new*.new");
        if (found.Length != 1)
            throw new Exception("Didn't find copied file!");
        if (!found[0].Name.Equals("newfile.new"))
            throw new Exception("Didn't find newfile.new after copy! got: " + found[0]);
        File.Delete(Path.Combine(dirName, "newfile.new"));
        found = dir.GetFiles("new*.new");
        if (found.Length != 0)
            throw new Exception("new file wasn't deleted!  " + found[0]);

        String curDir = Directory.GetCurrentDirectory();
        if (curDir == null)
            throw new Exception("Ack! got null string from get current directory");
        String newDir = Path.Combine(curDir, dirName);
        Directory.SetCurrentDirectory(newDir);
        if (!newDir.Equals(Directory.GetCurrentDirectory()))
            throw new Exception("Ack!  new directory didn't equal getcwd!  " + Directory.GetCurrentDirectory());
        if (!File.Exists(s_files[s_files.Length - 1]))
            throw new Exception("Not in the new directory!  Couldn't find last file!");
        Directory.SetCurrentDirectory(curDir);
        if (!curDir.Equals(Directory.GetCurrentDirectory()))
            throw new Exception("Ack!  old directory didn't equal getcwd!  " + Directory.GetCurrentDirectory());

        Directory.SetCurrentDirectory(newDir);
        // Test CreateDirectories on a directory tree
        Directory.CreateDirectory("a/b/c");
        if (!Directory.Exists("a"))
            throw new Exception("Directory a didn't exist!");
        if (!Directory.Exists("a/b"))
            throw new Exception("Directory a\\b didn't exist!");
        if (!Directory.Exists("a/b/c"))
            throw new Exception("Directory a\\b\\c didn't exist!");

        Directory.Delete("a/b/c");
        // Test creating one directory nested under existing ones
        Directory.CreateDirectory("a/b/c");
        if (!Directory.Exists("a/b/c"))
            throw new Exception("Directory a\\b\\c didn't exist!");

        // Delete "a\b\c\" recursively
        Directory.Delete("a", true);
        if (Directory.Exists("a"))
            throw new Exception("Directory \"a\" still exists!");

        try
        {
            Directory.CreateDirectory(s_files[0]);
        }
        catch (IOException)
        {
            /*
            if (e.ErrorCode != 183) { // File Exists
                Console.WriteLine("CreateDirectories threw: "+e);
                Console.WriteLine(e.ErrorCode);
                throw e;
            }
            */
            // Assume the path exists.
        }

        try
        {
            // SetCurrentDirectory on one that doesn't exist
            Directory.SetCurrentDirectory("a/b");  // doesn't exist
            throw new Exception("Ack!  Set Current Directory to one that doesn't exist!  should have thrown");
        }
        catch (DirectoryNotFoundException)
        {
        }

        // Try FileEnumerator on files[] in the test directory
        /*
        FileEnumerator fe = new FileEnumerator("*");
        // Skip over . and ..
        fe.MoveNext();
        if (fe.Name.Equals(".") && ((fe.Attributes & FileAttributes.Directory) != 0))
            fe.MoveNext();
        if (!fe.Name.Equals(".."))
            throw new Exception("Trying to skip . and .. in FileEnumerator test - not what I expected  "+fe.Name);
        int num = 0;
        Console.WriteLine("\tFileEnumerator Creation, Last Write, and Last Access Times and Size:");
        while(fe.MoveNext()) {
            if (!fe.Name.Equals(files[num]))
                throw new Exception("FileEnumerator name wasn't what was expected!  got: "+fe.Name);
            Console.WriteLine("\t"+fe.CreationTime+"\t"+fe.LastWriteTime+"\t"+fe.LastAccessTime+"  "+fe.Size);
            num++;
        }
        if (num != files.Length)
            throw new Exception("Files.Length wasn't equal number of files enumerated!  got: "+num+"  expected: "+files.Length);
        if (fe.MoveNext())
            throw new Exception("FileEnumerator GetNext returned true after enumeration finished!");
        fe.Close();
        */
        Directory.SetCurrentDirectory(curDir);

        // Delete files.
        for (int i = 0; i < s_files.Length; i++)
        {
            File.Delete(dirName + "/" + s_files[i]);
        }

        dir.Delete();

        Assert.True(s_pass);
    }

    public static void CreateTestFiles()
    {
        for (int i = 0; i < s_files.Length; i++)
        {
            Stream fs = File.Create(s_files[i]);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(s_files[i]);
            bw.Dispose();
        }
    }

    [Fact]
    public static void CreateDirTest()
    {
        String dir = "CreateDirTest";

        try
        {
            Directory.Delete(dir);
        }
        catch (IOException) { }

        Directory.CreateDirectory(dir);


        Directory.Delete(dir);

        Assert.True(s_pass);
    }

    [Fact]
    public static void DeleteDirTest()
    {
        String dir = "DeleteDirTest";

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (IOException)
        {
        }


        try
        {
            Directory.Delete(dir);
            Stream f = File.Create(dir);
            f.Dispose();
            File.Delete(dir);
        }
        catch (IOException io)
        {
            Console.WriteLine("File name: \"" + dir + '\"');
            Console.WriteLine("caught IOException when trying to delete the dir then open file");
            Console.WriteLine(io);
            Console.WriteLine(io.StackTrace);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Got unexpected exception from Delete or surrounding code: " + e);
            Console.WriteLine(e.StackTrace);
            throw;
        }

        Assert.True(s_pass);
    }

    [Fact]
    [ActiveIssue(1220)] // SetCurrentDirectory
    public static void GetSubdirectoriesTest()
    {
        String testDir = "GetSubdirectoriesTempDir";
        if (Directory.Exists(Path.Combine(".", testDir)))
        {
            Console.WriteLine("Test didn't clean up right, trying to delete directory \"" + testDir + "\"");
            try
            {
                Directory.Delete(testDir);
            }
            catch (Exception)
            {
                Console.WriteLine("Ack!  Test couldn't clean up after itself.  Delete .\\\"" + testDir + "\"");
                throw;
            }
        }

        String oldDirectory = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(testDir);
        Directory.SetCurrentDirectory(testDir);

        DirectoryInfo current = new DirectoryInfo(".");
        Directory.CreateDirectory("a1");
        Directory.CreateDirectory("a2");
        current.CreateSubdirectory("b1");
        Directory.CreateDirectory("b2");
        Stream junk = File.Create("c1");
        junk.Dispose();

        try
        {
            DirectoryInfo[] dirs = current.GetDirectories("*");
            if (dirs == null)
                throw new Exception("Directory Names array was NULL!");
            /*
            Console.WriteLine("Directory names: ");
            for(int i=0; i<dirs.Length; i++)
                Console.WriteLine(dirs[i]);
            */

            if (dirs.Length != 4)  // 4 directories + "." & ".." ( looks like . and .. are not returned )
                throw new Exception("Directory names array should have been length 4!  was: " + dirs.Length);

            // Now try wildcards, such as "*1" and "a*"
            dirs = current.GetDirectories("*1");
            if (dirs.Length != 2)
                throw new Exception("Directory names array should have been length 2 when asking for \"*1\", but was: " + dirs.Length);

            dirs = current.GetDirectories("a*");
            if (dirs.Length != 2)
                throw new Exception("Directory names array should have been length 2 after looking for \"a*\", but was: " + dirs.Length);
        }
        catch (Exception)
        {
            Console.WriteLine("Error in GetDirectoryNamesTest - throwing an exception");
            throw;
        }
        finally
        {
            try
            {
                Console.Out.Flush();
                Directory.Delete("a1");
                Directory.Delete("a2");
                Directory.Delete("b1");
                Directory.Delete("b2");
                File.Delete("c1");
                Directory.SetCurrentDirectory(oldDirectory);
                //Directory.Delete(testDir);
                current.Delete(false);  // We've deleted everything else, it should clean up fine.
            }
            catch (Exception ex)
            {
                Console.Write("Ran into error cleaning up GetDirectoriesTest... {0}", ex.ToString());
                throw;
            }
        }

        Assert.True(s_pass);
    }

    private void DeleteFile(String fileName)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);
    }


    //Checks for error
    private static bool Eval(bool expression, String msg, params Object[] values)
    {
        return Eval(expression, String.Format(msg, values));
    }

    private static bool Eval(bool expression, String msg)
    {
        if (!expression)
        {
            s_pass = false;
            Console.WriteLine(msg);
        }
        return expression;
    }

    //A short cut API that doesn't need to repeat the error 
    private static bool Eval<T>(T actual, T expected, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
        {
            String value = String.Format("{0} Expected: {1}, Actual: {2}", errorMsg, (null == expected ? "<null>" : expected.ToString()), (null == actual ? "<null>" : actual.ToString()));
            Eval(retValue, value);
        }

        return retValue;
    }


    //Checks for a particular type of exception
    private static void CheckException<E>(ExceptionCode test, string error, params Object[] values)
    {
        CheckException<E>(test, error, null, values);
    }

    //Checks for a particular type of exception and an Exception msg in the English locale
    private static void CheckException<E>(ExceptionCode test, string error, String msgExpected, params Object[] values)
    {
        bool exception = false;
        String exErrMsg = String.Format(error, values);
        try
        {
            test();
            error = String.Format("{0} Exception NOT thrown ", exErrMsg);
        }
        catch (Exception e)
        {
            if (e.GetType() == typeof(E))
            {
                exception = true;
                if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US" && msgExpected != null && e.Message != msgExpected)
                {
                    exception = false;
                    error = String.Format("{0} Message Different: <{1}>", exErrMsg, e.Message);
                }
            }
            else
                error = String.Format("{0} Exception type: {1}", exErrMsg, e.GetType().Name);
        }
        Eval(exception, error);
    }


    //Checks for a 2 types of exceptions
    private static void CheckException<E, V>(ExceptionCode test, string error)
    {
        bool exception = false;
        try
        {
            test();
            error = String.Format("{0} Exception NOT thrown ", error);
        }
        catch (Exception e)
        {
            if (e.GetType() == typeof(E) || e.GetType() == typeof(V))
            {
                exception = true;
            }
            else
                error = String.Format("{0} Exception type: {1}", error, e.GetType().Name);
        }
        Eval(exception, error);
    }
}
