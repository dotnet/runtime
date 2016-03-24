// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
/// <summary>
/// Int64.Parse(string,NumberStyle,IFormatProvider)
/// </summary>
public class Int64Parse3
{
    private long c_INT64_MinValue = -9223372036854775808;
    private long c_INT64_MaxValue = 9223372036854775807;
    public static int Main()
    {
        Int64Parse3 int64parse3 = new Int64Parse3();
        TestLibrary.TestFramework.BeginTestCase("Int64Parse3");
        if (int64parse3.RunTests())
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        TestLibrary.TestFramework.LogInformation("[NegTest]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1: the string corresponding Int64 is Int64 MaxValue 1");
        try
        {
            string strA = Int64.MaxValue.ToString();
            NumberStyles style = NumberStyles.Any;
            long int64A = Int64.Parse(strA, style, provider);
            if (int64A != c_INT64_MaxValue)
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
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest2: the string corresponding Int64 is Int64 MaxValue 2");
        try
        {
            string strA = Int64.MaxValue.ToString();
            NumberStyles style = NumberStyles.Any;
            long int64A = Int64.Parse(strA, style, provider);
            if (int64A != c_INT64_MaxValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: the string corresponding Int64 is Int64 MinValue 1");
        try
        {
            string strA = Int64.MinValue.ToString();
            NumberStyles style = NumberStyles.Any;
            long int64A = Int64.Parse(strA, style, provider);
            if (int64A != c_INT64_MinValue)
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
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest4: the string corresponding Int64 is Int64 MinValue 2");
        try
        {
            string strA = Int64.MinValue.ToString();
            NumberStyles style = NumberStyles.Any;
            long int64A = Int64.Parse(strA, style, provider);
            if (int64A != c_INT64_MinValue)
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
    public bool PosTest5()
    {
        bool retVal = true;
        CultureInfo mycultrue = new CultureInfo("en-us");
        IFormatProvider provider = mycultrue.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest5: the string format is [ws][sign]digits[ws] 1");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            string strA = "\u0020" + "+" + int64A.ToString();
            NumberStyles style = NumberStyles.Any;
            long result = Int64.Parse(strA, style, provider);
            if (result != int64A)
            {
                TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        CultureInfo mycultrue = new CultureInfo("en-us");
        IFormatProvider provider = mycultrue.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest5: the string format is [ws][sign]digits[ws] 2");

        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            string strA = "\u0020" + "-" + int64A.ToString();
            NumberStyles style = NumberStyles.Any;
            long result = Int64.Parse(strA, style, provider);
            long int64B = int64A * (-1);
            if (result != int64B)
            {
                TestLibrary.TestFramework.LogError("011", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest7()
    {
        bool retVal = true;
        CultureInfo mycultrue = new CultureInfo("en-us");
        IFormatProvider provider = mycultrue.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest7: the string format is [ws][sign]digits[ws] 3");

        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            string strA = "\u0020" + "-" + int64A.ToString() + "\u0020";
            NumberStyles style = NumberStyles.Any;
            long result = Int64.Parse(strA, style, provider);
            long int64B = int64A * (-1);
            if (result != int64B)
            {
                TestLibrary.TestFramework.LogError("013", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest8()
    {
        bool retVal = true;
        CultureInfo mycultrue = new CultureInfo("en-us");
        IFormatProvider provider = mycultrue.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest8: the string format is [ws][sign]digits[ws] 4");

        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            string strA = "\u0020" + "-0" + int64A.ToString() + "\u0020";
            NumberStyles style = NumberStyles.Any;
            long result = Int64.Parse(strA, style, provider);
            long int64B = int64A * (-1);
            if (result != int64B)
            {
                TestLibrary.TestFramework.LogError("015", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest9()
    {
        bool retVal = true;
        CultureInfo mycultrue = new CultureInfo("en-us");
        IFormatProvider provider = mycultrue.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest9: the string formed with acceptable characters");

        try
        {
            long int64A = 0xabcdfffffabc;
            string strA = "\u0020" + "-0" + int64A.ToString() + "\u0020";
            NumberStyles style = NumberStyles.Any;
            long result = Int64.Parse(strA, style, provider);
            long int64B = int64A * (-1);
            if (result != int64B)
            {
                TestLibrary.TestFramework.LogError("017", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        long int64A;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the parameter string is null");

        try
        {
            string strA = null;
            NumberStyles style = NumberStyles.Any;
            int64A = Int64.Parse(strA, style, provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N001", "the param string is null but not throw exception");
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        long int64A;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest2: the parameter string is  not correct format");

        try
        {
            string strA = "#$%abc";
            NumberStyles style = NumberStyles.Any;
            int64A = Int64.Parse(strA, style, provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N003", "the param string is null but not throw exception");
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the parameter string parsed number is less than the Int64 minValue");

        try
        {
            string strA = "-9223372036854775809";
            NumberStyles style = NumberStyles.Any;
            Int64 int64A = Int64.Parse(strA, style, provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N005", "the param string parsed number is less than MinValue but not throw exception");
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        long int64A;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the parameter string parsed number is greater than the Int64 maxValue");

        try
        {
            string strA = "9223372036854775808";
            NumberStyles style = NumberStyles.Any;
            int64A = Int64.Parse(strA, style, provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N007", "the param string parsed number is greater than MaxValue but not throw exception");
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest5()
    {
        bool retVal = true;
        long int64A;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest5: the param of style is not NumberStyles value");

        try
        {
            string strA = TestLibrary.Generator.GetInt64(-55).ToString();
            NumberStyles style = (NumberStyles)(Int32.MaxValue);
            int64A = Int64.Parse(strA, style, provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N009", "the param of style is not NumberStyles value but not throw exception");
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N010", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
