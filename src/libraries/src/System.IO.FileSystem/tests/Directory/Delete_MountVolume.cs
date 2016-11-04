// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**
This testcase attempts to delete some directories in a mounted volume
 - Different drive is mounted on the current drive
 - Current drive is mounted on a different drive
 - Current drive is mounted on current directory
 	 - refer to the directory in a recursive manner in addition to the normal one
**/
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
public class Directory_Delete_MountVolume
{
    private delegate void ExceptionCode();
    private static bool s_pass = true;

    [Fact]
    [ActiveIssue(1221)]
    [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
    public static void RunTest()
    {
        try
        {
            const String MountPrefixName = "LaksMount";

            String mountedDirName;
            String dirName;
            String dirNameWithoutRoot;
            String dirNameReferedFromMountedDrive;

            //Adding debug info since this test hangs sometime in RTS runs
            String debugFileName = "Co7604Delete_MountVolume_Debug.txt";
            DeleteFile(debugFileName);
            String scenarioDescription;

            scenarioDescription = "Scenario 1: Vanilla - Different drive is mounted on the current drive";
            try
            {
                File.AppendAllText(debugFileName, String.Format("{0}{1}", scenarioDescription, Environment.NewLine));

                string otherDriveInMachine = IOServices.GetNtfsDriveOtherThanCurrent();
                //out labs use UIP tools in one drive and don't expect this drive to be used by others. We avoid this problem by not testing if the other drive is not NTFS
                if (FileSystemDebugInfo.IsCurrentDriveNTFS() && otherDriveInMachine != null)
                {
                    Console.WriteLine(scenarioDescription);
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), MountPrefixName));

                    try
                    {
                        Directory.CreateDirectory(mountedDirName);

                        File.AppendAllText(debugFileName, String.Format("Mounting on {0}{1}{2}", otherDriveInMachine.Substring(0, 2), mountedDirName, Environment.NewLine));
                        MountHelper.Mount(otherDriveInMachine.Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(otherDriveInMachine, ManageFileSystem.DirPrefixName);
                        File.AppendAllText(debugFileName, String.Format("Creating a sub tree at: {0}{1}", dirName, Environment.NewLine));
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            Eval(Directory.Exists(dirName), "Err_3974g! Directory {0} doesn't exist: {1}", dirName, Directory.Exists(dirName));
                            //Lets refer to these via mounted drive and check
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                            Directory.Delete(dirNameReferedFromMountedDrive, true);
                            Task.Delay(300).Wait();
                            Eval(!Directory.Exists(dirName), "Err_20387g! Directory {0} still exist: {1}", dirName, Directory.Exists(dirName));
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                    File.AppendAllText(debugFileName, String.Format("Completed scenario {0}", Environment.NewLine));
                }
                else
                    File.AppendAllText(debugFileName, String.Format("Scenario 1 - Vanilla - NOT RUN: Different drive is mounted on the current drive {0}", Environment.NewLine));
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_768lme! Exception caught in scenario: {0}", ex);
            }

            scenarioDescription = "Scenario 2: Current drive is mounted on a different drive";
            Console.WriteLine(scenarioDescription);
            File.AppendAllText(debugFileName, String.Format("{0}{1}", scenarioDescription, Environment.NewLine));
            try
            {
                string otherDriveInMachine = IOServices.GetNtfsDriveOtherThanCurrent();
                if (otherDriveInMachine != null)
                {
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(otherDriveInMachine.Substring(0, 3), MountPrefixName));
                    try
                    {
                        Directory.CreateDirectory(mountedDirName);

                        File.AppendAllText(debugFileName, String.Format("Mounting on {0}{1}{2}", Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName, Environment.NewLine));
                        MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        File.AppendAllText(debugFileName, String.Format("Creating a sub tree at: {0}{1}", dirName, Environment.NewLine));
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            Eval(Directory.Exists(dirName), "Err_239ufz! Directory {0} doesn't exist: {1}", dirName, Directory.Exists(dirName));
                            //Lets refer to these via mounted drive and check
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                            Directory.Delete(dirNameReferedFromMountedDrive, true);
                            Task.Delay(300).Wait();
                            Eval(!Directory.Exists(dirName), "Err_794aiu! Directory {0} still exist: {1}", dirName, Directory.Exists(dirName));
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                    File.AppendAllText(debugFileName, String.Format("Completed scenario {0}", Environment.NewLine));
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_231vwf! Exception caught in scenario: {0}", ex);
            }

            //scenario 3.1: Current drive is mounted on current drive
            scenarioDescription = "Scenario 3.1 - Current drive is mounted on current drive";
            try
            {
                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                {
                    File.AppendAllText(debugFileName, String.Format("{0}{1}", scenarioDescription, Environment.NewLine));
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), MountPrefixName));
                    try
                    {
                        Directory.CreateDirectory(mountedDirName);

                        File.AppendAllText(debugFileName, String.Format("Mounting on {0}{1}{2}", Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName, Environment.NewLine));
                        MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        File.AppendAllText(debugFileName, String.Format("Creating a sub tree at: {0}{1}", dirName, Environment.NewLine));
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            Eval(Directory.Exists(dirName), "Err_324eez! Directory {0} doesn't exist: {1}", dirName, Directory.Exists(dirName));
                            //Lets refer to these via mounted drive and check
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                            Directory.Delete(dirNameReferedFromMountedDrive, true);
                            Task.Delay(300).Wait();
                            Eval(!Directory.Exists(dirName), "Err_195whv! Directory {0} still exist: {1}", dirName, Directory.Exists(dirName));
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                    File.AppendAllText(debugFileName, String.Format("Completed scenario {0}", Environment.NewLine));
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_493ojg! Exception caught in scenario: {0}", ex);
            }

            //scenario 3.2: Current drive is mounted on current directory
            scenarioDescription = "Scenario 3.2 - Current drive is mounted on current directory";
            try
            {
                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                {
                    File.AppendAllText(debugFileName, String.Format("{0}{1}", scenarioDescription, Environment.NewLine));
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), MountPrefixName));
                    try
                    {
                        Directory.CreateDirectory(mountedDirName);

                        File.AppendAllText(debugFileName, String.Format("Mounting on {0}{1}{2}", Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName, Environment.NewLine));
                        MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        File.AppendAllText(debugFileName, String.Format("Creating a sub tree at: {0}{1}", dirName, Environment.NewLine));
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                        {
                            Eval(Directory.Exists(dirName), "Err_951ipb! Directory {0} doesn't exist: {1}", dirName, Directory.Exists(dirName));
                            //Lets refer to these via mounted drive and check
                            dirNameWithoutRoot = dirName.Substring(3);
                            dirNameReferedFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                            Directory.Delete(dirNameReferedFromMountedDrive, true);
                            Task.Delay(300).Wait();
                            Eval(!Directory.Exists(dirName), "Err_493yin! Directory {0} still exist: {1}", dirName, Directory.Exists(dirName));
                        }
                    }
                    finally
                    {
                        MountHelper.Unmount(mountedDirName);
                        DeleteDir(mountedDirName, true);
                    }
                    File.AppendAllText(debugFileName, String.Format("Completed scenario {0}", Environment.NewLine));
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_432qcp! Exception caught in scenario: {0}", ex);
            }

