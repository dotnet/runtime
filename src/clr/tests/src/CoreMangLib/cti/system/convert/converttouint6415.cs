// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt64(Object,IFormatProvider)
/// </summary>
public class ConvertToUInt6415
{
    public static int Main()
    {
        ConvertToUInt6415 convertToUInt6415 = new ConvertToUInt6415();
        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt6415");
        if (convertToUInt6415.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Convert to UInt64 from object 1");
        try
        {
            TestClass2 objVal = new TestClass2();
            TestIFormat iformat = new TestIFormat();
            ulong ulongVal1 = Convert.ToUInt64(objVal, iformat);
            ulong ulongVal2 = Convert.ToUInt64(objVal, null);
            if (ulongVal1 != UInt64.MaxValue || ulongVal2 != UInt64.MinValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Convert to UInt64 from object 2");
        try
        {
            object objVal = true;
            TestIFormat iformat = new TestIFormat();
            ulong ulongVal1 = Convert.ToUInt64(objVal, iformat);
            ulong ulongVal2 = Convert.ToUInt64(objVal, null);
            if (ulongVal1 != 1 || ulongVal2 != 1)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Convert to UInt64 from object 3");
        try
        {
            object objVal = false;
            TestIFormat iformat = new TestIFormat();
            ulong ulongVal1 = Convert.ToUInt64(objVal, iformat);
            ulong ulongVal2 = Convert.ToUInt64(objVal, null);
            if (ulongVal1 != 0 || ulongVal2 != 0)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Convert to UInt64 from object 4");
        try
        {
            object objVal = null;
            TestIFormat iformat = new TestIFormat();
            ulong ulongVal1 = Convert.ToUInt32(objVal, iformat);
            ulong ulongVal2 = Convert.ToUInt32(objVal, null);
            if (ulongVal1 != 0 || ulongVal2 != 0)
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
            ulong ulongVal = Convert.ToUInt64(objVal, null);
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
        public bool IsUInt64MaxValue = true;
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
            throw new Exception("The method or operation is not implemented.");
        }
        public uint ToUInt32(IFormatProvider provider)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public ulong ToUInt64(IFormatProvider provider)
        {
            bool IsUInt64MinValue = true;
            if (provider != null)
            {
                TestIFormat iformat = (TestIFormat)provider.GetFormat(typeof(TestIFormat));
                if (iformat != null && iformat.IsUInt64MaxValue)
                {
                    IsUInt64MinValue = false;
                }
            }
            if (IsUInt64MinValue)
            {
                return UInt64.MinValue;
            }
            else
            {
                return UInt64.MaxValue;
            }
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
