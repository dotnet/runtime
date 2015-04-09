// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Security;
using System.Globalization;
using Xunit;

public class Directory_GetFiles_str_str_so
{
    private delegate void ExceptionCode();
    private static bool s_pass = true;

    [Fact]
    public static void runTest()
    {
        try
        {
            String dirName;
            String[] expectedFiles;
            String[] files;
            List<String> list;
            // part I - SearchOption.TopDirectoryOnly
            //Scenario 1:Vanilla - Create a directory, add a few files and call with searchPattern *.* and verify 
            //that all files are returned.
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
                {
                    expectedFiles = fileManager.GetFiles(dirName, 0);
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_3947g! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_582bmw! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_891vut! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_349t7g! Exception caught in scenario: {0}", ex);
            }

            //Scenario 2: Add some directories to the vanilla scenario and ensure that these are not returned
            //Scenario 3: Ensure that the path contains subdirectories and call this API and ensure that only the top directory files are returned
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedFiles = fileManager.GetFiles(dirName, 0);
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_763pjg! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_512kvk! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_839rbd! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_997pvx! Exception caught in scenario: {0}", ex);
            }

            // Path is not in the current directory (same drive)
            //Scenario 4: Ensure that the path contains subdirectories and call this API and ensure that only the top directory files are returned
            /* Scenario disabled when porting because it modifies the filesystem outside of the test's working directory
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedFiles = fileManager.GetFiles(dirName, 0);
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_386gef! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        //This will return as \<dirName>\<fileName> whereas our utility will return as <drive>:\<dirName>\<fileName>
                        String fileFullName = Path.GetFullPath(files[i]);
                        if (Eval(list.Contains(fileFullName), "Err_932izm! unexpected file found: {0}", fileFullName))
                            list.Remove(fileFullName);
                    }
                    if (!Eval(list.Count == 0, "Err_915sae! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
                //only 1 level
                dirName = ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), ManageFileSystem.DirPrefixName);
                dirName = Path.GetFullPath(dirName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
                {
                    expectedFiles = fileManager.GetFiles(dirName, 0);
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_792ifb! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_281tff! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_792qdn! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_992hic! Exception caught in scenario: {0}", ex);
            }
            */

            //Scenario 5: searchPattern variations - valid search characters, file match exactly the searchPattern, searchPattern is a subset of existing files, superset, no match, 
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                String searchPattern;
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedFiles = fileManager.GetFiles(dirName, 0);
                    //?
                    int maxLen = 0;
                    foreach (String file in expectedFiles)
                    {
                        String realFile = Path.GetFileNameWithoutExtension(file);
                        if (realFile.Length > maxLen)
                            maxLen = realFile.Length;
                    }
                    searchPattern = String.Format("{0}.???", new String('?', maxLen));
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_488sjb! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_750dop! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_629dvi! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //file match exactly 
                    searchPattern = Path.GetFileName(expectedFiles[0]);
                    list = new List<String>(new String[] { expectedFiles[0] });
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_841dnz! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_796xxd! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_552puh! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //subset
                    String tempSearchPattern = Path.GetFileName(expectedFiles[0]).Substring(2);
                    List<String> newFiles = new List<String>();
                    foreach (String file in expectedFiles)
                    {
                        String realFile = Path.GetFileName(file);
                        if (realFile.Substring(2).Equals(tempSearchPattern))
                            newFiles.Add(file);
                    }
                    searchPattern = String.Format("??{0}", tempSearchPattern);

                    list = newFiles;
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_847vxz! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_736kfh! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_576atr! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //there shouldn't be any with just the suffix
                    searchPattern = tempSearchPattern;
                    list = new List<String>();
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_624jmn! wrong file count.");

                    //superset
                    searchPattern = String.Format("blah{0}", Path.GetFileName(expectedFiles[0]));
                    newFiles = new List<String>();
                    foreach (String file in expectedFiles)
                    {
                        String realFile = Path.GetFileName(file);
                        if (realFile.Equals(searchPattern))
                            newFiles.Add(file);
                    }

                    list = newFiles;
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(files.Length, list.Count, "Err_026zqz! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_832yyg! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_605dke! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_728ono! Exception caught in scenario: {0}", ex);
            }