            //@WATCH - potentially dangerous code - can delete the whole drive!!
            //scenario 3.3: we call delete on the mounted volume - this should only delete the mounted drive?
            //Current drive is mounted on current directory
            scenarioDescription = "Scenario 3.3 - we call delete on the mounted volume";
            try
            {
                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                {
                    File.AppendAllText(debugFileName, String.Format("{0}{1}", scenarioDescription, Environment.NewLine));
                    mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), MountPrefixName));
                    try
                    {
                        Directory.CreateDirectory(mountedDirName);

                        File.AppendAllText(debugFileName, String.Format("Mounting on {0}{1}{2}", Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName, Environment.NewLine));
                        MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                        Directory.Delete(mountedDirName, true);
                        Task.Delay(300).Wait();
                    }
                    finally
                    {
                        if (!Eval(!Directory.Exists(mountedDirName), "Err_001yph! Directory {0} still exist: {1}", mountedDirName, Directory.Exists(mountedDirName)))
                        {
                            MountHelper.Unmount(mountedDirName);
                            DeleteDir(mountedDirName, true);
                        }
                    }
                    File.AppendAllText(debugFileName, String.Format("Completed scenario {0}", Environment.NewLine));
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_386rpj! Exception caught in scenario: {0}", ex);
            }

            //@WATCH - potentially dangerous code - can delete the whole drive!!
            //scenario 3.4: we call delete on parent directory of the mounted volume, the parent directory will have some other directories and files
            //Current drive is mounted on current directory
            scenarioDescription = "Scenario 3.4 - we call delete on parent directory of the mounted volume, the parent directory will have some other directories and files";
            try
            {
                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                {
                    File.AppendAllText(debugFileName, String.Format("{0}{1}", scenarioDescription, Environment.NewLine));
                    mountedDirName = null;
                    try
                    {
                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        File.AppendAllText(debugFileName, String.Format("Creating a sub tree at: {0}{1}", dirName, Environment.NewLine));
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 2, 20))
                        {
                            Eval(Directory.Exists(dirName), "Err_469yvh! Directory {0} doesn't exist: {1}", dirName, Directory.Exists(dirName));
                            String[] dirs = fileManager.GetDirectories(1);
                            mountedDirName = Path.GetFullPath(dirs[0]);
                            if (Eval(Directory.GetDirectories(mountedDirName).Length == 0, "Err_974tsg! the sub directory has directories: {0}", mountedDirName))
                            {
                                foreach (String file in Directory.GetFiles(mountedDirName))
                                    File.Delete(file);
                                if (Eval(Directory.GetFiles(mountedDirName).Length == 0, "Err_13ref! the mounted directory has files: {0}", mountedDirName))
                                {
                                    File.AppendAllText(debugFileName, String.Format("Mounting on {0}{1}{2}", Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName, Environment.NewLine));
                                    MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);
                                    //now lets call delete on the parent directory
                                    Directory.Delete(dirName, true);
                                    Task.Delay(300).Wait();
                                    Eval(!Directory.Exists(dirName), "Err_006jsf! Directory {0} still exist: {1}", dirName, Directory.Exists(dirName));
                                    Console.WriteLine("Completed Scenario 3.4");
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (!Eval(!Directory.Exists(mountedDirName), "Err_625ckx! Directory {0} still exist: {1}", mountedDirName, Directory.Exists(mountedDirName)))
                        {
                            MountHelper.Unmount(mountedDirName);
                            DeleteDir(mountedDirName, true);
                        }
                    }
                    File.AppendAllText(debugFileName, String.Format("Completed scenario {0}", Environment.NewLine));
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_578tni! Exception caught in scenario: {0}", ex);
            }

            //@WATCH - potentially dangerous code - can delete the whole drive!!
            //scenario 3.5: we call delete on parent directory of the mounted volume, the parent directory will have some other directories and files
            //we call a different directory than the first
            //Current drive is mounted on current directory
            scenarioDescription = "Scenario 3.5 - we call delete on parent directory of the mounted volume, the parent directory will have some other directories and files";
            try
            {
                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                {
                    File.AppendAllText(debugFileName, String.Format("{0}{1}", scenarioDescription, Environment.NewLine));
                    mountedDirName = null;
                    try
                    {
                        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                        File.AppendAllText(debugFileName, String.Format("Creating a sub tree at: {0}{1}", dirName, Environment.NewLine));
                        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 2, 30))
                        {
                            Eval(Directory.Exists(dirName), "Err_715tdq! Directory {0} doesn't exist: {1}", dirName, Directory.Exists(dirName));
                            String[] dirs = fileManager.GetDirectories(1);
                            mountedDirName = Path.GetFullPath(dirs[0]);
                            if (dirs.Length > 1)
                                mountedDirName = Path.GetFullPath(dirs[1]);
                            if (Eval(Directory.GetDirectories(mountedDirName).Length == 0, "Err_492qwl! the sub directory has directories: {0}", mountedDirName))
                            {
                                foreach (String file in Directory.GetFiles(mountedDirName))
                                    File.Delete(file);
                                if (Eval(Directory.GetFiles(mountedDirName).Length == 0, "Err_904kij! the mounted directory has files: {0}", mountedDirName))
                                {
                                    File.AppendAllText(debugFileName, String.Format("Mounting on {0}{1}{2}", Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName, Environment.NewLine));
                                    MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);
                                    //now lets call delete on the parent directory
                                    Directory.Delete(dirName, true);
                                    Task.Delay(300).Wait();
                                    Eval(!Directory.Exists(dirName), "Err_900edl! Directory {0} still exist: {1}", dirName, Directory.Exists(dirName));
                                    Console.WriteLine("Completed Scenario 3.5: {0}", mountedDirName);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (!Eval(!Directory.Exists(mountedDirName), "Err_462xtc! Directory {0} still exist: {1}", mountedDirName, Directory.Exists(mountedDirName)))
                        {
                            MountHelper.Unmount(mountedDirName);
                            DeleteDir(mountedDirName, true);
                        }
                    }
                    File.AppendAllText(debugFileName, String.Format("Completed scenario {0}", Environment.NewLine));
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_471jli! Exception caught in scenario: {0}", ex);
            }
        }
        catch (Exception ex)
        {
            s_pass = false;
            Console.WriteLine("Err_234rsgf! Uncaught exception in RunTest: {0}", ex);
        }
        finally
        {
            Assert.True(s_pass);
        }
    }

    private static void DeleteFile(String debugFileName)
    {
        if (File.Exists(debugFileName))
            File.Delete(debugFileName);
    }

    private static void DeleteDir(String debugFileName, bool sub)
    {
        bool deleted = false; int maxAttempts = 5;
        while (!deleted && maxAttempts > 0)
        {
            if (Directory.Exists(debugFileName))
            {
                try
                {
                    Directory.Delete(debugFileName, sub);
                    deleted = true;
                }
                catch (Exception)
                {
                    if (--maxAttempts == 0)
                        throw;
                    else
                        Task.Delay(300).Wait();
                }
            }
        }
    }

    //Checks for error
    private static bool Eval(bool expression, String msg, params Object[] values)
    {
        return Eval(expression, String.Format(msg, values));
    }

    private static bool Eval<T>(T actual, T expected, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            Eval(retValue, errorMsg +
            " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
            " Actual:" + (null == actual ? "<null>" : actual.ToString()));

        return retValue;
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

    //Checks for a particular type of exception
    private static void CheckException<E>(ExceptionCode test, string error)
    {
        CheckException<E>(test, error, null);
    }

    //Checks for a particular type of exception and an Exception msg in the English locale
    private static void CheckException<E>(ExceptionCode test, string error, String msgExpected)
    {
        bool exception = false;
        try
        {
            test();
            error = String.Format("{0} Exception NOT thrown ", error);
        }
        catch (Exception e)
        {
            if (e.GetType() == typeof(E))
            {
                exception = true;
                if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US" && msgExpected != null && e.Message != msgExpected)
                {
                    exception = false;
                    error = String.Format("{0} Message Different: <{1}>", error, e.Message);
                }
            }
            else
                error = String.Format("{0} Exception type: {1}", error, e.GetType().Name);
        }
        Eval(exception, error);
    }
}
