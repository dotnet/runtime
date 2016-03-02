// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

public class IntPtrCtor_Void
{

    public static int Main()
    {
        IntPtrCtor_Void testCase = new IntPtrCtor_Void();

        TestLibrary.TestFramework.BeginTestCase("IntPtr.ctor(void*)");

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
        retVal = PosTest1("001", 0);
        retVal = PosTest1("002", Int64.MaxValue) && retVal;
        retVal = PosTest1("003", Int64.MinValue) && retVal;
        retVal = PosTest1("004", Int64.MaxValue) && retVal;
        retVal = PosTest1("005", TestLibrary.Generator.GetInt64(-55)) && retVal;
       


        return retVal;
    }

    /// <summary>
    /// for positive testing 
    /// </summary>
    /// <returns></returns>
    [SecuritySafeCritical]
    unsafe public bool PosTest1(string id, long anyValue)
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr((void *)anyValue);
            if (ip.ToPointer() != (void*)anyValue)
            {
                TestLibrary.TestFramework.LogError(id, String.Format("expect IntPtr value is {0}", anyValue));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(id, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
}