            //Scenario 6: Different local drives, Network drives (UNC and drive letters)
            /* Scenario disabled when porting because it modifies the filesystem outside of the test's working directory
            try
            {
                string otherDriveInMachine = IOServices.GetNonNtfsDriveOtherThanCurrent();
                if (otherDriveInMachine != null)
                {
                    dirName = ManageFileSystem.GetNonExistingDir(otherDriveInMachine, ManageFileSystem.DirPrefixName);
                    using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                    {
                        Console.WriteLine("otherDriveInMachine: {0}", dirName);
                        expectedFiles = fileManager.GetFiles(dirName, 0);
                        list = new List<String>(expectedFiles);
                        files = Directory.GetFiles(dirName, "*.*", SearchOption.TopDirectoryOnly);
                        Eval(files.Length, list.Count, "Err_337kkf! wrong file count.");
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (Eval(list.Contains(files[i]), "Err_448nzn! unexpected file found: {0}", files[i]))
                                list.Remove(files[i]);
                        }
                        if (!Eval(list.Count == 0, "Err_849fvp! {0} expected files not found.", list.Count))
                        {
                            foreach (String fileName in list)
                                Console.WriteLine(fileName);
                        }
                    }
                }

                // network path scenario moved to RemoteIOTests.cs
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_768lme! Exception caught in scenario: {0}", ex);
            }
            */


