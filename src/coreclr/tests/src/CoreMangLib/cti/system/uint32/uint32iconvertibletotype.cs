// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;


public class UInt32IConvertibleToType
{
    public static int Main()
    {
        UInt32IConvertibleToType ui32icttype = new UInt32IConvertibleToType();
        TestLibrary.TestFramework.BeginTestCase("UInt32IConvertibleToType");
        if (ui32icttype.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:UInt32 MinValue to Type of string");

        try
        {
            UInt32 uintA = UInt32.MinValue;
            IConvertible iConvert = (IConvertible)(uintA);
            string strA = (string)iConvert.ToType(typeof(string), null);
            if (strA != uintA.ToString())
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:UInt32 MinValue to Type of bool");

        try
        {
            UInt32 uintA = UInt32.MinValue;
            IConvertible iConvert = (IConvertible)(uintA);
            bool boolA = (bool)iConvert.ToType(typeof(bool), null);
            if (boolA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:UInt32 MaxValue to Type of bool");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            IConvertible iConvert = (IConvertible)(uintA);
            bool boolA = (bool)iConvert.ToType(typeof(bool), null);
            if (!boolA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:UInt32 MaxValue to Type of Int64,double and decimal");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            IConvertible iConvert = (IConvertible)(uintA);
            Int64 int64A = (Int64)iConvert.ToType(typeof(Int64), null);
            Double doubleA = (Double)iConvert.ToType(typeof(Double), null);
            Decimal decimalA = (Decimal)iConvert.ToType(typeof(Decimal), null);
            Single singleA = (Single)iConvert.ToType(typeof(Single), null);
            if (int64A != uintA || doubleA!= uintA || decimalA != uintA || singleA != uintA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest5:UInt32 between MinValue to Int32.Max to Type of Int32");

        try
        {
            UInt32 uintA = (UInt32)this.GetInt32(0, Int32.MaxValue);
            IConvertible iConvert = (IConvertible)(uintA);
            Int32 int32A = (Int32)iConvert.ToType(typeof(Int32), null);
            if (int32A != uintA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest6:UInt32 between MinValue to Int16.Max to Type of Int16");

        try
        {
            UInt32 uintA = (UInt32)this.GetInt32(0, Int16.MaxValue + 1);
            IConvertible iConvert = (IConvertible)(uintA);
            Int16 int16A = (Int16)iConvert.ToType(typeof(Int16), null);
            if (int16A != uintA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest7:UInt32 between MinValue and SByte.Max to Type of SByte");

        try
        {
            UInt32 uintA = (UInt32)this.GetInt32(0, SByte.MaxValue + 1);
            IConvertible iConvert = (IConvertible)(uintA);
            SByte sbyteA = (SByte)iConvert.ToType(typeof(SByte), null);
            if (sbyteA != uintA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest8:UInt32 between MinValue and Byte.Max to Type of Byte");

        try
        {
            UInt32 uintA = (UInt32)this.GetInt32(0, Byte.MaxValue + 1);
            IConvertible iConvert = (IConvertible)(uintA);
            Byte byteA = (Byte)iConvert.ToType(typeof(Byte), null);
            if (byteA != uintA)
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:UInt32 between Int32.MaxValue and UInt32.MaxValue to Type of Int32");

        try
        {
            UInt32 uintA = (UInt32)(this.GetInt32(0, Int32.MaxValue) + Int32.MaxValue);
            IConvertible iConvert = (IConvertible)(uintA);
            Int32 int32A = (Int32)iConvert.ToType(typeof(Int32), null);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:UInt32 between Int16.Max and UInt32.MaxValue Type of Int16");

        try
        {
            UInt32 uintA = (UInt32)(Int16.MaxValue + this.GetInt32(1, Int32.MaxValue));
            IConvertible iConvert = (IConvertible)(uintA);
            Int16 int16A = (Int16)iConvert.ToType(typeof(Int16), null);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:UInt32 between SByte.Max and UInt32.MaxValue to Type of SByte");

        try
        {
            UInt32 uintA = (UInt32)(SByte.MaxValue + this.GetInt32(1, Int32.MaxValue));
            IConvertible iConvert = (IConvertible)(uintA);
            SByte sbyteA = (SByte)iConvert.ToType(typeof(SByte), null);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4:UInt32 between Byte.Max and UInt32.MaxValue to Type of Byte");

        try
        {
            UInt32 uintA = (UInt32)(Byte.MaxValue + this.GetInt32(1,Int32.MaxValue));
            IConvertible iConvert = (IConvertible)(uintA);
            Byte byteA = (Byte)iConvert.ToType(typeof(Byte), null);
            retVal = false;
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
    #region ForTestObject
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
