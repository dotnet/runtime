using System.Security;
using System;
using System.Runtime.InteropServices;
using TestLibrary;
	

[SecuritySafeCritical]
/// <summary>
/// ReadInt64(System.IntPtr)
/// </summary>
public class MarshalReadInt641
{
    #region Private Fields
    private const uint c_SIZE_OF_LONG = 8;
    private const uint GPTR = 0x0040;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");

        return retVal;
    }

    #region Positive Test Cases
    public unsafe bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ReadInt64 to read a long from unmanaged memory to managed memory");

        try
        {
            IntPtr ptr = IntPtr.Zero;
            long expected = TestLibrary.Generator.GetInt64(-55);

            ptr = AllocWrapper((uint) c_SIZE_OF_LONG);
            long* pointer = (long*)ptr.ToPointer();
            *pointer = expected;

            long actual = Marshal.ReadInt64(ptr);
            FreeWrapper(ptr);
            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling ReadInt64 reads wrong data from unmanaged memory to managed memory");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases

    #endregion
    #endregion

    public static int Main()
    {
        MarshalReadInt641 test = new MarshalReadInt641();

        TestLibrary.TestFramework.BeginTestCase("MarshalReadInt641");

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
    #region Windows P/invoke Declarations
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint uFlags, uint dwBytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalFree(IntPtr hMem);
    #endregion

    #region Macintosh P/invoke Declarations
    [DllImport("libc.dylib")]
    private static extern IntPtr malloc(uint siz);

    [DllImport("libc.dylib")]
    private static extern void free(IntPtr ptr);
    #endregion

    // delegate unmanaged-memory allocation calls to either GlobalAlloc (Windows) or malloc (Mac)
    private IntPtr AllocWrapper(uint nBytes)
    {
        if (Utilities.IsWindows)
            return GlobalAlloc(GPTR, nBytes);
        else
            return malloc(nBytes);
    }

    // delegate unmanaged-memory release calls to either GlobalFree (Windows) or free (Mac)
    private void FreeWrapper(IntPtr ptr)
    {
        if (Utilities.IsWindows)
            GlobalFree(ptr);
        else
            free(ptr);
    }

    #endregion
}
