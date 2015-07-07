// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Security;
using System.Globalization;
using Xunit;

public class Directory_GetDirectories_str_str_so
{
    private delegate void ExceptionCode();
    private static bool s_pass = true;

    [Fact]
    public static void runTest()
    {
        try
        {
            String dirName;
            String[] expectedDirs;
            String[] dirs;
            List<String> list;
            // part I - SearchOption.TopDirectoryOnly
            //Scenario 1:Vanilla - Create a directory, add a few dirs and call with searchPattern * and verify 
            //that all directories are returned.
            //Scenario 1.1: Ensure that the path contains deep subdirectories and call this API and ensure that only the top directories are returned
            //Scenario 1.2: no subdirectories exist in the directory
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedDirs = fileManager.GetDirectories(1);
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_3947g! wrong count");
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_582bmw! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_891vut! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }

                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
                {
                    expectedDirs = fileManager.GetDirectories(1);
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_412viu! wrong count");
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_004jzv! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_041qti! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
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


            //Scenario 2: Path is not in the current directory (same drive)
            /* Scenario disabled when porting because it modifies the filesystem outside of the test's working directory
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedDirs = fileManager.GetDirectories(1);
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_386gef! wrong count");
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        //This will return as \<dirName>\<fileName> whereas our utility will return as <drive>:\<dirName>\<fileName>
                        String fileFullName = Path.GetFullPath(dirs[i]);
                        if (Eval(list.Contains(fileFullName), "Err_932izm! No file found: {0}", fileFullName))
                            list.Remove(fileFullName);
                    }
                    if (!Eval(list.Count == 0, "Err_915sae! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
                //only 1 level
                dirName = ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), ManageFileSystem.DirPrefixName);
                dirName = Path.GetFullPath(dirName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
                {
                    expectedDirs = fileManager.GetDirectories(1);
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_792ifb! wrong count");
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_281tff! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_792qdn! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
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


            //Scenario 5: searchPattern variations - valid search characters, file match exactly the searchPattern, searchPattern is a subset of existing dirs, superset, no match, 
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                String searchPattern;
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedDirs = fileManager.GetDirectories(1);
                    //?
                    int maxLen = 0;
                    foreach (String dir in expectedDirs)
                    {
                        //we want the simple name of the directory and 
                        String realDir = new DirectoryInfo(dir).Name;
                        if (realDir.Length > maxLen)
                            maxLen = realDir.Length;
                    }
                    searchPattern = new String('?', maxLen);
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_488sjb! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_750dop! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_629dvi! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine("list - {0}", fileName);

                        foreach (String fileName in dirs)
                            Console.WriteLine("dirs - {0}", fileName);

                        Console.WriteLine("dirName: {0} searchPattern: {1}", dirName, searchPattern);
                    }

                    //*.*
                    searchPattern = "*.*";
                    expectedDirs = fileManager.GetDirectories(1);
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_403phi! wrong count: {0} - {1}", dirs.Length, list.Count);


                    //directory match exactly 			

                    searchPattern = expectedDirs[0].Substring(expectedDirs[0].LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    list = new List<String>(new String[] { expectedDirs[0] });
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_841dnz! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_796xxd! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_552puh! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine("SearchPattern: [{0}]", searchPattern);
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //subset
                    String tempSearchPattern = expectedDirs[0].Substring(expectedDirs[0].LastIndexOf(Path.DirectorySeparatorChar) + 1).Substring(2);
                    List<String> newFiles = new List<String>();
                    foreach (String dir in expectedDirs)
                    {
                        String realFile = dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        if (realFile.Substring(2).Equals(tempSearchPattern))
                            newFiles.Add(dir);
                    }
                    searchPattern = String.Format("??{0}", tempSearchPattern);

                    list = newFiles;
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_847vxz! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_736kfh! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_576atr! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //there shouldn't be any with just the suffix
                    searchPattern = tempSearchPattern;
                    list = new List<String>();
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_624jmn! wrong count: {0} - {1}", dirs.Length, list.Count);


                    //superset
                    searchPattern = String.Format("blah{0}", expectedDirs[0].Substring(expectedDirs[0].LastIndexOf(Path.DirectorySeparatorChar) + 1));
                    newFiles = new List<String>();
                    foreach (String dir in expectedDirs)
                    {
                        String realFile = dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        if (realFile.Equals(searchPattern))
                            newFiles.Add(dir);
                    }

                    list = newFiles;
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_026zqz! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_832yyg! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_605dke! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //pattern match an existing file name
                    String[] files = fileManager.GetFiles(dirName, 0);
                    searchPattern = Path.GetFileName(files[0]);
                    list = new List<String>();
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly);
                    Eval(dirs.Length == list.Count, "Err_786uyo! wrong count: {0} - {1}", dirs.Length, list.Count);
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
                        expectedDirs = fileManager.GetDirectories(1);
                        list = new List<String>(expectedDirs);
                        dirs = Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly);
                        Eval(dirs.Length == list.Count, "Err_337kkf! wrong count");
                        for (int i = 0; i < expectedDirs.Length; i++)
                        {
                            if (Eval(list.Contains(dirs[i]), "Err_448nzn! No file found: {0}", dirs[i]))
                                list.Remove(dirs[i]);
                        }
                        if (!Eval(list.Count == 0, "Err_849fvp! wrong count: {0}", list.Count))
                        {
                            Console.WriteLine();
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



            //Scenario 7: Arguments validation: nulls for the first 2, outside range for the enum value. Path contains empty, space and invalid filename 
            //characters. The same for searchPattern parm as well
            try
            {
                String[] invalidValuesForPath = Interop.IsWindows ? new[]{ "", " ", ">" } : new[]{ "", "\0" };
                String[] invalidValuesForSearch = { "..", @".." + Path.DirectorySeparatorChar };
                CheckException<ArgumentNullException>(delegate { dirs = Directory.GetDirectories(null, "*", SearchOption.TopDirectoryOnly); }, "Err_347g! worng exception thrown");
                CheckException<ArgumentNullException>(delegate { dirs = Directory.GetDirectories(Directory.GetCurrentDirectory(), null, SearchOption.TopDirectoryOnly); }, "Err_326pgt! worng exception thrown");
                CheckException<ArgumentOutOfRangeException>(delegate { dirs = Directory.GetDirectories(Directory.GetCurrentDirectory(), "*", (SearchOption)100); }, "Err_589kvu! worng exception thrown - see bug #386545");
                CheckException<ArgumentOutOfRangeException>(delegate { dirs = Directory.GetDirectories(Directory.GetCurrentDirectory(), "*", (SearchOption)(-1)); }, "Err_359vcj! worng exception thrown - see bug #386545");
                for (int i = 0; i < invalidValuesForPath.Length; i++)
                {
                    CheckException<ArgumentException>(delegate { dirs = Directory.GetDirectories(invalidValuesForPath[i], "*", SearchOption.TopDirectoryOnly); }, String.Format("Err_347sd_{0}! worng exception thrown: {1}", i, invalidValuesForPath[i]));
                }
                for (int i = 0; i < invalidValuesForSearch.Length; i++)
                {
                    CheckException<ArgumentException>(delegate { dirs = Directory.GetDirectories(Directory.GetCurrentDirectory(), invalidValuesForSearch[i], SearchOption.TopDirectoryOnly); }, String.Format("Err_631bwy! worng exception thrown: {1}", i, invalidValuesForSearch[i]));
                }
                Char[] invalidPaths = Path.GetInvalidPathChars();
                for (int i = 0; i < invalidPaths.Length; i++)
                {
                    CheckException<ArgumentException>(delegate { dirs = Directory.GetDirectories(invalidPaths[i].ToString(), "*", SearchOption.TopDirectoryOnly); }, String.Format("Err_538wyc! worng exception thrown: {1}", i, invalidPaths[i]));
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_995bae! Exception caught in scenario: {0}", ex);
            }



            // part II - SearchOption.AllDirectories
            //Scenario 1: Vanilla - create a directory with some dirs and then add a couple of directories with some dirs and check with searchPattern *
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedDirs = fileManager.GetAllDirectories();
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, "*", SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_415mbz! wrong count {0} {1}", dirs.Length, list.Count);
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_287kkm! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_921mhs! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
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

            //Scenario 2: create a directory only top level dirs and directories and check
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
                {
                    expectedDirs = fileManager.GetAllDirectories();
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, "*", SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_202wur! wrong count");
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_611lgv! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_648ibm! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
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
            //Scenario 3: Ensure that the path contains subdirectories and call this API and ensure that only the top directory dirs are returned
            /* Scenario disabled when porting because it modifies the filesystem outside of the test's working directory
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedDirs = fileManager.GetAllDirectories();
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, "*", SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_123rcm! wrong count");
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        //This will return as \<dirName>\<fileName> whereas our utility will return as <drive>:\<dirName>\<fileName>
                        String fileFullName = Path.GetFullPath(dirs[i]);
                        if (Eval(list.Contains(fileFullName), "Err_242yur! No file found: {0}", fileFullName))
                            list.Remove(fileFullName);
                    }
                    if (!Eval(list.Count == 0, "Err_477xiv! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
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

            //Scenario 4: searchPattern variations - valid search characters, file match exactly the searchPattern, searchPattern is a subset of existing dirs, superset, no match, 
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                String searchPattern;
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    expectedDirs = fileManager.GetAllDirectories();
                    //?
                    int maxLen = 0;
                    foreach (String dir in expectedDirs)
                    {
                        //we want the simple name of the directory and 
                        String realDir = new DirectoryInfo(dir).Name;
                        if (realDir.Length > maxLen)
                            maxLen = realDir.Length;
                    }
                    searchPattern = new String('?', maxLen);
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_261jae! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_631xmw! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_790fuv! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //*.*
                    searchPattern = "*.*";
                    expectedDirs = fileManager.GetAllDirectories();
                    list = new List<String>(expectedDirs);
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_304qte! wrong count: {0} - {1}", dirs.Length, list.Count);


                    //directory match exactly - @TODO!!!

                    searchPattern = expectedDirs[0].Substring(expectedDirs[0].LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    list = new List<String>();
                    foreach (String dir in expectedDirs)
                    {
                        String dirLastName = dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        if (searchPattern.Equals(dirLastName, StringComparison.CurrentCultureIgnoreCase))
                            list.Add(dir);
                    }
                    Console.WriteLine("<{0}>", searchPattern);
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_188xbk! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_120xcj! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_664zyk! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }


                    //subset
                    String tempSearchPattern = expectedDirs[0].Substring(expectedDirs[0].LastIndexOf(Path.DirectorySeparatorChar) + 1).Substring(2);
                    List<String> newFiles = new List<String>();
                    foreach (String dir in expectedDirs)
                    {
                        String realFile = dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        if (realFile.Substring(2).Equals(tempSearchPattern))
                            newFiles.Add(dir);
                    }
                    searchPattern = String.Format("??{0}", tempSearchPattern);

                    list = newFiles;
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_245luj! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_251vky! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_168yge! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //there shouldn't be any with just the suffix
                    searchPattern = tempSearchPattern;
                    list = new List<String>();
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_272suf! wrong count: {0} - {1}", dirs.Length, list.Count);


                    //superset
                    searchPattern = String.Format("blah{0}", expectedDirs[0].Substring(expectedDirs[0].LastIndexOf(Path.DirectorySeparatorChar) + 1));
                    newFiles = new List<String>();
                    foreach (String dir in expectedDirs)
                    {
                        String realFile = dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        if (realFile.Equals(searchPattern))
                            newFiles.Add(dir);
                    }

                    list = newFiles;
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_888snc! wrong count: {0} - {1}", dirs.Length, list.Count);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i]), "Err_330akg! No file found: {0}", dirs[i]))
                            list.Remove(dirs[i]);
                    }
                    if (!Eval(list.Count == 0, "Err_660npm! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }

                    //pattern match an existing file name
                    String[] files = fileManager.GetFiles(dirName, 0);
                    searchPattern = Path.GetFileName(files[0]);
                    list = new List<String>();
                    dirs = Directory.GetDirectories(dirName, searchPattern, SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_238cul! wrong count: {0} - {1}", dirs.Length, list.Count);
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_474hse! Exception caught in scenario: {0}", ex);
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
                        expectedDirs = fileManager.GetAllDirectories();
                        list = new List<String>(expectedDirs);
                        dirs = Directory.GetDirectories(dirName, "*", SearchOption.AllDirectories);
                        Eval(dirs.Length == list.Count, "Err_573sdo! wrong count");
                        for (int i = 0; i < expectedDirs.Length; i++)
                        {
                            if (Eval(list.Contains(dirs[i]), "Err_778kdo! No file found: {0}", dirs[i]))
                                list.Remove(dirs[i]);
                        }
                        if (!Eval(list.Count == 0, "Err_954xcx! wrong count: {0}", list.Count))
                        {
                            Console.WriteLine();
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

            // Regression Test (DevDiv Bug927807): throw NotSupportException when there are whitespace before drive letter
            if (Interop.IsWindows) // whitespace is valid in a file name on Unix, where the root is just a directory separator 
            {
                try
                {
                    string root = Path.GetPathRoot(Directory.GetCurrentDirectory());

                    var fullPath = Path.GetFullPath(" " + root);
                    Eval(fullPath, root, "Wrong Full Path");

                    string pathStr = root + "test" + Path.DirectorySeparatorChar + "test.cs";
                    fullPath = Path.GetFullPath(" " + pathStr);
                    Eval(fullPath, pathStr, "Wrong Full Path");
                }
                catch (Exception ex)
                {
                    s_pass = false;
                    Console.WriteLine("Bug927807! Exception caught in scenario: {0}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            s_pass = false;
            Console.WriteLine("Err_234rsgf! Uncaught exception in RunTest: {0}", ex);
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
                error = String.Format("{0} Exception type: {1}, {2}", error, e.GetType().ToString(), e.ToString());
        }
        Eval(exception, error);
    }
}







