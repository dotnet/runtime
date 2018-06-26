// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**
This testcase attempts to checks GetDirectories/GetFiles with the following ReparsePoint implementations
 - Mount Volumes
**/
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

public class Directory_ReparsePoints_MountVolume
{
    private delegate void ExceptionCode();
    private static bool s_pass = true;

    [Fact]
    [ActiveIssue(1221)]
    [PlatformSpecific(TestPlatforms.Windows)] // testing mounting volumes and reparse points
    public static void runTest()
    {
        try
        {
            Stopwatch watch;

            const string MountPrefixName = "LaksMount";

            string mountedDirName;
            string dirNameWithoutRoot;
            string dirNameReferedFromMountedDrive;
            string dirName;
            string[] expectedFiles;
            string[] files;
            string[] expectedDirs;
            string[] dirs;
            List<string> list;

            watch = new Stopwatch();
            watch.Start();
            try
            {
                //Scenario 1: Vanilla - Different drive is mounted on the current drive
                Console.WriteLine("Scenario 1 - Vanilla: Different drive is mounted on the current drive: {0}", watch.Elapsed);

                string otherDriveInMachine = IOServices.GetNtfsDriveOtherThanCurrent();
                if (FileSystemDebugInfo.IsCurrentDriveNTFS() && otherDriveInMachine != null)
                {
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), MountPrefixName));
                    try
                    {
                        Console.WriteLine("Creating directory " + mountedDirName);
                        Directory.CreateDirectory(mountedDirName);
                        MountHelper.Mount(otherDriveInMachine.Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(otherDriveInMachine, ManageFileSystem.DirPrefixName);
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);

                            //Files
                            expectedFiles = fileManager.GetAllFiles();
                            list = new List<string>();
                            //We will only test the filenames since they are unique
                            foreach (string file in expectedFiles)
                                list.Add(Path.GetFileName(file));
                            files = Directory.GetFiles(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(files.Length == list.Count, "Err_3947g! wrong count");
                            for (int i = 0; i < expectedFiles.Length; i++)
                            {
                                if (Eval(list.Contains(Path.GetFileName(files[i])), "Err_582bmw! No file found: {0}", files[i]))
                                    list.Remove(Path.GetFileName(files[i]));
                            }
                            if (!Eval(list.Count == 0, "Err_891vut! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string fileName in list)
                                    Console.WriteLine(fileName);
                            }

                            //Directories
                            expectedDirs = fileManager.GetAllDirectories();
                            list = new List<string>();
                            foreach (string dir in expectedDirs)
                                list.Add(dir.Substring(dirName.Length));
                            dirs = Directory.GetDirectories(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(dirs.Length == list.Count, "Err_813weq! wrong count");
                            for (int i = 0; i < dirs.Length; i++)
                            {
                                string exDir = dirs[i].Substring(dirNameReferedFromMountedDrive.Length);
                                if (Eval(list.Contains(exDir), "Err_287kkm! No file found: {0}", exDir))
                                    list.Remove(exDir);
                            }
                            if (!Eval(list.Count == 0, "Err_921mhs! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string value in list)
                                    Console.WriteLine(value);
                            }
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                }
                else
                {
                    Console.WriteLine("Skipping since drive is not NTFS and there is no other drive on the machine");
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_768lme! Exception caught in scenario: {0}", ex);
            }

            //Scenario 2: Current drive is mounted on a different drive
            Console.WriteLine(Environment.NewLine + "Scenario 2 - Current drive is mounted on a different drive: {0}", watch.Elapsed);
            try
            {
                string otherDriveInMachine = IOServices.GetNtfsDriveOtherThanCurrent();
                if (otherDriveInMachine != null)
                {
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(otherDriveInMachine.Substring(0, 3), MountPrefixName));
                    try
                    {
                        Directory.CreateDirectory(mountedDirName);

                        MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                            //Files
                            expectedFiles = fileManager.GetAllFiles();
                            list = new List<string>();
                            //We will only test the filenames since they are unique
                            foreach (string file in expectedFiles)
                                list.Add(Path.GetFileName(file));
                            files = Directory.GetFiles(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(files.Length == list.Count, "Err_689myg! wrong count");
                            for (int i = 0; i < expectedFiles.Length; i++)
                            {
                                if (Eval(list.Contains(Path.GetFileName(files[i])), "Err_894vhm! No file found: {0}", files[i]))
                                    list.Remove(Path.GetFileName(files[i]));
                            }
                            if (!Eval(list.Count == 0, "Err_952qkj! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string fileName in list)
                                    Console.WriteLine(fileName);
                            }

                            //Directories
                            expectedDirs = fileManager.GetAllDirectories();
                            list = new List<string>();
                            foreach (string dir in expectedDirs)
                                list.Add(dir.Substring(dirName.Length));
                            dirs = Directory.GetDirectories(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(dirs.Length == list.Count, "Err_154vrz! wrong count");
                            for (int i = 0; i < dirs.Length; i++)
                            {
                                string exDir = dirs[i].Substring(dirNameReferedFromMountedDrive.Length);
                                if (Eval(list.Contains(exDir), "Err_301sao! No file found: {0}", exDir))
                                    list.Remove(exDir);
                            }
                            if (!Eval(list.Count == 0, "Err_630gjj! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string value in list)
                                    Console.WriteLine(value);
                            }
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                }
                else
                {
                    Console.WriteLine("Skipping since drive is not NTFS and there is no other drive on the machine");
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_231vwf! Exception caught in scenario: {0}", ex);
            }

            //scenario 3.1: Current drive is mounted on current drive
            Console.WriteLine(Environment.NewLine + "Scenario 3.1 - Current drive is mounted on current drive: {0}", watch.Elapsed);
            try
            {
                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                {
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), MountPrefixName));
                    try
                    {
                        Directory.CreateDirectory(mountedDirName);
                        MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                            //Files
                            expectedFiles = fileManager.GetAllFiles();
                            list = new List<string>();
                            //We will only test the filenames since they are unique
                            foreach (string file in expectedFiles)
                                list.Add(Path.GetFileName(file));
                            files = Directory.GetFiles(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(files.Length == list.Count, "Err_213fuo! wrong count");
                            for (int i = 0; i < expectedFiles.Length; i++)
                            {
                                if (Eval(list.Contains(Path.GetFileName(files[i])), "Err_499oxz! No file found: {0}", files[i]))
                                    list.Remove(Path.GetFileName(files[i]));
                            }
                            if (!Eval(list.Count == 0, "Err_301gtz! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string fileName in list)
                                    Console.WriteLine(fileName);
                            }

                            //Directories
                            expectedDirs = fileManager.GetAllDirectories();
                            list = new List<string>();
                            foreach (string dir in expectedDirs)
                                list.Add(dir.Substring(dirName.Length));
                            dirs = Directory.GetDirectories(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(dirs.Length == list.Count, "Err_771dxv! wrong count");
                            for (int i = 0; i < dirs.Length; i++)
                            {
                                string exDir = dirs[i].Substring(dirNameReferedFromMountedDrive.Length);
                                if (Eval(list.Contains(exDir), "Err_315jey! No file found: {0}", exDir))
                                    list.Remove(exDir);
                            }
                            if (!Eval(list.Count == 0, "Err_424opm! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string value in list)
                                    Console.WriteLine(value);
                            }
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                }
                else
                {
                    Console.WriteLine("Drive is not NTFS. Skipping scenario");
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_493ojg! Exception caught in scenario: {0}", ex);
            }

            //scenario 3.2: Current drive is mounted on current directory
            Console.WriteLine(Environment.NewLine + "Scenario 3.2 - Current drive is mounted on current directory: {0}", watch.Elapsed);
            try
            {
                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                {
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), MountPrefixName));
                    try
                    {
                        Directory.CreateDirectory(mountedDirName);
                        MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                            //Files
                            expectedFiles = fileManager.GetAllFiles();
                            list = new List<string>();
                            //We will only test the filenames since they are unique
                            foreach (string file in expectedFiles)
                                list.Add(Path.GetFileName(file));
                            files = Directory.GetFiles(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(files.Length == list.Count, "Err_253yit! wrong count");
                            for (int i = 0; i < expectedFiles.Length; i++)
                            {
                                if (Eval(list.Contains(Path.GetFileName(files[i])), "Err_798mjs! No file found: {0}", files[i]))
                                    list.Remove(Path.GetFileName(files[i]));
                            }
                            if (!Eval(list.Count == 0, "Err_141lgl! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string fileName in list)
                                    Console.WriteLine(fileName);
                            }

                            //Directories
                            expectedDirs = fileManager.GetAllDirectories();
                            list = new List<string>();
                            foreach (string dir in expectedDirs)
                                list.Add(dir.Substring(dirName.Length));
                            dirs = Directory.GetDirectories(dirNameReferedFromMountedDrive, "*.*", SearchOption.AllDirectories);
                            Eval(dirs.Length == list.Count, "Err_512oxq! wrong count");
                            for (int i = 0; i < dirs.Length; i++)
                            {
                                string exDir = dirs[i].Substring(dirNameReferedFromMountedDrive.Length);
                                if (Eval(list.Contains(exDir), "Err_907zbr! No file found: {0}", exDir))
                                    list.Remove(exDir);
                            }
                            if (!Eval(list.Count == 0, "Err_574raf! wrong count: {0}", list.Count))
                            {
                                Console.WriteLine();
                                foreach (string value in list)
                                    Console.WriteLine(value);
                            }
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                }
                else
                {
                    Console.WriteLine("Drive is not NTFS. Skipping scenario");
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_432qcp! Exception caught in scenario: {0}", ex);
            }

            Console.WriteLine("Completed {0}", watch.Elapsed);
        }
        catch (Exception ex)
        {
            s_pass = false;
            Console.WriteLine("Err_234rsgf! Uncaught exception in RunTest: {0}", ex);
        }

        Assert.True(s_pass);
    }

    private static void DeleteFile(string fileName)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);
    }

    private static void DeleteDir(string fileName, bool sub)
    {
        if (Directory.Exists(fileName))
            Directory.Delete(fileName, sub);
    }

    //Checks for error
    private static bool Eval(bool expression, string msg, params object[] values)
    {
        return Eval(expression, string.Format(msg, values));
    }

    private static bool Eval<T>(T actual, T expected, string errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            Eval(retValue, errorMsg +
            " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
            " Actual:" + (null == actual ? "<null>" : actual.ToString()));

        return retValue;
    }

    private static bool Eval(bool expression, string msg)
    {
        if (!expression)
        {
            s_pass = false;
            Console.WriteLine(msg);
        }
        return expression;
    }

    //Checks for a particular type of exception
    private static void CheckException<E>(ExceptionCode test, string error)
    {
        CheckException<E>(test, error, null);
    }

    //Checks for a particular type of exception and an Exception msg in the English locale
    private static void CheckException<E>(ExceptionCode test, string error, string msgExpected)
    {
        bool exception = false;
        try
        {
            test();
            error = string.Format("{0} Exception NOT thrown ", error);
        }
        catch (Exception e)
        {
            if (e.GetType() == typeof(E))
            {
                exception = true;
                if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US" && msgExpected != null && e.Message != msgExpected)
                {
                    exception = false;
                    error = string.Format("{0} Message Different: <{1}>", error, e.Message);
                }
            }
            else
                error = string.Format("{0} Exception type: {1}", error, e.GetType().Name);
        }
        Eval(exception, error);
    }
}





