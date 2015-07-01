// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace EnumerableTests
{
    internal class EnumerableUtils
    {
        private int _totalFailCount = 0;
        private int _totalCount = 0;

        public bool Passed
        {
            get { return _totalFailCount == 0; }
        }

        public void PrintTestStatus(String testName, String methodName, int failCount)
        {
            _totalCount++;

            if (failCount != 0)
            {
                _totalFailCount++;
                Console.WriteLine("---- Inner Test FAILED: {0} ({1}) ----", methodName, testName);
            }
        }

        // test setup and tear-down
        public void CreateTestDirs(String testFolder)
        {
            String currentDir = Directory.GetCurrentDirectory();
            testDir = Path.Combine(currentDir, testFolder, Path.GetRandomFileName());

            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);

            Directory.CreateDirectory(testDir);

            String subDir_a = Path.Combine(testDir, "lev1_a");
            String subDir_b = Path.Combine(testDir, "lev1_b");
            String subDir_c = Path.Combine(testDir, "lev1_c");

            Directory.CreateDirectory(subDir_a);
            Directory.CreateDirectory(subDir_b);
            Directory.CreateDirectory(subDir_c);

            String subDir_d = Path.Combine(subDir_a, "lev2_d");
            String subDir_e = Path.Combine(subDir_a, "lev2_e");
            String subDir_f = Path.Combine(subDir_b, "lev2_f");
            deepDir = subDir_b;

            Directory.CreateDirectory(subDir_d);
            Directory.CreateDirectory(subDir_e);
            Directory.CreateDirectory(subDir_f);

            String file1 = Path.Combine(testDir, "file1");
            String file2 = Path.Combine(subDir_b, "file2");
            String file3 = Path.Combine(subDir_f, "file3");
            deepFile = file2;

            File.WriteAllText(file1, "this is file 1" + Environment.NewLine + "Line 2 in file 1" + Environment.NewLine + "Line 3 in file 1" + Environment.NewLine + "Line 4 in file 1");
            File.WriteAllText(file2, "this is file 2");
            File.WriteAllText(file3, "this is file 3");

            expected_Dirs_Deep = new HashSet<String>();
            expected_Dirs_Deep.Add(subDir_a);
            expected_Dirs_Deep.Add(subDir_b);
            expected_Dirs_Deep.Add(subDir_c);
            expected_Dirs_Deep.Add(subDir_d);
            expected_Dirs_Deep.Add(subDir_e);
            expected_Dirs_Deep.Add(subDir_f);

            expected_Dirs_Shallow = new HashSet<String>();
            expected_Dirs_Shallow.Add(subDir_a);
            expected_Dirs_Shallow.Add(subDir_b);
            expected_Dirs_Shallow.Add(subDir_c);

            expected_Files_Deep = new HashSet<String>();
            expected_Files_Deep.Add(file1);
            expected_Files_Deep.Add(file2);
            expected_Files_Deep.Add(file3);

            expected_Files_Shallow = new HashSet<String>();
            expected_Files_Shallow.Add(file1);

            expected_Dirs_Subdir = new HashSet<String>();
            expected_Dirs_Subdir.Add(subDir_d);
            expected_Dirs_Subdir.Add(subDir_e);

            expected_Dirs_Lev2SearchPattern = new HashSet<String>();
            expected_Dirs_Lev2SearchPattern.Add(subDir_d);
            expected_Dirs_Lev2SearchPattern.Add(subDir_e);
            expected_Dirs_Lev2SearchPattern.Add(subDir_f);

            expected_Dirs_ExactSearchPattern = new HashSet<String>();
            expected_Dirs_ExactSearchPattern.Add(subDir_f);

            FSIEntry entry_a = new FSIEntry("lev1_a", subDir_a, null, "lev1_a");
            FSIEntry entry_b = new FSIEntry("lev1_b", subDir_b, null, "lev1_b");
            FSIEntry entry_c = new FSIEntry("lev1_c", subDir_c, null, "lev1_c");
            FSIEntry entry_d = new FSIEntry("lev2_d", subDir_d, null, "lev2_d");
            FSIEntry entry_e = new FSIEntry("lev2_e", subDir_e, null, "lev2_e");
            FSIEntry entry_f = new FSIEntry("lev2_f", subDir_f, null, "lev2_f");

            FSIEntry entry_1 = new FSIEntry("file1", file1, testDir, "file1");
            FSIEntry entry_2 = new FSIEntry("file2", file2, subDir_b, "file2");
            FSIEntry entry_3 = new FSIEntry("file3", file3, subDir_f, "file3");

            expected_Dirs_Deep_FSI = new HashSet<FSIEntry>();
            expected_Dirs_Deep_FSI.Add(entry_a);
            expected_Dirs_Deep_FSI.Add(entry_b);
            expected_Dirs_Deep_FSI.Add(entry_c);
            expected_Dirs_Deep_FSI.Add(entry_d);
            expected_Dirs_Deep_FSI.Add(entry_e);
            expected_Dirs_Deep_FSI.Add(entry_f);

            expected_Dirs_Shallow_FSI = new HashSet<FSIEntry>();
            expected_Dirs_Shallow_FSI.Add(entry_a);
            expected_Dirs_Shallow_FSI.Add(entry_b);
            expected_Dirs_Shallow_FSI.Add(entry_c);

            expected_Files_Deep_FSI = new HashSet<FSIEntry>();
            expected_Files_Deep_FSI.Add(entry_1);
            expected_Files_Deep_FSI.Add(entry_2);
            expected_Files_Deep_FSI.Add(entry_3);

            expected_Files_Shallow_FSI = new HashSet<FSIEntry>();
            expected_Files_Shallow_FSI.Add(entry_1);

            expected_Dirs_Subdir_FSI = new HashSet<FSIEntry>();
            expected_Dirs_Subdir_FSI.Add(entry_d);
            expected_Dirs_Subdir_FSI.Add(entry_e);
        }

        public static string GetUnusedDrive()
        {
            return IOServices.GetNonExistentDrive();
        }

        public void DeleteTestDirs()
        {
            bool deleted = false;
            int attemptsRemaining = 5;
            while (!deleted && attemptsRemaining > 0)
            {
                try
                {
                    if (Directory.Exists(testDir))
                        FailSafeDirectoryOperations.DeleteDirectory(testDir, true);
                    deleted = true;
                    break;
                }
                catch (IOException)
                {
                    if (-attemptsRemaining == 0)
                        throw;
                    else
                        Task.Delay(200).Wait();
                }
            }
        }

        public void ChangeFSAdd()
        {
            String subDir_a = Path.Combine(testDir, "lev1_a");
            String subDir_b = Path.Combine(testDir, "lev1_b");
            String subDir_c = Path.Combine(testDir, "lev1_c");
            String subDir_d = Path.Combine(subDir_a, "lev2_d");
            String subDir_e = Path.Combine(subDir_a, "lev2_e");
            String subDir_f = Path.Combine(subDir_b, "lev2_f");

            String file1 = Path.Combine(testDir, "file1");
            String file2 = Path.Combine(subDir_b, "file2");
            String file3 = Path.Combine(subDir_f, "file3");

            String newDir = Path.Combine(subDir_b, "newDir");
            String newFile = Path.Combine(subDir_c, "newFile");

            Directory.CreateDirectory(newDir);
            File.WriteAllText(newFile, "new file");

            expected_Dirs_Changed = new HashSet<String>();
            expected_Dirs_Changed.Add(subDir_a);
            expected_Dirs_Changed.Add(subDir_b);
            expected_Dirs_Changed.Add(subDir_c);
            expected_Dirs_Changed.Add(subDir_d);
            expected_Dirs_Changed.Add(subDir_e);
            expected_Dirs_Changed.Add(subDir_f);
            expected_Dirs_Changed.Add(newDir);

            expected_Files_Changed = new HashSet<String>();
            expected_Files_Changed.Add(file1);
            expected_Files_Changed.Add(file2);
            expected_Files_Changed.Add(newFile);
            expected_Files_Changed.Add(file3);
        }

        public void ChangeFSDelete()
        {
            String subDir_a = Path.Combine(testDir, "lev1_a");
            String subDir_b = Path.Combine(testDir, "lev1_b");
            String subDir_c = Path.Combine(testDir, "lev1_c");
            String subDir_f = Path.Combine(subDir_b, "lev2_f");

            String file1 = Path.Combine(testDir, "file1");
            String file2 = Path.Combine(subDir_b, "file2");
            String file3 = Path.Combine(subDir_f, "file3");

            Directory.Delete(subDir_a, true); // also deletes d and e
            File.Delete(file1);
            File.Delete(file3);

            expected_Dirs_Changed = new HashSet<String>();
            expected_Dirs_Changed.Add(subDir_a); // enumerated before delete
            expected_Dirs_Changed.Add(subDir_b);
            expected_Dirs_Changed.Add(subDir_c);
            expected_Dirs_Changed.Add(subDir_f);

            expected_Files_Changed = new HashSet<String>();
            expected_Files_Changed.Add(file1);  // enumerated before delete
            expected_Files_Changed.Add(file2);
        }

        public String testDir;
        public String deepDir;
        public String deepFile;
        public HashSet<String> expected_Dirs_Deep;
        public HashSet<String> expected_Dirs_Shallow;
        public HashSet<String> expected_Dirs_Subdir;
        public HashSet<String> expected_Dirs_Lev2SearchPattern;
        public HashSet<String> expected_Files_Deep;
        public HashSet<String> expected_Files_Shallow;
        public HashSet<String> expected_Dirs_ExactSearchPattern;
        public HashSet<String> expected_Dirs_Changed;
        public HashSet<String> expected_Files_Changed;

        public HashSet<FSIEntry> expected_Dirs_Deep_FSI;
        public HashSet<FSIEntry> expected_Dirs_Shallow_FSI;
        public HashSet<FSIEntry> expected_Dirs_Subdir_FSI;
        public HashSet<FSIEntry> expected_Files_Deep_FSI;
        public HashSet<FSIEntry> expected_Files_Shallow_FSI;

        public delegate IEnumerable<String> ReadFastDelegate1(String path);
        public delegate IEnumerable<String> ReadFastDelegate2(String path, Encoding encoding);
        public delegate void WriteFastDelegate1(String path, IEnumerable<String> contents);
        public delegate void WriteFastDelegate2(String path, IEnumerable<String> contents, Encoding encoding);
        public delegate void AppendFastDelegate1(String path, IEnumerable<String> contents);
        public delegate void AppendFastDelegate2(String path, IEnumerable<String> contents, Encoding encoding);

        public delegate String[] GetFSEs0(String path);
        public delegate String[] GetFSEs1(String path, String pattern);
        public delegate String[] GetFSEs2(String path, String pattern, SearchOption option);
        public delegate IEnumerable<String> GetFSEsFast0(String path);
        public delegate IEnumerable<String> GetFSEsFast1(String path, String pattern);
        public delegate IEnumerable<String> GetFSEsFast2(String path, String pattern, SearchOption option);
    }

    public sealed class FSIEntry
    {
        public FSIEntry(String Name, String FullName, String DirectoryName, String ToString)
        {
            this.Name = Name;
            this.FullName = FullName;
            this.DirectoryName = DirectoryName;
            this.ToStr = ToString;
        }

        public String Name;
        public String FullName;
        public String DirectoryName;
        public String ToStr;

        public override bool Equals(Object o)
        {
            FSIEntry other = o as FSIEntry;
            if (other == null) return false;
            return this.Name == other.Name &&
                this.FullName == other.FullName &&
                this.DirectoryName == other.DirectoryName &&
                this.ToStr == other.ToStr;
        }

        public override int GetHashCode()
        {
            int hc = Name.GetHashCode() + FullName.GetHashCode() + ToStr.GetHashCode();
            if (DirectoryName != null)
            {
                hc += DirectoryName.GetHashCode();
            }
            return hc;
        }
    }
}
