// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class IntPtrCtor_Int32
{

    public static int Main()
    {
        IntPtrCtor_Int32 testCase = new IntPtrCtor_Int32();

        TestLibrary.TestFramework.BeginTestCase("IntPtr.ctor(Int32)");

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
        retVal = PosTest1("001", Int32.MaxValue) && retVal;
        retVal = PosTest1("002", Int32.MinValue) && retVal;
        retVal = PosTest1("003", 0) && retVal;
        retVal = PosTest1("004", Int32.MaxValue-1) && retVal;
        retVal = PosTest1("005", Int32.MinValue+1) && retVal;
        retVal = PosTest1("006", TestLibrary.Generator.GetInt32(-55)) && retVal;

        return retVal;
    }

    /// <summary>
    /// for testing 
    /// </summary>
    /// <returns></returns>
    public bool PosTest1(string id, int i)
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr(i);
            if (ip.ToInt32() != i)
            {
                TestLibrary.TestFramework.LogError(id,
                    String.Format("IntPtr value expect {0}", i));
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
