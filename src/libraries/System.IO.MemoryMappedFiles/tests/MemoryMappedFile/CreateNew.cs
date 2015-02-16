// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

[Collection("CreateNew")]
public class CreateNew : MMFTestBase
{
    private readonly static string s_uniquifier = Guid.NewGuid().ToString();

    [Fact]
    public static void CreateNewTestCases()
    {
        bool bResult = false;
        CreateNew test = new CreateNew();

        try
        {
            bResult = test.RunTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("FAiL! Error in CreateNew! Uncaught Exception in main(), exc_main==" + exc_main.ToString());
        }

        Assert.True(bResult, "One or more test cases failed.");
    }

    public bool RunTest()
    {
        try
        {
            ////////////////////////////////////////////////////////////////////////
            // CreateNew(mapName, capcity)
            ////////////////////////////////////////////////////////////////////////

            // [] mapName

            // mapname > 260 chars
            VerifyCreateNew("Loc111", "CreateNew" + new String('a', 1000) + s_uniquifier, 4096);

            // null
            VerifyCreateNew("Loc112", null, 4096);

            // empty string disallowed
            VerifyCreateNewException<ArgumentException>("Loc113", String.Empty, 4096);

            // all whitespace
            VerifyCreateNew("Loc114", "\t\t \n\u00A0", 4096);

            // MMF with this mapname already exists
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("map115" + s_uniquifier, 1000))
            {
                VerifyCreateNewException<IOException>("Loc115", "map115" + s_uniquifier, 1000);
            }

            // MMF with this mapname existed, but was closed
            VerifyCreateNew("Loc116", "map115" + s_uniquifier, 500);

            // "global/" prefix
            VerifyCreateNew("Loc117", "global/CN_0" + s_uniquifier, 4096);

            // "local/" prefix
            VerifyCreateNew("Loc118", "local/CN_1" + s_uniquifier, 4096);

            // [] capacity

            // >0 capacity
            VerifyCreateNew("Loc211", "CN_mapname211" + s_uniquifier, 50);

            // 0 capacity
            VerifyCreateNewException<ArgumentOutOfRangeException>("Loc211", "CN_mapname211" + s_uniquifier, 0);

            // negative
            VerifyCreateNewException<ArgumentOutOfRangeException>("Loc213", "CN_mapname213" + s_uniquifier, -1);

            // negative
            VerifyCreateNewException<ArgumentOutOfRangeException>("Loc214", "CN_mapname214" + s_uniquifier, -4096);

            // Int64.MaxValue - cannot exceed local address space
            if (IntPtr.Size == 4)
                VerifyCreateNewException<ArgumentOutOfRangeException>("Loc215", "CN_mapname215" + s_uniquifier, Int64.MaxValue);
            else // 64-bit machine
                VerifyCreateNewException<IOException>("Loc215b", "CN_mapname215" + s_uniquifier, Int64.MaxValue); // valid but too large

            ////////////////////////////////////////////////////////////////////////
            // CreateNew(mapName, capcity, MemoryMappedFileAccess)
            ////////////////////////////////////////////////////////////////////////

            // [] access

            // Write is disallowed
            VerifyCreateNewException<ArgumentException>("Loc330", "CN_mapname330" + s_uniquifier, 1000, MemoryMappedFileAccess.Write);

            // valid access
            MemoryMappedFileAccess[] accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.CopyOnWrite,
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyCreateNew("Loc331_" + access, "CN_mapname331_" + access + s_uniquifier, 1000, access);
            }

            // invalid enum value
            accessList = new MemoryMappedFileAccess[] {
                (MemoryMappedFileAccess)(-1),
                (MemoryMappedFileAccess)(6),
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyCreateNewException<ArgumentOutOfRangeException>("Loc332_" + ((int)access), "CN_mapname332_" + ((int)access) + s_uniquifier, 1000, access);
            }

            ////////////////////////////////////////////////////////////////////////
            // CreateNew(String, long, MemoryMappedFileAccess, MemoryMappedFileOptions, 
            //    MemoryMappedFileSecurity, HandleInheritability)
            ////////////////////////////////////////////////////////////////////////

            // [] mapName

