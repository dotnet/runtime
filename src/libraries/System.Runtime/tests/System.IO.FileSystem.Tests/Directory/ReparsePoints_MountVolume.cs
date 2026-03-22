// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
This testcase attempts to checks GetDirectories/GetFiles with the following ReparsePoint implementations
 - Mount Volumes
**/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_ReparsePoints_MountVolume
    {
        private const string MountPrefixName = "LaksMount";
        private const int ErrorPathNotFound = 3;
        private const int ErrorNotAReparsePoint = 4390;

        private static bool IsNtfs =>
            FileSystemDebugInfo.IsCurrentDriveNTFS();

        private static bool IsNtfsWithOtherNtfsDrive =>
            FileSystemDebugInfo.IsCurrentDriveNTFS() && IOServices.GetNtfsDriveOtherThanCurrent() != null;

        private static bool HasOtherNtfsDrive =>
            IOServices.GetNtfsDriveOtherThanCurrent() != null;

        [ConditionalFact(nameof(IsNtfsWithOtherNtfsDrive))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing mounting volumes and reparse points
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
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);

                    // Files
                    string[] expectedFiles = fileManager.GetAllFiles();
                    List<string> list = new List<string>();
                    foreach (string file in expectedFiles)
                        list.Add(Path.GetFileName(file));
                    string[] files = Directory.GetFiles(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(files.Length == list.Count, $"Err_3947g! wrong count: expected {list.Count} got {files.Length}");
                    for (int i = 0; i < files.Length; i++)
                    {
                        Assert.True(list.Contains(Path.GetFileName(files[i])), $"Err_582bmw! No file found: {files[i]}");
                        list.Remove(Path.GetFileName(files[i]));
                    }
                    Assert.True(list.Count == 0, $"Err_891vut! wrong count: {list.Count}\n{string.Join("\n", list)}");

                    // Directories
                    string[] expectedDirs = fileManager.GetAllDirectories();
                    list = new List<string>();
                    foreach (string dir in expectedDirs)
                        list.Add(dir.Substring(dirName.Length));
                    string[] dirs = Directory.GetDirectories(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(dirs.Length == list.Count, $"Err_813weq! wrong count: expected {list.Count} got {dirs.Length}");
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        string exDir = dirs[i].Substring(dirNameReferredFromMountedDrive.Length);
                        Assert.True(list.Contains(exDir), $"Err_287kkm! No dir found: {exDir}");
                        list.Remove(exDir);
                    }
                    Assert.True(list.Count == 0, $"Err_921mhs! wrong count: {list.Count}\n{string.Join("\n", list)}");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is ErrorNotAReparsePoint or ErrorPathNotFound) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        [ConditionalFact(nameof(HasOtherNtfsDrive))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing mounting volumes and reparse points
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
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);

                    // Files
                    string[] expectedFiles = fileManager.GetAllFiles();
                    List<string> list = new List<string>();
                    foreach (string file in expectedFiles)
                        list.Add(Path.GetFileName(file));
                    string[] files = Directory.GetFiles(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(files.Length == list.Count, $"Err_689myg! wrong count: expected {list.Count} got {files.Length}");
                    for (int i = 0; i < files.Length; i++)
                    {
                        Assert.True(list.Contains(Path.GetFileName(files[i])), $"Err_894vhm! No file found: {files[i]}");
                        list.Remove(Path.GetFileName(files[i]));
                    }
                    Assert.True(list.Count == 0, $"Err_952qkj! wrong count: {list.Count}\n{string.Join("\n", list)}");

                    // Directories
                    string[] expectedDirs = fileManager.GetAllDirectories();
                    list = new List<string>();
                    foreach (string dir in expectedDirs)
                        list.Add(dir.Substring(dirName.Length));
                    string[] dirs = Directory.GetDirectories(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(dirs.Length == list.Count, $"Err_154vrz! wrong count: expected {list.Count} got {dirs.Length}");
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        string exDir = dirs[i].Substring(dirNameReferredFromMountedDrive.Length);
                        Assert.True(list.Contains(exDir), $"Err_301sao! No dir found: {exDir}");
                        list.Remove(exDir);
                    }
                    Assert.True(list.Count == 0, $"Err_630gjj! wrong count: {list.Count}\n{string.Join("\n", list)}");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is ErrorNotAReparsePoint or ErrorPathNotFound) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        [ConditionalFact(nameof(IsNtfs))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing mounting volumes and reparse points
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
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);

                    // Files
                    string[] expectedFiles = fileManager.GetAllFiles();
                    List<string> list = new List<string>();
                    foreach (string file in expectedFiles)
                        list.Add(Path.GetFileName(file));
                    string[] files = Directory.GetFiles(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(files.Length == list.Count, $"Err_213fuo! wrong count: expected {list.Count} got {files.Length}");
                    for (int i = 0; i < files.Length; i++)
                    {
                        Assert.True(list.Contains(Path.GetFileName(files[i])), $"Err_499oxz! No file found: {files[i]}");
                        list.Remove(Path.GetFileName(files[i]));
                    }
                    Assert.True(list.Count == 0, $"Err_301gtz! wrong count: {list.Count}\n{string.Join("\n", list)}");

                    // Directories
                    string[] expectedDirs = fileManager.GetAllDirectories();
                    list = new List<string>();
                    foreach (string dir in expectedDirs)
                        list.Add(dir.Substring(dirName.Length));
                    string[] dirs = Directory.GetDirectories(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(dirs.Length == list.Count, $"Err_771dxv! wrong count: expected {list.Count} got {dirs.Length}");
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        string exDir = dirs[i].Substring(dirNameReferredFromMountedDrive.Length);
                        Assert.True(list.Contains(exDir), $"Err_315jey! No dir found: {exDir}");
                        list.Remove(exDir);
                    }
                    Assert.True(list.Count == 0, $"Err_424opm! wrong count: {list.Count}\n{string.Join("\n", list)}");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is ErrorNotAReparsePoint or ErrorPathNotFound) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        [ConditionalFact(nameof(IsNtfs))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing mounting volumes and reparse points
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
                    string dirNameWithoutRoot = dirName.Substring(3);
                    string dirNameReferredFromMountedDrive = Path.Combine(mountedDirName, dirNameWithoutRoot);

                    // Files
                    string[] expectedFiles = fileManager.GetAllFiles();
                    List<string> list = new List<string>();
                    foreach (string file in expectedFiles)
                        list.Add(Path.GetFileName(file));
                    string[] files = Directory.GetFiles(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(files.Length == list.Count, $"Err_253yit! wrong count: expected {list.Count} got {files.Length}");
                    for (int i = 0; i < files.Length; i++)
                    {
                        Assert.True(list.Contains(Path.GetFileName(files[i])), $"Err_798mjs! No file found: {files[i]}");
                        list.Remove(Path.GetFileName(files[i]));
                    }
                    Assert.True(list.Count == 0, $"Err_141lgl! wrong count: {list.Count}\n{string.Join("\n", list)}");

                    // Directories
                    string[] expectedDirs = fileManager.GetAllDirectories();
                    list = new List<string>();
                    foreach (string dir in expectedDirs)
                        list.Add(dir.Substring(dirName.Length));
                    string[] dirs = Directory.GetDirectories(dirNameReferredFromMountedDrive, "*.*", SearchOption.AllDirectories);
                    Assert.True(dirs.Length == list.Count, $"Err_512oxq! wrong count: expected {list.Count} got {dirs.Length}");
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        string exDir = dirs[i].Substring(dirNameReferredFromMountedDrive.Length);
                        Assert.True(list.Contains(exDir), $"Err_907zbr! No dir found: {exDir}");
                        list.Remove(exDir);
                    }
                    Assert.True(list.Count == 0, $"Err_574raf! wrong count: {list.Count}\n{string.Join("\n", list)}");
                }
            }
            finally
            {
                if (Directory.Exists(mountedDirName))
                {
                    try { MountHelper.Unmount(mountedDirName); }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is ErrorNotAReparsePoint or ErrorPathNotFound) { }
                    DeleteDir(mountedDirName, true);
                }
            }
        }

        private static void DeleteDir(string fileName, bool sub)
        {
            if (Directory.Exists(fileName))
                Directory.Delete(fileName, sub);
        }
    }
}
