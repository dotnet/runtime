using System;
using System.Globalization;

/// <summary>
/// System.Convert.ToDecimal(Object,IFormatProvider)
/// </summary>
public class ConvertToDecimal15
{
    #region Public Methods
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The object is a byte");

        try
        {
            byte i = TestLibrary.Generator.GetByte(-55);
            object ob = i;
            IFormatProvider iFormatProvider = new CultureInfo("en-US");
            decimal decimalValue = Convert.ToDecimal(ob, iFormatProvider);
            if (decimalValue != i)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,i is" + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The object is an int32");

        try
        {
            Int32 i = TestLibrary.Generator.GetInt32(-55);
            object ob = i;
            IFormatProvider iFormatProvider = new CultureInfo("fr-FR");
            decimal decimalValue = Convert.ToDecimal(ob, iFormatProvider);
            if (decimalValue != i)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,i is" + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: The object is a double and the Iformatprovider is a null reference");

        try
        {
            double i = TestLibrary.Generator.GetDouble(-55);
            object ob = i;
            IFormatProvider iFormatProvider = null;
            decimal decimalValue = Convert.ToDecimal(ob, iFormatProvider);
            if (decimalValue != (i as IConvertible).ToDecimal(null))
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,i is" + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: The object is a string and the cultureinfo is fr-FR");

        try
        {
            string i = "-100 123 456";
            object ob = i;
            IFormatProvider iFormatProvider = new CultureInfo("fr-FR");
            decimal decimalValue = Convert.ToDecimal(ob, iFormatProvider);
            if (decimalValue != -100123456)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,i is" + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: The object is a string and the cultureinfo is en-US");

        try
        {
            string i = "-100,123,456.12345";
            object ob = i;
            IFormatProvider iFormatProvider = new CultureInfo("en-US");
            decimal decimalValue = Convert.ToDecimal(ob, iFormatProvider);
            if (decimalValue != -100123456.12345M)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected,i is" + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: The object is a custom class which implement the iconvertible interface");

        try
        {
            MyClass myClass = new MyClass();
            object ob = myClass;
            IFormatProvider iFormatProvider = new CultureInfo("en-US");
            decimal decimalValue = Convert.ToDecimal(ob, iFormatProvider);
            if (decimalValue != -1)
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The conversion is invalid");

        try
        {
            char i = '1';
            object ob = i;
            IFormatProvider iFormatProvider = new CultureInfo("en-US");
            decimal decimalValue = Convert.ToDecimal(ob, iFormatProvider);
            TestLibrary.TestFramework.LogError("101", "The InvalidCastException was not thrown as expected");
            retVal = false;
        }
        catch (InvalidCastException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToDecimal15 test = new ConvertToDecimal15();

        TestLibrary.TestFramework.BeginTestCase("ConvertToDecimal15");

        if (test.RunTests())
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
}
public class MyClass : IConvertible
{
    #region IConvertible Members

    public TypeCode GetTypeCode()
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public bool ToBoolean(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public byte ToByte(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public char ToChar(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public DateTime ToDateTime(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public decimal ToDecimal(IFormatProvider provider)
    {
        if (((CultureInfo)provider).Equals(new CultureInfo("en-US")))
        {
            return -1;
        }
        if (((CultureInfo)provider).Equals(new CultureInfo("fr-FR")))
        {
            return 1;
        }
        return 0;
    }

    public double ToDouble(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public short ToInt16(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public int ToInt32(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public long ToInt64(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public sbyte ToSByte(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public float ToSingle(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public string ToString(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public object ToType(Type conversionType, IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public ushort ToUInt16(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public uint ToUInt32(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public ulong ToUInt64(IFormatProvider provider)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    #endregion
}
