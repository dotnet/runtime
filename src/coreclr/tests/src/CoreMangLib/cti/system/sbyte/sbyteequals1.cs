// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// SByte.Equals(object)
/// </summary>
public class SByteEquals1
{
    public static int Main()
    {
        SByteEquals1 sbyteEquals1 = new SByteEquals1();
        TestLibrary.TestFramework.BeginTestCase("SByteEquals1");
        if (sbyteEquals1.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: The object compare to the SByte maxValue");
        try
        {
            object objVal = SByte.MaxValue;
            sbyte sourceSByte = sbyte.MaxValue; 
            bool retbool = sourceSByte.Equals(objVal);
            if (!retbool)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: The object compare to the SByte minValue");
        try
        {
            object objVal = SByte.MinValue;
            sbyte sourceSByte = sbyte.MinValue;
            bool retbool = sourceSByte.Equals(objVal);
            if (!retbool)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: The object compare to the random sbyte");
        try
        {
            sbyte sbyteVal = (sbyte)(this.GetInt32(0, 128) + this.GetInt32(0, 129) * (-1));
            object objVal = sbyteVal;
            sbyte sourceSByte = sbyteVal;
            bool retbool = sourceSByte.Equals(objVal);
            if (!retbool)
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: The sbyte maxValue equals sbyte minValue");
        try
        {
            object objVal = sbyte.MinValue;
            sbyte sourceSByte = sbyte.MaxValue;
            bool retbool = sourceSByte.Equals(objVal);
            if (retbool)
            {
                TestLibrary.TestFramework.LogError("007", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: The random sbyte equals random sbyte");
        try
        {
            sbyte sbyteVal = (sbyte)(this.GetInt32(0, 127) + this.GetInt32(0, 128) * (-1));
            object objVal = sbyteVal;
            sbyte sourceSByte = (sbyte)(sbyteVal + 1);
            bool retbool = sourceSByte.Equals(objVal);
            if (retbool)
            {
                TestLibrary.TestFramework.LogError("009", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: The object is not sbyte");
        try
        {
            object objVal = "helloworld";
            sbyte sourceSByte = (sbyte)(this.GetInt32(0, 128) + this.GetInt32(0, 129) * (-1));
            bool retbool = sourceSByte.Equals(objVal);
            if (retbool)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
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