            // mapname > 260 chars
            VerifyCreateNew("Loc411", "CreateNew2" + new String('a', 1000) + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // null
            VerifyCreateNew("Loc412", null, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // empty string disallowed
            VerifyCreateNewException<ArgumentException>("Loc413", String.Empty, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // all whitespace
            VerifyCreateNew("Loc414", "\t\t \n\u00A0", 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // MMF with this mapname already exists
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("map415" + s_uniquifier, 4096))
            {
                VerifyCreateNewException<IOException>("Loc415", "map415" + s_uniquifier, 4096, MemoryMappedFileAccess.Read, MemoryMappedFileOptions.None, HandleInheritability.None);
            }

            // MMF with this mapname existed, but was closed
            VerifyCreateNew("Loc416", "map415" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // "global/" prefix
            VerifyCreateNew("Loc417", "global/CN_2" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // "local/" prefix
            VerifyCreateNew("Loc418", "local/CN_3" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // [] capacity

            // >0 capacity
            VerifyCreateNew("Loc421", "CN_mapname421" + s_uniquifier, 50, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // 0 capacity
            VerifyCreateNewException<ArgumentOutOfRangeException>("Loc422", "CN_mapname422" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // negative
            VerifyCreateNewException<ArgumentOutOfRangeException>("Loc423", "CN_mapname423" + s_uniquifier, -1, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // negative
            VerifyCreateNewException<ArgumentOutOfRangeException>("Loc424", "CN_mapname424" + s_uniquifier, -4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // Int64.MaxValue - cannot exceed local address space
            if (IntPtr.Size == 4)
                VerifyCreateNewException<ArgumentOutOfRangeException>("Loc425", "CN_mapname425" + s_uniquifier, Int64.MaxValue, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
            else // 64-bit machine
                VerifyCreateNewException<IOException>("Loc425b", "CN_mapname425" + s_uniquifier, Int64.MaxValue, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None); // valid but too large

            // [] access

            // Write is disallowed
            VerifyCreateNewException<ArgumentException>("Loc430", "CN_mapname430" + s_uniquifier, 1000, MemoryMappedFileAccess.Write, MemoryMappedFileOptions.None, HandleInheritability.None);

            // valid access
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.CopyOnWrite,
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyCreateNew("Loc431_" + access, "CN_mapname431_" + access + s_uniquifier, 1000, access, MemoryMappedFileOptions.None, HandleInheritability.None);
            }

            // invalid enum value
            accessList = new MemoryMappedFileAccess[] {
                (MemoryMappedFileAccess)(-1),
                (MemoryMappedFileAccess)(6),
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                VerifyCreateNewException<ArgumentOutOfRangeException>("Loc432_" + ((int)access), "CN_mapname432_" + ((int)access) + s_uniquifier, 1000, access, MemoryMappedFileOptions.None, HandleInheritability.None);
            }

            // [] options

            // Default
            VerifyCreateNew("Loc440a", null, 4096 * 1000);
            VerifyCreateNew("Loc440b", null, 4096 * 10000);

            // None
            VerifyCreateNew("Loc441", "CN_mapname441" + s_uniquifier, 4096 * 10000, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            // DelayAllocatePages
            VerifyCreateNew("Loc442", "CN_mapname442" + s_uniquifier, 4096 * 10000, MemoryMappedFileAccess.Read, MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.None);

            // invalid
            VerifyCreateNewException<ArgumentOutOfRangeException>("Loc444", "CN_mapname444" + s_uniquifier, 100, MemoryMappedFileAccess.ReadWrite, (MemoryMappedFileOptions)(-1), HandleInheritability.None);

            /// END TEST CASES

            if (iCountErrors == 0)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Fail: iCountErrors==" + iCountErrors);
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
    public void VerifyCreateNew(String strLoc, String mapName, long capacity)
    {
        iCountTestcases++;
        try
        {
            ulong initAvailPageFile = GetAvailPageFile();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity))
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

    public void VerifyCreateNewException<EXCTYPE>(String strLoc, String mapName, long capacity) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity))
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

    public void VerifyCreateNew(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access)
    {
        iCountTestcases++;
        try
        {
            ulong initAvailPageFile = GetAvailPageFile();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access))
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

    public void VerifyCreateNewException<EXCTYPE>(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access))
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

    public void VerifyCreateNew(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability)
    {
        iCountTestcases++;
        try
        {
            ulong initAvailPageFile = GetAvailPageFile();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access, options, inheritability))
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

    public void VerifyCreateNewException<EXCTYPE>(String strLoc, String mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access, options, inheritability))
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
