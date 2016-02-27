// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class BooleanIConvertibleToUInt16
{

    public static int Main()
    {
        BooleanIConvertibleToUInt16 testCase = new BooleanIConvertibleToUInt16();

        TestLibrary.TestFramework.BeginTestCase("Boolean.IConvertible.ToUInt16");
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


        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            if ( (true as IConvertible).ToUInt16(null) != (ushort)1 )
            {
                TestLibrary.TestFramework.LogError("001", "expect (true as IConvertible).ToUInt16(null) == (ushort)1");
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

    public bool PosTest2()
    {
        bool retVal = true;
        try
        {
            if ((false as IConvertible).ToUInt16(null) != (ushort)0)
            {
                TestLibrary.TestFramework.LogError("002", "expect (false as IConvertible).ToUInt16(null) == (ushort)0");
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

}
