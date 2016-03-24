// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Int32.Equals(System.Int32)
public class Int32Equals1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]"); 
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1:Test random number ");

        try
        {
            Int32 i1, i2;
            i1 = i2 = TestLibrary.Generator.GetInt32(-55);
            if (!i1.Equals(i2))
            {
                TestLibrary.TestFramework.LogError("001", String.Format("equal two equal number {0} did not return true", i2));
                retVal = false;
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

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2:Test two different number");

        try
        {
            
            Int32 i1 = TestLibrary.Generator.GetInt32(-55);
            Int32 i2;
            if (i1 == Int32.MaxValue)
            {
                i2 = i1-1;
            }
            else
            {
                i2 = i1+1;
            }
            if (i1.Equals(i2))
            {

                TestLibrary.TestFramework.LogError("003", String.Format("compare two number which is not equal did not return false: {0} and {1}",i1,i2 ));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Test 0 equals 0 ");

        try
        {
            Int32 i1, i2;
            i1 = i2 = 0;
            if (!i1.Equals(i2))
            {
                TestLibrary.TestFramework.LogError("005", "0!=0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
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
        Int32Equals1 test = new Int32Equals1();

        TestLibrary.TestFramework.BeginTestCase("Int32Equals1");

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
