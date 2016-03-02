// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///  SByte.System.IConvertible.ToBoolean(IFormatProvider)
/// </summary>
public class SByteIConvertibleToBoolean
{
    public static int Main()
    {
        SByteIConvertibleToBoolean sbyteIContBool = new SByteIConvertibleToBoolean();
        TestLibrary.TestFramework.BeginTestCase("SByteIConvertibleToBoolean");
        if (sbyteIContBool.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The sbyte MaxValue IConvertible To Boolean");
        try
        {
            sbyte sbyteVal = sbyte.MaxValue;
            IConvertible iConvert = (IConvertible)(sbyteVal);
            bool boolA = iConvert.ToBoolean(provider);
            if (!boolA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:The sbyte MinValue IConvertible To Boolean");
        try
        {
            sbyte sbyteVal = sbyte.MinValue;
            IConvertible iConvert = (IConvertible)(sbyteVal);
            bool boolA = iConvert.ToBoolean(null);
            if (!boolA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:The random sbyte IConvertible To Boolean 1");
        try
        {
            sbyte sbyteVal = 0;
            IConvertible iConvert = (IConvertible)(sbyteVal);
            bool boolA = iConvert.ToBoolean(null);
            if (boolA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:The random sbyte IConvertible To Boolean 2");
        try
        {
            sbyte sbyteVal = (sbyte)(this.GetInt32(0, 127));
            IConvertible iConvert = (IConvertible)(sbyteVal);
            bool boolA = iConvert.ToBoolean(null);
            if (sbyteVal == 0)
            {
                if (boolA)
                {
                    TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
            }
            else
            {
                if (!boolA)
                {
                    TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
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
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest5:The random sbyte IConvertible To Boolean 3");
        try
        {
            sbyte sbyteVal = (sbyte)(this.GetInt32(0, 128) * (-1));
            IConvertible iConvert = (IConvertible)(sbyteVal);
            bool boolA = iConvert.ToBoolean(provider);
            if (sbyteVal == 0)
            {
                if (boolA)
                {
                    TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
            }
            else
            {
                if (!boolA)
                {
                    TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
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
