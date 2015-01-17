// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;
using Xunit;
using System.Runtime.InteropServices;

[Collection("CreateViewAccessor")]
public class CreateViewAccessor : MMFTestBase
{
    private static readonly String s_fileNameForLargeCapacity = "CreateViewAccessor_MMF_ForLargeCapacity.txt";

    [Fact]
    public static void CreateViewAccessorTestCases()
    {
        bool bResult = false;
        CreateViewAccessor test = new CreateViewAccessor();

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
            // CreateViewAccessor()
            ////////////////////////////////////////////////////////////////////////

            long defaultCapacity = SystemInfoHelpers.GetPageSize();
            MemoryMappedFileAccess defaultAccess = MemoryMappedFileAccess.ReadWrite;
            String fileContents = String.Empty;

            // Verify default values
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname101", 100))
            {
                VerifyCreateViewAccessor("Loc101", mmf, defaultCapacity, defaultAccess, fileContents);
            }

            // default length is full MMF
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname102", defaultCapacity * 2))
            {
                VerifyCreateViewAccessor("Loc102", mmf, defaultCapacity * 2, defaultAccess, fileContents);
            }

            // if MMF is read-only, default access throws
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname103", 100, MemoryMappedFileAccess.Read))
            {
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc103", mmf);
            }

            ////////////////////////////////////////////////////////////////////////
            // CreateViewAccessor(long, long)
            ////////////////////////////////////////////////////////////////////////

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname200", defaultCapacity * 2))
            {
                // 0
                VerifyCreateViewAccessor("Loc201", mmf, 0, 0, defaultCapacity * 2, defaultAccess, fileContents);

                // >0
                VerifyCreateViewAccessor("Loc202", mmf, 100, 0, defaultCapacity * 2 - 100, defaultAccess, fileContents);

                // >pagesize
                VerifyCreateViewAccessor("Loc203", mmf, defaultCapacity + 100, 0, defaultCapacity - 100, defaultAccess, fileContents);

                // <0
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc204", mmf, -1, 0);

                // =MMF capacity
                VerifyCreateViewAccessor("Loc205", mmf, defaultCapacity * 2, 0, 0, defaultAccess, fileContents);

                // >MMF capacity
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc206", mmf, defaultCapacity * 2 + 1, 0);
            }

            // size

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname410", defaultCapacity * 2))
            {
                // 0
                VerifyCreateViewAccessor("Loc211", mmf, 1000, 0, defaultCapacity * 2 - 1000, defaultAccess, fileContents);

                // >0, <pagesize
                VerifyCreateViewAccessor("Loc212", mmf, 100, 1000, 1000, defaultAccess, fileContents);

                // =pagesize
                VerifyCreateViewAccessor("Loc213", mmf, 100, defaultCapacity, defaultCapacity, defaultAccess, fileContents);

                // >pagesize
                VerifyCreateViewAccessor("Loc214", mmf, 100, defaultCapacity + 1000, defaultCapacity + 1000, defaultAccess, fileContents);

                // <0
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc215", mmf, 0, -1);
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc216", mmf, 0, -2);
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc217", mmf, 0, Int64.MinValue);

                // offset+size = MMF capacity
                VerifyCreateViewAccessor("Loc218", mmf, 0, defaultCapacity * 2, defaultCapacity * 2, defaultAccess, fileContents);
                VerifyCreateViewAccessor("Loc219", mmf, defaultCapacity, defaultCapacity, defaultCapacity, defaultAccess, fileContents);
                VerifyCreateViewAccessor("Loc220", mmf, defaultCapacity * 2 - 1, 1, 1, defaultAccess, fileContents);

                // offset+size > MMF capacity
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc221", mmf, 0, defaultCapacity * 2 + 1);
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc222", mmf, 1, defaultCapacity * 2);
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc223", mmf, defaultCapacity, defaultCapacity + 1);
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc224", mmf, defaultCapacity * 2, 1);

                // Int64.MaxValue - cannot exceed local address space
                if (IntPtr.Size == 4)
                    VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc225", mmf, 0, Int64.MaxValue);
                else // 64-bit machine
                    VerifyCreateViewAccessorException<IOException>("Loc225b", mmf, 0, Int64.MaxValue); // valid but too large
            }

            ////////////////////////////////////////////////////////////////////////
            // CreateViewAccessor(long, long, MemoryMappedFileAccess)
            ////////////////////////////////////////////////////////////////////////

            // [] offset

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname400", defaultCapacity * 2))
            {
                // 0
                VerifyCreateViewAccessor("Loc401", mmf, 0, 0, defaultAccess, defaultCapacity * 2, defaultAccess, fileContents);

                // >0
                VerifyCreateViewAccessor("Loc402", mmf, 100, 0, defaultAccess, defaultCapacity * 2 - 100, defaultAccess, fileContents);

                // >pagesize
                VerifyCreateViewAccessor("Loc403", mmf, defaultCapacity + 100, 0, defaultAccess, defaultCapacity - 100, defaultAccess, fileContents);

                // <0
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc404", mmf, -1, 0, defaultAccess);

                // =MMF capacity
                VerifyCreateViewAccessor("Loc405", mmf, defaultCapacity * 2, 0, defaultAccess, 0, defaultAccess, fileContents);

                // >MMF capacity
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc406", mmf, defaultCapacity * 2 + 1, 0, defaultAccess);
            }

            // size

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname410", defaultCapacity * 2))
            {
                // 0
                VerifyCreateViewAccessor("Loc411", mmf, 1000, 0, defaultAccess, defaultCapacity * 2 - 1000, defaultAccess, fileContents);

                // >0, <pagesize
                VerifyCreateViewAccessor("Loc412", mmf, 100, 1000, defaultAccess, 1000, defaultAccess, fileContents);

                // =pagesize
                VerifyCreateViewAccessor("Loc413", mmf, 100, defaultCapacity, defaultAccess, defaultCapacity, defaultAccess, fileContents);

                // >pagesize
                VerifyCreateViewAccessor("Loc414", mmf, 100, defaultCapacity + 1000, defaultAccess, defaultCapacity + 1000, defaultAccess, fileContents);

                // <0
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc415", mmf, 0, -1, defaultAccess);
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc416", mmf, 0, -2, defaultAccess);
                VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc417", mmf, 0, Int64.MinValue, defaultAccess);

                // offset+size = MMF capacity
                VerifyCreateViewAccessor("Loc418", mmf, 0, defaultCapacity * 2, defaultAccess, defaultCapacity * 2, defaultAccess, fileContents);
                VerifyCreateViewAccessor("Loc419", mmf, defaultCapacity, defaultCapacity, defaultAccess, defaultCapacity, defaultAccess, fileContents);
                VerifyCreateViewAccessor("Loc420", mmf, defaultCapacity * 2 - 1, 1, defaultAccess, 1, defaultAccess, fileContents);

                // offset+size > MMF capacity
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc421", mmf, 0, defaultCapacity * 2 + 1, defaultAccess);
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc422", mmf, 1, defaultCapacity * 2, defaultAccess);
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc423", mmf, defaultCapacity, defaultCapacity + 1, defaultAccess);
                VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc424", mmf, defaultCapacity * 2, 1, defaultAccess);

                // Int64.MaxValue - cannot exceed local address space
                if (IntPtr.Size == 4)
                    VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc425", mmf, 0, Int64.MaxValue, defaultAccess);
                else // 64-bit machine
                    VerifyCreateViewAccessorException<IOException>("Loc425b", mmf, 0, Int64.MaxValue, defaultAccess); // valid but too large
            }

            // [] access

            MemoryMappedFileAccess[] accessList;

            // existing file is ReadWriteExecute
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname431", 1000, MemoryMappedFileAccess.ReadWriteExecute))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.Write,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.CopyOnWrite,
                    MemoryMappedFileAccess.ReadExecute,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessor("Loc431_" + access, mmf, 0, 0, access, defaultCapacity, access, fileContents);
                }
            }

            // existing file is ReadExecute
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname432", 1000, MemoryMappedFileAccess.ReadExecute))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.CopyOnWrite,
                    MemoryMappedFileAccess.ReadExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessor("Loc432_" + access, mmf, 0, 0, access, defaultCapacity, access, fileContents);
                }
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Write,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc432_" + access, mmf, 0, 0, access);
                }
            }

            // existing file is CopyOnWrite
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname433", 1000, MemoryMappedFileAccess.CopyOnWrite))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.CopyOnWrite,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessor("Loc433_" + access, mmf, 0, 0, access, defaultCapacity, access, fileContents);
                }
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Write,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.ReadExecute,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc433_" + access, mmf, 0, 0, access);
                }
            }

            // existing file is ReadWrite
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname434", 1000, MemoryMappedFileAccess.ReadWrite))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.Write,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.CopyOnWrite,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessor("Loc434_" + access, mmf, 0, 0, access, defaultCapacity, access, fileContents);
                }
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.ReadExecute,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc434_" + access, mmf, 0, 0, access);
                }
            }

            // existing file is Read
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname435", 1000, MemoryMappedFileAccess.Read))
            {
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Read,
                    MemoryMappedFileAccess.CopyOnWrite,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessor("Loc435_" + access, mmf, 0, 0, access, defaultCapacity, access, fileContents);
                }
                accessList = new MemoryMappedFileAccess[] {
                    MemoryMappedFileAccess.Write,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileAccess.ReadExecute,
                    MemoryMappedFileAccess.ReadWriteExecute,
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessorException<UnauthorizedAccessException>("Loc435_" + access, mmf, 0, 0, access);
                }
            }

            // invalid enum value
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("CVA_mapname436", 1000, MemoryMappedFileAccess.ReadWrite))
            {
                accessList = new MemoryMappedFileAccess[] {
                    (MemoryMappedFileAccess)(-1),
                    (MemoryMappedFileAccess)(6),
                };
                foreach (MemoryMappedFileAccess access in accessList)
                {
                    VerifyCreateViewAccessorException<ArgumentOutOfRangeException>("Loc436_" + ((int)access), mmf, 0, 0, access);
                }
            }

            // File-backed MemoryMappedFile size should not be constrained to size of system's logical address space
            TestLargeCapacity();

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

    private void TestLargeCapacity()
    {
        try
        {
            // Prepare the test file
            using (FileStream fs = File.Open(s_fileNameForLargeCapacity, FileMode.CreateNew)) { }


            // 2^31-1, 2^31, 2^31+1, 2^32-1, 2^32, 2^32+1
            Int64[] capacities = { 2147483647, 2147483648, 2147483649, 4294967295, 4294967296, 4294967297 };

            foreach (Int64 capacity in capacities)
            {
                RunTestLargeCapacity(capacity);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("Unexpected exception in TestLargeCapacity: {0}", ex.ToString());
        }
        finally
        {
            if (File.Exists(s_fileNameForLargeCapacity))
            {
                File.Delete(s_fileNameForLargeCapacity);
            }
        }
    }

    private void RunTestLargeCapacity(Int64 capacity)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameForLargeCapacity, FileMode.Open, "CVA_RunTestLargeCapacity", capacity))
            {
                try
                {
                    // mapping all; this should fail for 32-bit
                    using (MemoryMappedViewAccessor viewAccessor =
                        mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.ReadWrite))
                    {
                        if (IntPtr.Size == 4)
                        {
                            iCountErrors++;
                            Console.WriteLine("Err440! Not throwing expected ArgumentOutOfRangeException for capacity " + capacity);
                        }
                    }
                }
                catch (ArgumentOutOfRangeException aore)
                {
                    if (IntPtr.Size != 4)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err441! Shouldn't get ArgumentOutOfRangeException on 64-bit: {0}", aore.ToString());
                    }
                    else if (capacity < UInt32.MaxValue)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err442! Expected IOExc but got ArgOutOfRangeExc");
                    }
                    else
                    {
                        // Got expected ArgumentOutOfRangeException
                    }
                }
                catch (IOException ioex)
                {
                    if (IntPtr.Size != 4)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err443! Shouldn't get IOException on 64-bit: {0}", ioex.ToString());
                    }
                    else if (capacity > UInt32.MaxValue)
                    {
                        Console.WriteLine("Err444! Expected ArgOutOfRangeExc but got IOExc: {0}", ioex.ToString());
                    }
                    else
                    {
                        // Got expected IOException
                    }
                }
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("Err445! Got unexpected exception: {0}", ex.ToString());
        }
    }

    /// START HELPER FUNCTIONS
    public void VerifyCreateViewAccessor(String strLoc, MemoryMappedFile mmf, long expectedCapacity, MemoryMappedFileAccess expectedAccess, String expectedContents)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
            {
                Eval(expectedCapacity, view.Capacity, "ERROR, {0}: Wrong capacity", strLoc);
                VerifyAccess(strLoc, view, expectedAccess);
                VerifyContents(strLoc, view, expectedContents);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateViewAccessorException<EXCTYPE>(String strLoc, MemoryMappedFile mmf) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
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

    public void VerifyCreateViewAccessor(String strLoc, MemoryMappedFile mmf, long offset, long size, long expectedCapacity, MemoryMappedFileAccess expectedAccess, String expectedContents)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(offset, size))
            {
                Eval(expectedCapacity, view.Capacity, "ERROR, {0}: Wrong capacity", strLoc);
                VerifyAccess(strLoc, view, expectedAccess);
                VerifyContents(strLoc, view, expectedContents);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateViewAccessorException<EXCTYPE>(String strLoc, MemoryMappedFile mmf, long offset, long size) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(offset, size))
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

    public void VerifyCreateViewAccessor(String strLoc, MemoryMappedFile mmf, long offset, long size, MemoryMappedFileAccess access, long expectedCapacity, MemoryMappedFileAccess expectedAccess, String expectedContents)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(offset, size, access))
            {
                Eval(expectedCapacity, view.Capacity, "ERROR, {0}: Wrong capacity", strLoc);
                VerifyAccess(strLoc, view, expectedAccess);
                VerifyContents(strLoc, view, expectedContents);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateViewAccessorException<EXCTYPE>(String strLoc, MemoryMappedFile mmf, long offset, long size, MemoryMappedFileAccess access) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(offset, size, access))
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

    void VerifyAccess(String strLoc, MemoryMappedViewAccessor view, MemoryMappedFileAccess expectedAccess)
    {
        try
        {
            bool expectedRead = ((expectedAccess == MemoryMappedFileAccess.Read) ||
                     (expectedAccess == MemoryMappedFileAccess.CopyOnWrite) ||
                     (expectedAccess == MemoryMappedFileAccess.ReadWrite) ||
                     (expectedAccess == MemoryMappedFileAccess.ReadExecute) ||
                     (expectedAccess == MemoryMappedFileAccess.ReadWriteExecute));
            bool expectedWrite = ((expectedAccess == MemoryMappedFileAccess.Write) ||
                      (expectedAccess == MemoryMappedFileAccess.CopyOnWrite) ||
                      (expectedAccess == MemoryMappedFileAccess.ReadWrite) ||
                      (expectedAccess == MemoryMappedFileAccess.ReadWriteExecute));
            Eval(expectedRead, view.CanRead, "ERROR, {0}, CanRead was wrong", strLoc);
            Eval(expectedWrite, view.CanWrite, "ERROR, {0}, CanWrite was wrong", strLoc);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}, Unexpected exception, {1}", strLoc, ex);
        }
    }

    void VerifyContents(String strLoc, MemoryMappedViewAccessor view, String expectedContents)
    {
    }
}
