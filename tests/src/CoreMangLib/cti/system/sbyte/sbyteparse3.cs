// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// SByte.Parse(String,NumberStyle,IFormatProvider)
/// </summary>
public class SByteParse3
{
    public static int Main()
    {
        SByteParse3 sbyteParse3 = new SByteParse3();
        TestLibrary.TestFramework.BeginTestCase("SByteParse3");
        if (sbyteParse3.RunTests())
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negative]");
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
        IFormatProvider provider = new CultureInfo("en-us");
        TestLibrary.TestFramework.BeginScenario("PosTest1: the string represents SByte MaxValue 1");
        try
        {
            string sourceStr = SByte.MaxValue.ToString();
            NumberStyles style = NumberStyles.Any;
            sbyte SByteVale = sbyte.Parse(sourceStr, style, provider);
            if (SByteVale != sbyte.MaxValue)
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
        IFormatProvider provider = new CultureInfo("el-GR");
        TestLibrary.TestFramework.BeginScenario("PosTest2: the string represents sbyte MaxValue 2");
        try
        {
            string sourceStr = sbyte.MaxValue.ToString();
            NumberStyles style = NumberStyles.Any;
            sbyte SByteVal = sbyte.Parse(sourceStr, style, provider);
            if (SByteVal != sbyte.MaxValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: the string represents sbyte MinValue 1");
        try
        {
            string sourceStr= sbyte.MinValue.ToString();
            NumberStyles style = NumberStyles.Any;
            sbyte SByteVal = sbyte.Parse(sourceStr, style, provider);
            if (SByteVal != sbyte.MinValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: the string represents sbyte MinValue 2");
        try
        {
            string sourceStr= sbyte.MinValue.ToString();
            NumberStyles style = NumberStyles.Any;
            sbyte SByteVal = sbyte.Parse(sourceStr, style, provider);
            if (SByteVal != sbyte.MinValue)
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
            sbyte SByteVal = (sbyte)this.GetInt32(0, 127);
            string sourceStr = "\u0020" + "+" + SByteVal.ToString();
            NumberStyles style = NumberStyles.Any;
            sbyte result = sbyte.Parse(sourceStr, style, provider);
            if (result != SByteVal)
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: the string format is [ws][sign]digits[ws] 2");

        try
        {
            sbyte SByteVal = (sbyte)this.GetInt32(0, 128);
            string sourceStr = "\u0020" + "-" + SByteVal.ToString();
            NumberStyles style = NumberStyles.Any;
            sbyte result = sbyte.Parse(sourceStr, style, provider);
            if (result != SByteVal * (-1))
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
            sbyte SByteVal = (sbyte)this.GetInt32(0, 128);
            string sourceStr = "\u0020" + "-" + SByteVal.ToString() + "\u0020";
            NumberStyles style = NumberStyles.Any;
            sbyte result = sbyte.Parse(sourceStr, style, provider);
            if (result != SByteVal * (-1))
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
            sbyte SByteVal = (sbyte)this.GetInt32(0, 128);
            string sourceStr = "\u0020" + "-0" + SByteVal.ToString() + "\u0020";
            NumberStyles style = NumberStyles.Any;
            sbyte result = sbyte.Parse(sourceStr, style, provider);
            if (result != SByteVal * (-1))
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
            string sourceStr = "\u0020" + "7f" + "\u0020";
            NumberStyles style = NumberStyles.HexNumber;
            sbyte result = sbyte.Parse(sourceStr, style, provider);
            if (result != sbyte.MaxValue)
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
        TestLibrary.TestFramework.BeginScenario("NegTest1: the string is null");
        try
        {
            string sourceStr = null;
            NumberStyles style = NumberStyles.Any;
            IFormatProvider provider = new CultureInfo("en-us");
            sbyte desSByte = sbyte.Parse(sourceStr, style, provider);
            TestLibrary.TestFramework.LogError("N001", "the string is null but not throw exception");
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: the string does not consist of an optional sign follow by a sequence of digit");
        try
        {
            string sourceStr = "helloworld";
            NumberStyles style = NumberStyles.Any;
            IFormatProvider provider = new CultureInfo("en-us");
            sbyte desSByte = sbyte.Parse(sourceStr, style, provider);
            TestLibrary.TestFramework.LogError("N003", "the string does not consist of an optional sign follow by a sequence of digit but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the string represents digit less than SByte MinValue");
        try
        {
            string sourceStr = "-129";
            NumberStyles style = NumberStyles.Any;
            IFormatProvider provider = new CultureInfo("en-us");
            sbyte desSByte = sbyte.Parse(sourceStr, style, provider);
            TestLibrary.TestFramework.LogError("N005", "the string represents digit less than SByte MinValue");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the string represents digit greater than SByte MaxValue");
        try
        {
            string sourceStr = "128";
            NumberStyles style = NumberStyles.Any;
            IFormatProvider provider = new CultureInfo("en-us");
            sbyte desSByte = sbyte.Parse(sourceStr, style, provider);
            TestLibrary.TestFramework.LogError("N007", "the string represents digit greater than SByte MaxValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5: the NumberStyle value is not a NumberStyle");
        try
        {
            string sourceStr = this.GetInt32(0, 128).ToString();
            NumberStyles style = (NumberStyles)(-1);
            IFormatProvider provider = new CultureInfo("en-us");
            sbyte desSByte = sbyte.Parse(sourceStr, style, provider);
            TestLibrary.TestFramework.LogError("N009", "the NumberStyle value is not a NumberStyle but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region HelpMethod
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
     #endregion
}
