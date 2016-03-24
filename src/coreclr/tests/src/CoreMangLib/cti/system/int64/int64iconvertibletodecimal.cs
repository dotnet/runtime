// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// Int64.System.IConvertible.ToDecimal(IFormatProvider)
/// </summary>
public class Int64IConvertibleToDecimal
{
    public static int Main()
    {
        Int64IConvertibleToDecimal ui64IContDecimal = new Int64IConvertibleToDecimal();
        TestLibrary.TestFramework.BeginTestCase("Int64IConvertibleToDecimal");
        if (ui64IContDecimal.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[PosTest]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The Int64 MaxValue IConvertible To Decimal");
        try
        {
            long int64A = Int64.MaxValue;
            IConvertible iConvert = (IConvertible)(int64A);
            decimal decimalA = iConvert.ToDecimal(provider);
            if (decimalA != int64A)
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
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
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:The Int64 MinValue IConvertible To Decimal");
        try
        {
            long int64A = Int64.MinValue;
            IConvertible iConvert = (IConvertible)(int64A);
            decimal decimalA = iConvert.ToDecimal(null);
            if (decimalA != int64A)
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
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
    public bool PosTest3()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest3:The random Int64 IConvertible To Decimal 1");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            IConvertible iConvert = (IConvertible)(int64A);
            decimal decimalA = iConvert.ToDecimal(provider);
            if (decimalA != int64A)
            {
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:The random Int64 IConvertible To Decimal 2");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            IConvertible iConvert = (IConvertible)(int64A);
            decimal decimalA = iConvert.ToDecimal(null);
            if (decimalA != int64A)
            {
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
