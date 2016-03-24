// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

public class IntPtrEquals
{

    public static int Main()
    {
        IntPtrEquals testCase = new IntPtrEquals();

        TestLibrary.TestFramework.BeginTestCase("IntPtr.Equals(Object)");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr(0);
            if (ip.Equals(null))
            {
                TestLibrary.TestFramework.LogError("001", "expect new IntPtr(0).Equals(null)");
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
            System.IntPtr ip1 = new IntPtr((void*)mem);
            System.IntPtr ip2 = new IntPtr((void*)mem);
            if (!ip1.Equals(ip2))
            {
                TestLibrary.TestFramework.LogError("002", "expect two IntPtr equals");
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
            System.IntPtr ip1 = new IntPtr(anyAddr);
            System.IntPtr ip2 = new IntPtr(anyAddr);
            if (!ip1.Equals(ip2))
            {
                TestLibrary.TestFramework.LogError("003", "expect two IntPtr equals");
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

    [SecuritySafeCritical]
    unsafe public bool NegTest1()
    {
        bool retVal = true;
        try
        {
            byte* mem1 = stackalloc byte[1];
            byte* mem2 = stackalloc byte[1];
            mem1[0] = mem1[0] = TestLibrary.Generator.GetByte(-55);
            System.IntPtr ip1 = new IntPtr((void*)mem1);
            System.IntPtr ip2 = new IntPtr((void*)mem2);
            if (ip1.Equals(ip2))
            {
                TestLibrary.TestFramework.LogError("001", "expect two IntPtrs NOT equals");
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

    public bool NegTest2()
    {
        bool retVal = true;
        try
        {
            int anyAddr = TestLibrary.Generator.GetInt32(-55);
            if (anyAddr == Int32.MaxValue)
                anyAddr -= 1;
            else if (anyAddr == Int32.MinValue)
                anyAddr += 1;

            System.IntPtr ip1 = new IntPtr(anyAddr);
            System.IntPtr ip2 = new IntPtr(anyAddr + 1);
            System.IntPtr ip3 = new IntPtr(anyAddr - 1);
            if (ip1.Equals(ip2))
            {
                TestLibrary.TestFramework.LogError("002", "expect two IntPtrs NOT equals");
                retVal = false;
            }

            if (ip1.Equals(ip3))
            {
                TestLibrary.TestFramework.LogError("002", "expect two IntPtrs NOT equals");
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

    [SecuritySafeCritical]
    unsafe public bool NegTest3()
    {
        bool retVal = true;
        try
        {
            object o = new Object();
            System.IntPtr ip = new IntPtr(TestLibrary.Generator.GetInt32(-55));
            if (ip.Equals(o))
            {
                TestLibrary.TestFramework.LogError("003", "expect IntPtr NOT equals an object referece");
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
}
