// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt16(object,IFormatProvider)
/// </summary>
public class ConvertToUInt1614
{
    public static int Main()
    {
        ConvertToUInt1614 convertToUInt1614 = new ConvertToUInt1614();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt1614");
        if (convertToUInt1614.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Convert to UInt16 from object 1");
        try
        {
            TestClass2 objVal = new TestClass2();
            TestIFormat iformat = new TestIFormat();
            ushort unshortVal1 = Convert.ToUInt16(objVal,iformat);
            ushort unshortVal2 = Convert.ToUInt16(objVal, null);
            if (unshortVal1 != UInt16.MaxValue || unshortVal2 != UInt16.MinValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Convert to UInt16 from object 2");
        try
        {
            object objVal = true;
            TestIFormat iformat = new TestIFormat();
            ushort unshortVal1 = Convert.ToUInt16(objVal, iformat);
            ushort unshortVal2 = Convert.ToUInt16(objVal, null);
            if (unshortVal1 != 1 || unshortVal2 != 1)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Convert to UInt16 from object 3");
        try
        {
            object objVal = false;
            TestIFormat iformat = new TestIFormat();
            ushort unshortVal1 = Convert.ToUInt16(objVal, iformat);
            ushort unshortVal2 = Convert.ToUInt16(objVal, null);
            if (unshortVal1 != 0 || unshortVal2 != 0)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Convert to UInt16 from object 4");
        try
        {
            object objVal = null;
            TestIFormat iformat = new TestIFormat();
            ushort unshortVal1 = Convert.ToUInt16(objVal, iformat);
            ushort unshortVal2 = Convert.ToUInt16(objVal, null);
            if (unshortVal1 != 0 || unshortVal2 != 0)
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The object does not implement IConvertible");
        try
        {
            TestClass1 objVal = new TestClass1();
            ushort unshortVal = Convert.ToUInt16(objVal,null);
            TestLibrary.TestFramework.LogError("N001", "The object does not implement IConvertible but not throw exception");
            retVal = false;
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestClass
    public class TestClass1 { }
    public class TestIFormat : IFormatProvider
    {
        public bool IsUInt16MaxValue = true;
        public object GetFormat(Type argType)
        {
            if (argType == typeof(TestIFormat))
                return this;
            else
                return null;
        }
    }
    public class TestClass2 : IConvertible
    {
        public TypeCode GetTypeCode()
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public bool ToBoolean(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public byte ToByte(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public char ToChar(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public double ToDouble(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public short ToInt16(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public int ToInt32(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public long ToInt64(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public float ToSingle(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public string ToString(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public ushort ToUInt16(IFormatProvider provider)
        {
            bool IsUInt16MinValue = true;
            if (provider != null)
            {
                TestIFormat iformat = (TestIFormat)provider.GetFormat(typeof(TestIFormat));
                if (iformat != null && iformat.IsUInt16MaxValue)
                {
                    IsUInt16MinValue = false;
                }
            }
            if (IsUInt16MinValue)
            {
                return UInt16.MinValue;
            }
            else
            {
                return UInt16.MaxValue;
            }
        }
        public uint ToUInt32(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
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
