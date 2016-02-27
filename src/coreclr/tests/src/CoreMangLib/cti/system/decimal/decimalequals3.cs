// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///Equals(System.Object)
/// </summary>
public class DecimalEquals3
{
    #region const
    private const int SEEDVALUE = 2;
    private const int EQUALVALUE = 1;
    private const int ZEROVALUE = 0;
    #endregion
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: compare to itself.");

        try
        {

            Decimal myDecimal1 = new decimal(TestLibrary.Generator.GetInt32(-55));
            object myValue = myDecimal1;
            if (!myDecimal1.Equals(myValue))
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling Equals method should return true" );
                retVal = false;
            }

            myDecimal1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            myValue = myDecimal1;
            if (!myDecimal1.Equals(myValue))
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling Equals method should return true");
                retVal = false;
            }
            myDecimal1 = new decimal(TestLibrary.Generator.GetDouble(-55));
            myValue = myDecimal1;
            if (!myDecimal1.Equals(myValue))
            {
                TestLibrary.TestFramework.LogError("001.3", "Calling Equals method should return true");
                retVal = false;
            }
         }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare to difference value.");

        try
        {

            Decimal myDecimal1 = new decimal(TestLibrary.Generator.GetInt32(-55));
            Decimal myDecimal2 = new decimal(TestLibrary.Generator.GetSingle(-55));
            object myValue = myDecimal2;
            if (myDecimal1.Equals(myValue))
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling Equals method should return false." );
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare to difference value.");

        try
        {

            int myInt = TestLibrary.Generator.GetInt32(-55);
            Decimal myDecimal1 = new decimal(myInt);
            Decimal myDecimal2 = new decimal(myInt);
            object myValue = myDecimal2;
            if (!myDecimal1.Equals(myValue))
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling Equals method should return true.");
                retVal = false;
            }
            Single mySingle = TestLibrary.Generator.GetSingle(-55);
            myDecimal1 = new decimal(mySingle);
            myDecimal2 = new decimal(mySingle);
            myValue = myDecimal2;
            if (!myDecimal1.Equals(myValue))
            {
                TestLibrary.TestFramework.LogError("003.2", "Calling Equals method should return true.");
                retVal = false;
            }
            double myDouble = TestLibrary.Generator.GetDouble(-55);
            myDecimal1 = new decimal(myDouble);
            myDecimal2 = new decimal(myDouble);
            myValue = myDecimal2;
            if (!myDecimal1.Equals(myValue))
            {
                TestLibrary.TestFramework.LogError("003.3", "Calling Equals method should return true.");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
   
    #endregion

    #endregion

    public static int Main()
    {
        DecimalEquals3 test = new DecimalEquals3();

        TestLibrary.TestFramework.BeginTestCase("DecimalEquals3");

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
