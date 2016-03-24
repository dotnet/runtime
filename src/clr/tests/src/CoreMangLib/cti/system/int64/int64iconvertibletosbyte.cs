// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// Int64.System.IConvertible.ToSByte(IFormatProvider)
/// </summary>
public class Int64IConvertibleToSByte
{
    public static int Main()
    {
        Int64IConvertibleToSByte ui64IContSByte = new Int64IConvertibleToSByte();
        TestLibrary.TestFramework.BeginTestCase("Int64IConvertibleToSByte");
        if (ui64IContSByte.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[NegTest]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The Int64 value which is in the range of SByte IConvertible To SByte 1");
        try
        {
            long int64A = (long)SByte.MaxValue;
            IConvertible iConvert = (IConvertible)(int64A);
            sbyte sbyteA = iConvert.ToSByte(provider);
            if (sbyteA != 127)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:The Int64 value which is in the range of SByte IConvertible To SByte 2");
        try
        {
            long int64A = (long)SByte.MinValue;
            IConvertible iConvert = (IConvertible)(int64A);
            sbyte sbyteA = iConvert.ToSByte(null);
            if (sbyteA != (SByte)int64A)
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
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest3:The Int64 value which is in the range of SByte IConvertible To SByte 3");
        try
        {
            long int64A = this.GetInt32(0, 127);
            IConvertible iConvert = (IConvertible)(int64A);
            sbyte sbyteA = iConvert.ToSByte(provider);
            if (sbyteA != (SByte)int64A)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:The Int64 value which is in the range of SByte IConvertible To SByte 4");
        try
        {
            long int64A = this.GetInt32(1, 129) * (-1);
            IConvertible iConvert = (IConvertible)(int64A);
            sbyte sbyteA = iConvert.ToSByte(null);
            if (sbyteA != (SByte)int64A)
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The Int64 value which is out the range of SByte IConvertible To SByte 1");
        try
        {
            long int64A = Int64.MaxValue;
            IConvertible iConvert = (IConvertible)(int64A);
            sbyte sbyteA = iConvert.ToSByte(provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N001", "Int64 value out of the range of SByte but not throw exception");
        }
        catch (OverflowException) { }
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
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("NegTest2:The Int64 value which is out the range of SByte IConvertible To SByte 2");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            if (int64A <= 127)
            {
                int64A = int64A + 127;
            }
            IConvertible iConvert = (IConvertible)(int64A);
            sbyte sbyteA = iConvert.ToSByte(provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N004", "Int64 value out of the range of Byte but not throw exception");
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N005", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("NegTest3:The Int64 value which is out the range of Byte IConvertible To Byte 3");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            if (int64A <=128)
            {
                int64A = (int64A + 129) * (-1);
            }
            else
            {
                int64A = int64A * (-1);
            }
            IConvertible iConvert = (IConvertible)(int64A);
            sbyte sbyteA = iConvert.ToSByte(provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N003", "Int64 value out of the range of Byte but not throw exception");
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpected exception: " + e);
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
