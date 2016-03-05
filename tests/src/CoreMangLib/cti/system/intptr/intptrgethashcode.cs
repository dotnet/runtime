// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

public class IntPtrGetHashCode
{

    public static int Main()
    {
        IntPtrGetHashCode testCase = new IntPtrGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("IntPtr.GetHashCode()");
        if (testCase.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr(0);
            if (ip.GetHashCode() != 0)
            {
                TestLibrary.TestFramework.LogError("001", "expect IntPtr(0).GetHashCode() == 0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    [SecuritySafeCritical]
    unsafe public bool PosTest2()
    {
        bool retVal = true;
        try
        {
            byte* mem = stackalloc byte[1024];
            System.IntPtr ip = new IntPtr((void*)mem);
            if (ip.GetHashCode() != (int)mem)
            {
                TestLibrary.TestFramework.LogError("002", "expect IntPtr.GetHashCode() equals the address");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        try
        {
            int anyAddr = TestLibrary.Generator.GetInt32(-55);
            System.IntPtr ip = new IntPtr(anyAddr);
            if (ip.GetHashCode() != anyAddr )
            {
                TestLibrary.TestFramework.LogError("003", String.Format("expect IntPtr.GetHashCode() == {0}", anyAddr));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    
    public bool PosTest4()
    {
        bool retValue = true;
        try
        {
            long addressOne = 0x123456FFFFFFFFL;
            long addressTwo = 0x654321FFFFFFFFL;
            System.IntPtr ipOne = new IntPtr(addressOne);
            System.IntPtr ipTwo = new IntPtr(addressTwo);
            if (ipOne.GetHashCode() == ipTwo.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("004", "expect different hashcodes.");
                retVal = false;
            }
        }
        catch (System.OverflowException ex)
        {
            if (System.IntPtr.Size == 4)
            {
                // ok, that's what it should be
                return retVal;
            }
            else
		   	{
                TestLibrary.TestFramework.LogError(id, String.Format("IntPtr should not have thrown an OverflowException for value {0}: ", i) + ex.ToString());
                retVal = false;
		   	}
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}
