using System.Security;
using System;
using System.Runtime.InteropServices;


public class EnvironmentProcessorCount
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern void GetSystemInfo(ref CPU_INFO cpuinfo);

    [StructLayout(LayoutKind.Sequential)]
    public struct CPU_INFO
    {
        public int dwOemId;
        public struct Union
        {
            public short wProcessorArchitecture;
            public short wReserved;
        }
        public int dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public int dwNumberOfProcessors;
        public int dwProcessorType;
        public int dwAllocationGranularity;
        public short wProcessorLevel;
        public short wProcessorRevision;
    }


    #region Public Methods
    [SecuritySafeCritical]
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        

        return retVal;
    }

    private int GetProcessorCount()
    {
        return System.Environment.ProcessorCount;
    }

    #region Positive Test Cases
    
    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify return-value is whether or not equal to value from OS API");

        try
        {
            if (TestLibrary.Utilities.IsWindows)
            {
                CPU_INFO cpuInfo;
                cpuInfo = new CPU_INFO();
                GetSystemInfo(ref cpuInfo);

                if (cpuInfo.dwNumberOfProcessors != GetProcessorCount())
                {
                    TestLibrary.TestFramework.LogError("001", "The ProcessorCount is wrong!");                
                    TestLibrary.TestFramework.LogError("001", " [LOCAL VARIABLES] ProcessorCount(expected value) = " + System.Environment.ProcessorCount);
                    TestLibrary.TestFramework.LogError("001", " [LOCAL VARIABLES] dwNumberOfProcessors(actual value) = " + cpuInfo.dwNumberOfProcessors);

                    retVal = false;
                }
            }
            else
            {
                if (0 >= System.Environment.ProcessorCount)
                {
                    TestLibrary.TestFramework.LogError("001", "The ProcessorCount is wrong! " + System.Environment.ProcessorCount);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    
    #endregion

    #endregion
    
    [SecuritySafeCritical]
    public static int Main()
    {
        EnvironmentProcessorCount test = new EnvironmentProcessorCount();

        TestLibrary.TestFramework.BeginTestCase("EnvironmentProcessorCount");

        if (test.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }
}