            //Scenario 7: Arguments validation: 
            // - nulls for the first 2, 
            // - outside range for the enum value. 
            // - Path contains empty, space and invalid filename, long, readonly invalid characters. The same for searchPattern parm as well
            try
            {
                String[] invalidValuesForPath = Interop.IsWindows ? new[]{ "", " ", ">" } : new[]{ "", "\0" };
                String[] invalidValuesForSearch = { "..", @".." + Path.DirectorySeparatorChar };
                CheckException<ArgumentNullException>(delegate { files = Directory.GetFiles(null, "*.*", SearchOption.TopDirectoryOnly); }, "Err_347g! worng exception thrown");
                CheckException<ArgumentNullException>(delegate { files = Directory.GetFiles(Directory.GetCurrentDirectory(), null, SearchOption.TopDirectoryOnly); }, "Err_326pgt! worng exception thrown");
                CheckException<ArgumentOutOfRangeException>(delegate { files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", (SearchOption)100); }, "Err_589kvu! worng exception thrown - see bug #386545");
                CheckException<ArgumentOutOfRangeException>(delegate { files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", (SearchOption)(-1)); }, "Err_359vcj! worng exception thrown - see bug #386545");
                for (int i = 0; i < invalidValuesForPath.Length; i++)
                {
                    CheckException<ArgumentException>(delegate { files = Directory.GetFiles(invalidValuesForPath[i], "*.*", SearchOption.TopDirectoryOnly); }, String.Format("Err_347sd_{0}! worng exception thrown: {1}", i, invalidValuesForPath[i]));
                }
                for (int i = 0; i < invalidValuesForSearch.Length; i++)
                {
                    CheckException<ArgumentException>(delegate { files = Directory.GetFiles(Directory.GetCurrentDirectory(), invalidValuesForSearch[i], SearchOption.TopDirectoryOnly); }, String.Format("Err_074ts! worng exception thrown: {1}", i, invalidValuesForSearch[i]));
                }
                Char[] invalidPaths = Path.GetInvalidPathChars();
                for (int i = 0; i < invalidPaths.Length; i++)
                {
                    CheckException<ArgumentException>(delegate { files = Directory.GetFiles(invalidPaths[i].ToString(), "*.*", SearchOption.TopDirectoryOnly); }, String.Format("Err_538wyc! worng exception thrown: {1}", i, invalidPaths[i]));
                }
                Char[] invalidFileNames = Interop.IsWindows ? Path.GetInvalidFileNameChars() : new[] { '\0' };
                for (int i = 0; i < invalidFileNames.Length; i++)
                {
                    switch (invalidFileNames[i])
                    {
                        case '\\':
                        case '/':
                            CheckException<DirectoryNotFoundException>(delegate { files = Directory.GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly); }, String.Format("Err_631bwy_{0}! worng exception thrown: {1} - bug#387196", i, (int)invalidFileNames[i]));
                            break;
                        //We dont throw in V1 too
                        case ':':
                            //History:
                            // 1) we assumed that this will work in all non-9x machine
                            // 2) Then only in XP
                            // 3) NTFS?
                            if (Interop.IsWindows && FileSystemDebugInfo.IsCurrentDriveNTFS()) // testing NTFS
                            {
                                CheckException<IOException>(delegate { files = Directory.GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly); }, String.Format("Err_997gqs_{0}! worng exception thrown: {1} - bug#387196", i, (int)invalidFileNames[i]));
                            }
                            else
                            {
                                try
                                {
                                    files = Directory.GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly);
                                }
                                catch (IOException)
                                {
                                    Console.WriteLine(FileSystemDebugInfo.MachineInfo());
                                    Eval(false, "Err_961lcx! Another OS throwing for DI.GetFiles(). modify the above check after confirming the v1.x behavior in that machine");
                                }
                            }

                            break;
                        case '*':
                        case '?':
                            files = Directory.GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly);
                            break;
                        default:
                            CheckException<ArgumentException>(delegate { files = Directory.GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly); }, String.Format("Err_036gza! worng exception thrown: {1} - bug#387196", i, (int)invalidFileNames[i]));
                            break;
                    }
                }
                //path too long
                CheckException<PathTooLongException>(delegate { files = Directory.GetFiles(Path.Combine(new String('a', IOInputs.MaxPath), new String('b', IOInputs.MaxPath)), "*.*", SearchOption.TopDirectoryOnly); }, String.Format("Err_927gs! wrong exception thrown"));
                CheckException<PathTooLongException>(delegate { files = Directory.GetFiles(new String('a', IOInputs.MaxPath), new String('b', IOInputs.MaxPath), SearchOption.TopDirectoryOnly); }, String.Format("Err_213aka! wrong exception thrown"));
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_995bae! Exception caught in scenario: {0}", ex);
            }

            // part II - SearchOption.AllDirectories
            //Scenario 1: Vanilla - create a directory with some files and then add a couple of directories with some files and check with searchPattern *.*
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedFiles = fileManager.GetAllFiles();
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_415mbz! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_287kkm! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_921mhs! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_415nwr! Exception caught in scenario: {0}", ex);
            }

            //Scenario 2: create a directory only top level files and directories and check
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
                {
                    expectedFiles = fileManager.GetAllFiles();
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_202wur! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_611lgv! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_648ibm! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_391mwx! Exception caught in scenario: {0}", ex);
            }

            // Path is not in the current directory (same drive)
            //Scenario 3: Ensure that the path contains subdirectories and call this API and ensure that only the top directory files are returned
            /* Scenario disabled when porting because it modifies the filesystem outside of the test's working directory
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedFiles = fileManager.GetAllFiles();
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_123rcm! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        //This will return as \<dirName>\<fileName> whereas our utility will return as <drive>:\<dirName>\<fileName>
                        String fileFullName = Path.GetFullPath(files[i]);
                        if (Eval(list.Contains(fileFullName), "Err_242yur! unexpected file found: {0}", fileFullName))
                            list.Remove(fileFullName);
                    }
                    if (!Eval(list.Count == 0, "Err_477xiv! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_401dkm! Exception caught in scenario: {0}", ex);
            }
            */

            //Scenario 4: searchPattern variations - valid search characters, file match exactly the searchPattern, searchPattern is a subset of existing files, superset, no match, 
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                String searchPattern;
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedFiles = fileManager.GetAllFiles();
                    //?
                    int maxLen = 0;
                    foreach (String file in expectedFiles)
                    {
                        String realFile = Path.GetFileNameWithoutExtension(file);
                        if (realFile.Length > maxLen)
                            maxLen = realFile.Length;
                    }
                    searchPattern = String.Format("{0}.???", new String('?', maxLen));
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_654wlf! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_792olh! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_434gew! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //file match exactly 
                    searchPattern = Path.GetFileName(expectedFiles[0]);
                    list = new List<String>(new String[] { expectedFiles[0] });
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_427fug! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_382bzl! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_008xan! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //subset
                    String tempSearchPattern = Path.GetFileName(expectedFiles[0]).Substring(2);
                    List<String> newFiles = new List<String>();
                    foreach (String file in expectedFiles)
                    {
                        String realFile = Path.GetFileName(file);
                        if (realFile.Substring(2).Equals(tempSearchPattern))
                            newFiles.Add(file);
                    }
                    searchPattern = String.Format("??{0}", tempSearchPattern);

                    list = newFiles;
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_030bfw! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_393mly! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_328gse! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //there shouldn't be any with just the suffix
                    searchPattern = tempSearchPattern;
                    list = new List<String>();
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_747fnq! wrong file count.");

                    //superset
                    searchPattern = String.Format("blah{0}", Path.GetFileName(expectedFiles[0]));
                    newFiles = new List<String>();
                    foreach (String file in expectedFiles)
                    {
                        String realFile = Path.GetFileName(file);
                        if (realFile.Equals(searchPattern))
                            newFiles.Add(file);
                    }

                    list = newFiles;
                    files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_969vnk! wrong file count.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_353ygu! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_830vvw! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_983ist! Exception caught in scenario: {0}", ex);
            }

            //Scenario 6: Different local drives, Network drives (UNC and drive letters)
            /* Scenario disabled when porting because it modifies the filesystem outside of the test's working directory
            try
            {
                string otherDriveInMachine = IOServices.GetNonNtfsDriveOtherThanCurrent();
                if (otherDriveInMachine != null)
                {
                    dirName = ManageFileSystem.GetNonExistingDir(otherDriveInMachine, ManageFileSystem.DirPrefixName);
                    using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                    {
                        expectedFiles = fileManager.GetAllFiles();
                        list = new List<String>(expectedFiles);
                        files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
                        Eval(files.Length, list.Count, "Err_573sdo! wrong file count.");
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (Eval(list.Contains(files[i]), "Err_778kdo! unexpected file found: {0}", files[i]))
                                list.Remove(files[i]);
                        }
                        if (!Eval(list.Count == 0, "Err_954xcx! {0} expected files not found.", list.Count))
                        {
                            foreach (String fileName in list)
                                Console.WriteLine(fileName);
                        }
                    }
                }

                // network path scenario moved to RemoteIOTests.cs
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_064vel! Exception caught in scenario: {0}", ex);
            }
            */

            //Scenario 7: dir is readonly
            try
            {
                //Readonly
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    //now lets make this directory readonly?
                    new DirectoryInfo(dirName).Attributes = new DirectoryInfo(dirName).Attributes | FileAttributes.ReadOnly;

                    expectedFiles = fileManager.GetAllFiles();
                    list = new List<String>(expectedFiles);
                    files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
                    Eval(files.Length, list.Count, "Err_862vhr! wrong file count.");

                    for (int i = 0; i < files.Length; i++)
                    {
                        if (Eval(list.Contains(files[i]), "Err_556ioj! unexpected file found: {0}", files[i]))
                            list.Remove(files[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_562xwh! {0} expected files not found.", list.Count))
                    {
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                    if (Directory.Exists(dirName) && (new DirectoryInfo(dirName).Attributes & FileAttributes.ReadOnly) != 0)
                        new DirectoryInfo(dirName).Attributes = new DirectoryInfo(dirName).Attributes ^ FileAttributes.ReadOnly;
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_418qfv! Exception caught in scenario: {0}", ex);
            }
        }
        catch (Exception ex)
        {
            s_pass = false;
            Console.WriteLine("Err_234rsgf! Uncaught exception in RunTest: {0}", ex);
        }

        Assert.True(s_pass);
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

    //Checks for a particular type of exception and an Exception msg
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
                error = String.Format("{0} Exception type: {1}, expected {2}", error, e.GetType().ToString(), typeof(E).ToString());
        }
        Eval(exception, error);
    }
}



