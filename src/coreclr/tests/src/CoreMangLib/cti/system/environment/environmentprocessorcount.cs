using System.Security;
using System;
using TestLibrary;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEM_INFO
{
    internal int dwOemId;    // This is a union of a DWORD and a struct containing 2 WORDs.
    internal int dwPageSize;
    internal IntPtr lpMinimumApplicationAddress;
    internal IntPtr lpMaximumApplicationAddress;
    internal IntPtr dwActiveProcessorMask;
    internal int dwNumberOfProcessors;
    internal int dwProcessorType;
    internal int dwAllocationGranularity;
    internal short wProcessorLevel;
    internal short wProcessorRevision;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);
}

public class EnvironmentProcessorCount
{
    public static int Main(string[] args)
    {
        EnvironmentProcessorCount test = new EnvironmentProcessorCount();
        TestFramework.BeginScenario("Testing Environment.ProcessorCount");

        if (test.RunTests())
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestFramework.BeginScenario("Test ProcessorCount. If host is Windows, verify count.");

        try
        {
            int actual = Environment.ProcessorCount;
            if (Utilities.IsWindows) retVal = VerifyProcessorCount() && retVal;
        }
        catch (Exception e)
        {
            TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
    [SecuritySafeCritical]
    bool VerifyProcessorCount()
    {
        SYSTEM_INFO sysInfo = new SYSTEM_INFO();
        SYSTEM_INFO.GetSystemInfo(ref sysInfo);
        if (Environment.ProcessorCount != sysInfo.dwNumberOfProcessors)
        {
            TestFramework.LogError("001", @"ProcessorCount not as expected. Expected (from Win32): " + sysInfo.dwNumberOfProcessors.ToString() + ", Actual (from CLR): " + Environment.ProcessorCount.ToString());
            return false;
        }
        return true;
    }
}
