// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class BooleanIConvertableToBoolean
{
    #region MainEntry
    public static int Main()
    {
        BooleanIConvertableToBoolean testCase = new BooleanIConvertableToBoolean();

        TestLibrary.TestFramework.BeginTestCase("Boolean.IConvertible.ToBoolean");
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
    #endregion

    #region Run Cases
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;


        return retVal;
    }
    #endregion

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            if ( (true as IConvertible).ToBoolean(null) != true)
            {
                TestLibrary.TestFramework.LogError("001", "expect (true as IConvertible).ToBoolean(null) == true");
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
            if ( (false as IConvertible).ToBoolean(null) != false)
            {
                TestLibrary.TestFramework.LogError("002", "(false as IConvertible).ToBoolean(null) == false");
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
