// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

[Collection("CreateOrOpen")]
public class CreateOrOpen : MMFTestBase
{
    private readonly static string s_uniquifier = Guid.NewGuid().ToString();
    private readonly static string s_fileNameTest = "CreateOrOpen_test_" + s_uniquifier + ".txt";

    [Fact]
    public static void CreateOrOpenTestCases()
    {
        bool bResult = false;
        CreateOrOpen test = new CreateOrOpen();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("FAiL! Error Err_9999zzz! Uncaught Exception in main(), exc_main==" + exc_main.ToString());
        }

        Assert.True(bResult, "One or more test cases failed.");
    }

    public bool runTest()
    {
        try
        {
            ////////////////////////////////////////////////////////////////////////
            // CreateOrOpen(mapName, capcity)
            ////////////////////////////////////////////////////////////////////////

            // [] mapName

            // mapname > 260 chars
            VerifyCreate("Loc111", "CreateOrOpen" + new String('a', 1000) + s_uniquifier, 4096);

            // null
            VerifyException<ArgumentNullException>("Loc112", null, 4096);

            // empty string disallowed
            VerifyException<ArgumentException>("Loc113", String.Empty, 4096);

            // all whitespace
            VerifyCreate("Loc114", "\t \n\u00A0", 4096);

            // MMF with this mapname already exists (pagefile backed)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_map115a" + s_uniquifier, 1000))
            {
                VerifyOpen("Loc115a", "COO_map115a" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite);
            }

            // MMF with this mapname already exists (filesystem backed)
            String fileText = "Non-empty file for MMF testing.";
            File.WriteAllText(s_fileNameTest, fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameTest, FileMode.Open, "COO_map115b" + s_uniquifier))
            {
                VerifyOpen("Loc115b", "COO_map115b" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite);
            }

            // MMF with this mapname existed, but was closed - new MMF
            VerifyCreate("Loc116", "COO_map115a" + s_uniquifier, 500);

            // "global/" prefix
            VerifyCreate("Loc117", "global/COO_mapname" + s_uniquifier, 4096);

            // "local/" prefix
            VerifyCreate("Loc118", "local/COO_mapname" + s_uniquifier, 4096);

            // [] capacity

            // >0 capacity
            VerifyCreate("Loc211", "COO_mapname211" + s_uniquifier, 50);

            // 0 capacity
            VerifyException<ArgumentOutOfRangeException>("Loc211", "COO_mapname211" + s_uniquifier, 0);

            // negative
            VerifyException<ArgumentOutOfRangeException>("Loc213", "COO_mapname213" + s_uniquifier, -1);

            // negative
            VerifyException<ArgumentOutOfRangeException>("Loc214", "COO_mapname214" + s_uniquifier, -4096);

            // Int64.MaxValue - cannot exceed local address space
            if (IntPtr.Size == 4)
                VerifyException<ArgumentOutOfRangeException>("Loc215", "COO_mapname215" + s_uniquifier, Int64.MaxValue);
            else // 64-bit machine
                VerifyException<IOException>("Loc215b", "COO_mapname215" + s_uniquifier, Int64.MaxValue); // valid but too large

            // ignored for existing file (smaller)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname216" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyOpen("Loc216", "COO_mapname216" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // ignored for existing file (larger)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname217" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyOpen("Loc217", "COO_mapname217" + s_uniquifier, 10000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // existing file - invalid - still exception
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname218" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyException<ArgumentOutOfRangeException>("Loc218", "COO_mapname218" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
            }

            ////////////////////////////////////////////////////////////////////////
            // CreateOrOpen(mapName, capcity, MemoryMappedFileAccess)
            ////////////////////////////////////////////////////////////////////////

            // [] access

            // Write is disallowed
            VerifyException<ArgumentException>("Loc330", "COO_mapname330" + s_uniquifier, 1000, MemoryMappedFileAccess.Write);

            // valid access - new file
            MemoryMappedFileAccess[] accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.CopyOnWrite,
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyCreate("Loc331_" + access, "COO_mapname331_" + access + s_uniquifier, 1000, access);
            }

            // invalid enum value
            accessList = new MemoryMappedFileAccess[] {
                (MemoryMappedFileAccess)(-1),
                (MemoryMappedFileAccess)(6),
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyException<ArgumentOutOfRangeException>("Loc332_" + ((int)access), "COO_mapname332_" + ((int)access) + s_uniquifier, 1000, access);
            }

            // default security - all valid for existing file
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname333" + s_uniquifier, 1000))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.CopyOnWrite,
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.Write,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.ReadExecute,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyOpen("Loc333_" + access, "COO_mapname333" + s_uniquifier, 1000, access, access);
                }
            }

            ////////////////////////////////////////////////////////////////////////
            // CreateOrOpen(String, long, MemoryMappedFileAccess, MemoryMappedFileOptions,
            //    MemoryMappedFileSecurity, HandleInheritability)
            ////////////////////////////////////////////////////////////////////////

            // [] mapName

            // mapname > 260 chars
            VerifyCreate("Loc411", "CreateOrOpen2" + new String('a', 1000) + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // null
            VerifyException<ArgumentNullException>("Loc412", null, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // empty string disallowed
            VerifyException<ArgumentException>("Loc413", String.Empty, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // all whitespace
            VerifyCreate("Loc414", "\t \n\u00A0", 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // MMF with this mapname already exists (pagefile backed)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_map415a" + s_uniquifier, 1000))
            {
                using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen("COO_map415a" + s_uniquifier, 1000))
                {
                    VerifyOpen("Loc415a", "COO_map415a" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
                }
            }

            // MMF with this mapname already exists (filesystem backed)
            File.WriteAllText(s_fileNameTest, fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameTest, FileMode.Open, "COO_map415b"))
            {
                VerifyOpen("Loc415b", "COO_map415b" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // MMF with this mapname existed, but was closed - new MMF
            VerifyCreate("Loc416", "COO_map415a" + s_uniquifier, 500, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // "global/" prefix
            VerifyCreate("Loc417", "global/COO_mapname" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // "local/" prefix
            VerifyCreate("Loc418", "local/COO_mapname" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // [] capacity

            // >0 capacity
            VerifyCreate("Loc421", "COO_mapname421" + s_uniquifier, 50, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // 0 capacity
            VerifyException<ArgumentOutOfRangeException>("Loc422", "COO_mapname422" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // negative
            VerifyException<ArgumentOutOfRangeException>("Loc423", "COO_mapname423" + s_uniquifier, -1, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // negative
            VerifyException<ArgumentOutOfRangeException>("Loc424", "COO_mapname424" + s_uniquifier, -4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // Int64.MaxValue - cannot exceed local address space
            if (IntPtr.Size == 4)
                VerifyException<ArgumentOutOfRangeException>("Loc425", "COO_mapname425" + s_uniquifier, Int64.MaxValue, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
            else // 64-bit machine
                VerifyException<IOException>("Loc425b", "COO_mapname425" + s_uniquifier, Int64.MaxValue, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None); // valid but too large

            // ignored for existing file (smaller)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname426" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyOpen("Loc426", "COO_mapname426" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // ignored for existing file (larger)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname427" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyOpen("Loc427", "COO_mapname427" + s_uniquifier, 10000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // existing file - invalid - still exception
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname428" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyException<ArgumentOutOfRangeException>("Loc428", "COO_mapname428" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
            }

            // [] access

            // Write is disallowed for new file
            VerifyException<ArgumentException>("Loc430", "COO_mapname430" + s_uniquifier, 1000, MemoryMappedFileAccess.Write, MemoryMappedFileOptions.None, HandleInheritability.None);

            // valid access for a new file
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.CopyOnWrite,
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyCreate("Loc431_" + access, "COO_mapname431_" + access + s_uniquifier, 1000, access, MemoryMappedFileOptions.None, HandleInheritability.None);
            }

            // invalid enum value
            accessList = new MemoryMappedFileAccess[] {
                (MemoryMappedFileAccess)(-1),
                (MemoryMappedFileAccess)(6),
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyException<ArgumentOutOfRangeException>("Loc432_" + ((int)access), "COO_mapname432_" + ((int)access) + s_uniquifier, 1000, access, MemoryMappedFileOptions.None, HandleInheritability.None);
            }

            // default security - all valid
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname433" + s_uniquifier, 1000))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.CopyOnWrite,
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.Write,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.ReadExecute,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyOpen("Loc433_" + access, "COO_mapname433" + s_uniquifier, 1000, access, MemoryMappedFileOptions.None, HandleInheritability.None, access);
                }
            }

            // default security, original (lesser) viewAccess is respected
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname434" + s_uniquifier, 1000, MemoryMappedFileAccess.Read))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.CopyOnWrite,
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.ReadExecute,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyOpen("Loc434_" + access, "COO_mapname434" + s_uniquifier, 1000, access, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.Read);
                }
                VerifyOpen("Loc434_Write", "COO_mapname434" + s_uniquifier, 1000, MemoryMappedFileAccess.Write, MemoryMappedFileOptions.None, HandleInheritability.None, (MemoryMappedFileAccess)(-1));  // for current architecture, rights=Write implies ReadWrite access but not Read, so no expected access here
            }

            // [] options

            // Default
            VerifyCreate("Loc440a", "COO_mapname440a" + s_uniquifier, 4096 * 1000);
            VerifyCreate("Loc440b", "COO_mapname440b" + s_uniquifier, 4096 * 10000);

            // None - new file
            VerifyCreate("Loc441", "COO_mapname441" + s_uniquifier, 4096 * 10000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // DelayAllocatePages - new file
            VerifyCreate("Loc442", "COO_mapname442" + s_uniquifier, 4096 * 10000, MemoryMappedFileAccess.Read, MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.None);

            // invalid
            VerifyException<ArgumentOutOfRangeException>("Loc443", "COO_mapname443" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, (MemoryMappedFileOptions)(-1), HandleInheritability.None);

            // ignored for existing file
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname444" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyOpen("Loc444", "COO_mapname444" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // [] memoryMappedFileSecurity

            // null, tested throughout this file

            // valid non-null
            VerifyCreate("Loc451", "COO_mapname451" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // ignored for existing
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("COO_mapname452" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                VerifyOpen("Loc452", "COO_mapname452" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // [] inheritability

            // None - new file
            VerifyCreate("Loc461", "COO_mapname461" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // Inheritable - new file
            VerifyCreate("Loc462", "COO_mapname462" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.Inheritable);

            // Mix and match: None - existing file w/Inheritable
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("COO_mapname464" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.Inheritable))
            {
                VerifyOpen("Loc464a", "COO_mapname464" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None, MemoryMappedFileAccess.ReadWrite);
            }

            // Mix and match: Inheritable - existing file w/None
            using (FileStream fs = new FileStream(s_fileNameTest, FileMode.Open))
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, "COO_mapname465" + s_uniquifier, 1000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                {
                    VerifyOpen("Loc465b", "COO_mapname465" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.Inheritable, MemoryMappedFileAccess.ReadWrite);
                }
            }

            // invalid
            VerifyException<ArgumentOutOfRangeException>("Loc467", "COO_mapname467" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, (HandleInheritability)(-1));
            VerifyException<ArgumentOutOfRangeException>("Loc468", "COO_mapname468" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, (HandleInheritability)(2));


            /// END TEST CASES

            if (iCountErrors == 0)
            {
                return true;
            }
            else
            {
                Console.WriteLine("FAiL! iCountErrors==" + iCountErrors);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERR999: Unexpected exception in runTest, {0}", ex);
            return false;
        }
    }

    /// START HELPER FUNCTIONS
    public void VerifyCreate(String strLoc, String mapName, long capacity)
    {
        iCountTestcases++;
        try
        {
            ulong initAvailPageFile = GetAvailPageFile();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity))
            {
                VerifyAccess(strLoc, mmf, MemoryMappedFileAccess.ReadWrite, capacity);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyOpen(String strLoc, String mapName, long capacity, MemoryMappedFileAccess expectedAccess)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity))
            {
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
                VerifyAccess(strLoc, mmf, expectedAccess, 10);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyException<EXCTYPE>(String strLoc, String mapName, long capacity) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreate(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access)
    {
        iCountTestcases++;
        try
        {
            ulong initAvailPageFile = GetAvailPageFile();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                VerifyAccess(strLoc, mmf, access, capacity);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyOpen(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileAccess expectedAccess)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
                VerifyAccess(strLoc, mmf, expectedAccess, 10);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }


    public void VerifyException<EXCTYPE>(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreate(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability)
    {
        iCountTestcases++;
        try
        {
            ulong initAvailPageFile = GetAvailPageFile();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, inheritability))
            {
                VerifyAccess(strLoc, mmf, access, capacity);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, inheritability);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyOpen(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability, MemoryMappedFileAccess expectedAccess)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, inheritability))
            {
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, inheritability);
                VerifyAccess(strLoc, mmf, expectedAccess, 10);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyException<EXCTYPE>(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, inheritability))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }
}
