// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
This testcase attempts to delete some directories in a mounted volume
 - Different drive is mounted on the current drive
 - Current drive is mounted on a different drive
 - Current drive is mounted on current directory
   - refer to the directory in a recursive manner in addition to the normal one
**/
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_Delete_MountVolume
    {
        private const string MountPrefixName = "LaksMount";

        private static bool IsNtfs =>
            FileSystemDebugInfo.IsCurrentDriveNTFS();

        private static bool IsNtfsWithOtherNtfsDrive =>
            FileSystemDebugInfo.IsCurrentDriveNTFS() && IOServices.GetNtfsDriveOtherThanCurrent() != null;

        private static bool HasOtherNtfsDrive =>
            IOServices.GetNtfsDriveOtherThanCurrent() != null;

        [ConditionalFact(nameof(IsNtfsWithOtherNtfsDrive))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
        public static void Scenario1_DifferentDriveMountedOnCurrentDrive()
        {
            string otherDriveInMachine = IOServices.GetNtfsDriveOtherThanCurrent();
            string mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), MountPrefixName));
            try
            {
                Directory.CreateDirectory(mountedDirName);
                MountHelper.Mount(otherDriveInMachine.Substring(0, 2), mountedDirName);

                string dirName = ManageFileSystem.GetNonExistingDir(otherDriveInMachine, ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    Assert.True(Directory.Exists(dirName), $"Err_3974g! Directory {dirName} doesn't exist");
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                    Directory.Delete(dirNameReferredFromMountedDrive, true);
                    Task.Delay(300).Wait();
                    Assert.False(Directory.Exists(dirName), $"Err_20387g! Directory {dirName} still exists");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 4390 or 3) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        [ConditionalFact(nameof(HasOtherNtfsDrive))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
        public static void Scenario2_CurrentDriveMountedOnOtherDrive()
        {
            string otherDriveInMachine = IOServices.GetNtfsDriveOtherThanCurrent();
            string mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(otherDriveInMachine.Substring(0, 3), MountPrefixName));
            try
            {
                Directory.CreateDirectory(mountedDirName);
                MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                string dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    Assert.True(Directory.Exists(dirName), $"Err_239ufz! Directory {dirName} doesn't exist");
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                    Directory.Delete(dirNameReferredFromMountedDrive, true);
                    Task.Delay(300).Wait();
                    Assert.False(Directory.Exists(dirName), $"Err_794aiu! Directory {dirName} still exists");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 4390 or 3) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        [ConditionalFact(nameof(IsNtfs))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
        public static void Scenario31_CurrentDriveMountedOnCurrentDrive()
        {
            string mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), MountPrefixName));
            try
            {
                Directory.CreateDirectory(mountedDirName);
                MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                string dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    Assert.True(Directory.Exists(dirName), $"Err_324eez! Directory {dirName} doesn't exist");
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                    Directory.Delete(dirNameReferredFromMountedDrive, true);
                    Task.Delay(300).Wait();
                    Assert.False(Directory.Exists(dirName), $"Err_195whv! Directory {dirName} still exists");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 4390 or 3) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        [ConditionalFact(nameof(IsNtfs))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
        public static void Scenario32_CurrentDriveMountedOnCurrentDirectory()
        {
            string mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), MountPrefixName));
            try
            {
                Directory.CreateDirectory(mountedDirName);
                MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);

                string dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    Assert.True(Directory.Exists(dirName), $"Err_951ipb! Directory {dirName} doesn't exist");
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);
                    Directory.Delete(dirNameReferredFromMountedDrive, true);
                    Task.Delay(300).Wait();
                    Assert.False(Directory.Exists(dirName), $"Err_493yin! Directory {dirName} still exists");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 4390 or 3) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        // @WATCH - potentially dangerous code - can delete the whole drive!!
        // Scenario 3.3: we call delete on the mounted volume - this should only delete the mounted drive?
        [ConditionalFact(nameof(IsNtfs))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
        public static void Scenario33_DeleteMountedVolume()
        {
            string mountedDirName = Path.GetFullPath(ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), MountPrefixName));
            try
            {
                Directory.CreateDirectory(mountedDirName);
                MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);
                Directory.Delete(mountedDirName, true);
                Task.Delay(300).Wait();
                Assert.False(Directory.Exists(mountedDirName), $"Err_001yph! Directory {mountedDirName} still exists");
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 4390 or 3) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        // @WATCH - potentially dangerous code - can delete the whole drive!!
        // Scenario 3.4: we call delete on parent directory of the mounted volume
        [ConditionalFact(nameof(IsNtfs))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
        public static void Scenario34_DeleteParentOfMountPoint()
        {
            string mountedDirName = null;
            string dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
            try
            {
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 2, 20))
                {
                    Assert.True(Directory.Exists(dirName), $"Err_469yvh! Directory {dirName} doesn't exist");
                    string[] dirs = fileManager.GetDirectories(1);
                    mountedDirName = Path.GetFullPath(dirs[0]);
                    Assert.True(Directory.GetDirectories(mountedDirName).Length == 0, $"Err_974tsg! the sub directory has directories: {mountedDirName}");
                    foreach (string file in Directory.GetFiles(mountedDirName))
                        File.Delete(file);
                    Assert.True(Directory.GetFiles(mountedDirName).Length == 0, $"Err_13ref! the mounted directory has files: {mountedDirName}");
                    MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);
                    Directory.Delete(dirName, true);
                    Task.Delay(300).Wait();
                    Assert.False(Directory.Exists(dirName), $"Err_006jsf! Directory {dirName} still exists");
                    Console.WriteLine("Completed Scenario 3.4");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 4390 or 3) { }
                    DeleteDir(mountedDirName, true);
                }
                DeleteDir(dirName, true);
            }
        }

        // @WATCH - potentially dangerous code - can delete the whole drive!!
        // Scenario 3.5: we call delete on parent directory of the mounted volume (alternate subdirectory)
        [ConditionalFact(nameof(IsNtfs))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing volumes / mounts / drive letters
        public static void Scenario35_DeleteParentOfMountPointAlternateDir()
        {
            string mountedDirName = null;
            string dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
            try
            {
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 2, 30))
                {
                    Assert.True(Directory.Exists(dirName), $"Err_715tdq! Directory {dirName} doesn't exist");
                    string[] dirs = fileManager.GetDirectories(1);
                    mountedDirName = Path.GetFullPath(dirs[0]);
                    if (dirs.Length > 1)
                        mountedDirName = Path.GetFullPath(dirs[1]);
                    Assert.True(Directory.GetDirectories(mountedDirName).Length == 0, $"Err_492qwl! the sub directory has directories: {mountedDirName}");
                    foreach (string file in Directory.GetFiles(mountedDirName))
                        File.Delete(file);
                    Assert.True(Directory.GetFiles(mountedDirName).Length == 0, $"Err_904kij! the mounted directory has files: {mountedDirName}");
                    MountHelper.Mount(Directory.GetCurrentDirectory().Substring(0, 2), mountedDirName);
                    Directory.Delete(dirName, true);
                    Task.Delay(300).Wait();
                    Assert.False(Directory.Exists(dirName), $"Err_900edl! Directory {dirName} still exists");
                    Console.WriteLine("Completed Scenario 3.5: {0}", mountedDirName);
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is 4390 or 3) { }
                    DeleteDir(mountedDirName, true);
                }
                DeleteDir(dirName, true);
            }
        }

        private static void DeleteDir(string path, bool sub)
        {
            bool deleted = false; int maxAttempts = 5;
            while (!deleted && maxAttempts > 0)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, sub);
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
                else
                {
                    deleted = true;
                }
            }
        }
    }
}
