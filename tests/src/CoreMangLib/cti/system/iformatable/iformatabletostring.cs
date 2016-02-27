// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.IFormatable.ToString()
///</summary>

public class IFormatableToString
{

    public static int Main()
    {
        IFormatableToString testObj = new IFormatableToString();
        TestLibrary.TestFramework.BeginTestCase("for interface of System.IFormatable.ToString()");
        if (testObj.RunTests())
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
        retVal = PosTest1() && retVal;
        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        TestClassIFormatable tIF = new TestClassIFormatable();
        TestClassIFormatProvider tIFP = new TestClassIFormatProvider();

        String format = TestLibrary.Generator.GetString(-55, false,0,255);

        TestLibrary.TestFramework.BeginScenario("PosTest1:Override the interface and invoke it.");
        try
        {
            if (tIF.ToString(format,tIFP)!=null)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(null) !=ActualValue(not null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion

}

#region Init Derive Class

public class TestClassIFormatable:IFormattable
{
    public String ToString(String format, IFormatProvider formatProvider)
    {
        return null;
    }

}

public class TestClassIFormatProvider : IFormatProvider
{
    public Object GetFormat(Type formatType)
    {
        return null;
    }    
}

#endregion
