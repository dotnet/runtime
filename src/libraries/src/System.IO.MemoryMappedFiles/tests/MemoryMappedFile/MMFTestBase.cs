// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

public class MMFTestBase
{
    public String strLoc = "baseLoc";
    public int iCountTestcases = 0;
    public int iCountErrors = 0;

    public bool Eval(bool exp)
    {
        return Eval(exp, null);
    }
    public bool Eval(bool exp, String errorMsg)
    {
        if (!exp)
        {
            iCountErrors++;
            String err = errorMsg;
            if (err == null)
                err = "Test Failed at location: " + strLoc;
            Console.WriteLine(err);
        }
        return exp;
    }

    public bool Eval(bool exp, String format, params object[] arg)
    {
        if (!exp)
        {
            return Eval(exp, String.Format(format, arg));
        }
        return true;
    }

    public bool Eval<T>(T expected, T actual, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            return Eval(retValue, errorMsg +
                "   Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                "   Actual  :" + (null == actual ? "<null>" : actual.ToString()));
        return true;
    }

    public bool Eval<T>(T expected, T actual, String format, params object[] arg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            return Eval(retValue, String.Format(format, arg) +
                "   Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                "   Actual:  " + (null == actual ? "<null>" : actual.ToString()));

        return true;
    }

    // If this flag is set, a child process created with the bInheritHandles parameter of CreateProcess set to TRUE will inherit the object handle. 
    private const uint HANDLE_FLAG_INHERIT = 0x00000001;
    //If this flag is set, calling the CloseHandle function will not close the object handle.
    //const uint HANDLE_FLAG_PROTECT_FROM_CLOSE = 0x00000002;

    [DllImport("kernel32.dll")]
    static extern bool GetHandleInformation(IntPtr hObject, out uint lpdwFlags);

    public void VerifyHandleInheritability(String strLoc, SafeMemoryMappedFileHandle handle, HandleInheritability inheritability)
    {
        uint expected;
        if (inheritability == HandleInheritability.Inheritable)
            expected = HANDLE_FLAG_INHERIT;
        else if (inheritability == HandleInheritability.None)
            expected = 0;
        else
            throw new Exception("Test error!  Invalid HandleInheritability passed to VerifyHandleInheritability");

        uint flags;
        bool result = GetHandleInformation(handle.DangerousGetHandle(), out flags);

        Eval(expected, (flags & HANDLE_FLAG_INHERIT), "ERROR, {0}: HandleInheritability was wrong.", strLoc);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class MEMORYSTATUSEX
    {
        internal MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        }
        internal uint dwLength;
        internal uint dwMemoryLoad;
        internal ulong ullTotalPhys;
        internal ulong ullAvailPhys;
        internal ulong ullTotalPageFile;
        internal ulong ullAvailPageFile;
        internal ulong ullTotalVirtual;
        internal ulong ullAvailVirtual;
        internal ulong ullAvailExtendedVirtual;
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public ulong GetAvailPageFile()
    {
        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
        bool result = GlobalMemoryStatusEx(memStatus);
        ulong availPageFile = memStatus.ullAvailPageFile;
        return availPageFile;
    }

    public void VerifyAccess(String strLoc, MemoryMappedFile mmf, MemoryMappedFileAccess expectedAccess, long capacity)
    {
        bool expectedRead = ((expectedAccess == MemoryMappedFileAccess.Read) ||
                     (expectedAccess == MemoryMappedFileAccess.CopyOnWrite) ||
                     (expectedAccess == MemoryMappedFileAccess.ReadWrite) ||
                     (expectedAccess == MemoryMappedFileAccess.ReadExecute) ||
                     (expectedAccess == MemoryMappedFileAccess.ReadWriteExecute));
        bool expectedWrite = ((expectedAccess == MemoryMappedFileAccess.Write) ||
                      (expectedAccess == MemoryMappedFileAccess.ReadWrite) ||
                      (expectedAccess == MemoryMappedFileAccess.ReadWriteExecute));

        //Console.WriteLine("Access={0}, R={1} W={2}", expectedAccess, expectedRead, expectedWrite);

        TestAccess(strLoc + "_1", mmf, MemoryMappedFileAccess.Read, expectedRead, true, false);
        TestAccess(strLoc + "_2", mmf, MemoryMappedFileAccess.Write, expectedWrite, false, true);
        TestAccess(strLoc + "_3", mmf, MemoryMappedFileAccess.ReadWrite, expectedWrite, true, true);
    }

    public void TestAccess(String strLoc, MemoryMappedFile mmf, MemoryMappedFileAccess access, bool expectedPass, bool expectedRead, bool expectedWrite)
    {
        if (expectedPass)
        {
            try
            {
                // Create ViewAccessor with specified access
                using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, 0, access))
                {
                    Eval(expectedRead, accessor.CanRead, "ERROR, {0}, CanRead was wrong", strLoc);
                    Eval(expectedWrite, accessor.CanWrite, "ERROR, {0}, CanWrite was wrong", strLoc);
                }
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}, Unexpected exception creating a {1} ViewAccessor, {2}", strLoc, access, ex);
            }
        }
        else
        {
            try
            {
                using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, 0, access))
                {
                    iCountErrors++;
                    Console.WriteLine("ERROR, {0}, Created {1} ViewAccessor from MemoryMappedFile", strLoc, access);
                }
            }
            catch (UnauthorizedAccessException)
            {
                //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
            }
            catch (IOException)
            {
                // Valid if MMF is being reopened.
            }

            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}, Unexpected exception creating a {1} ViewAccessor, {2}", strLoc, access, ex);
            }
        }
    }
}
