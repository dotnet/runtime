using System.Security;
using System;
using System.Runtime.InteropServices;


[SecuritySafeCritical]

/// <summary>
/// GetLastWin32Error
/// </summary>
public class MarshalGetLastWin32Error
{
    #region Private Fields
    private const int c_MIN_STRING_LENGTH = 300;
    private const int c_MAX_STRING_LENGTH = 301;
    private const int CREATE_ALWAYS = unchecked((int)0x2);
    private const int FILE_ATTRIBUTE_NORMAL = unchecked((int)0x00000080L);
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        if (TestLibrary.Utilities.IsWindows)
        {
            retVal = PosTest1() && retVal;
            retVal = PosTest2() && retVal;
            retVal = PosTest3() && retVal;
        }
#if !WinCoreSys
        else
        {
            retVal = PosMacTest1() && retVal;
        }
#endif
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        string filePath = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetLastWin32Error for P/Invoke functions with SetLastError flags sets to true");

        try
        {
            CopyFile("MarshalGetLastWin32Error_DoesnotExist1.tx", "MarshalGetLastWin32Error_DoesnotExist2.txt", true);
            if (Marshal.GetLastWin32Error() == 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "Call GetLastWin32Error for P/Invoke functions with SetLastError flags sets to true dose not return 0 when call function successed");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] filePath = " + filePath);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            SetLastError(0);
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string filePath = null;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call GetLastWin32Error for P/Invoke functions with SetLastError flags sets to true");

        try
        {
            /*
             * GetProcessHeap sets last error only if it fails.
             * In case of success, the value is not set. So we are checking only failure scenario here.
             */
            IntPtr pHandle = GetProcessHeap();
            if ((pHandle == null) && (Marshal.GetLastWin32Error() == 0))
            {
                TestLibrary.TestFramework.LogError("002.1", "Call GetLastWin32Error for P/Invoke functions with SetLastError flags sets to true returned 0 even if call failed");
                retVal = false;
            }
            filePath = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            CreateFile(filePath, 0, 0, IntPtr.Zero, CREATE_ALWAYS, 0, IntPtr.Zero);

            if (Marshal.GetLastWin32Error() == 0)
            {
                TestLibrary.TestFramework.LogError("002.2", "Call GetLastWin32Error for P/Invoke functions with SetLastError flags sets to true returns 0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] filePath = " + filePath);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] filePath = " + filePath);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            SetLastError(0);
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        uint desiredCode = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call GetLastWin32Error for P/Invoke functions with after call SetLastError");

        try
        {
            desiredCode = (uint)TestLibrary.Generator.GetInt32(-55);
            SetLastError(desiredCode);
            int actualCode = Marshal.GetLastWin32Error();

            if (desiredCode != actualCode)
            {
                TestLibrary.TestFramework.LogError("003.1", "Call GetLastWin32Error for P/Invoke functions with after call SetLastError returns wrong value");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] desiredCode = " + desiredCode + ", actualCode = " + actualCode);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] desiredCode = " + desiredCode);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
#if !WinCoreSys
    public bool PosMacTest1()
    {
        bool   retVal = true;
        string pwd    = null;

        TestLibrary.TestFramework.BeginScenario("PosMacTest1: Call GetLastWin32Error for P/Invoke functions with SetLastError flags sets to true");

        try
        {
            pwd = getenv("PWD");

            if (Marshal.GetLastWin32Error() != 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Call GetLastWin32Error for P/Invoke functions with SetLastError flags sets to true dose not return 0 when call function successed");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
#endif
    #endregion
    #endregion

    public static int Main()
    {
        MarshalGetLastWin32Error test = new MarshalGetLastWin32Error();

        TestLibrary.TestFramework.BeginTestCase("MarshalGetLastWin32Error");

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

    #region Private Methods

#if !WinCoreSys
    [SecurityCritical]
    [DllImport("/usr/lib/libc.dylib")]
    private static extern string getenv(string key);
#endif

    [DllImport("kernel32.dll", SetLastError = true)]
    private extern static void SetLastError(uint dwErrCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private extern static IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("Kernel32.dll", SetLastError=true)]
    private extern static IntPtr GetProcessHeap();

    [DllImport("Kernel32.dll", SetLastError = true)]
    private extern static bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);
    #endregion
}
