// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestClass
{
}

public class TestFormatProvider : IFormatProvider
{
    public bool ToInt16MaxValue = true;

    #region IFormatProvider Members

    public object GetFormat(Type formatType)
    {
        if (formatType.Equals(typeof(TestFormatProvider)))
        {
            return this;
        }
        else
        {
            return null;
        }
    }

    #endregion
}

public class TestConvertableClass : IConvertible
{
    #region IConvertible Members

    public TypeCode GetTypeCode()
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public bool ToBoolean(IFormatProvider provider)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public int ToInt32(IFormatProvider provider)
    {
        bool toMinValue = true;

        if (provider != null)
        {
            TestFormatProvider format = provider.GetFormat(typeof(TestFormatProvider)) as TestFormatProvider;
            if ((format != null) && format.ToInt16MaxValue)
            {
                toMinValue = false;
            }
        }

        if (toMinValue)
        {
            return Int32.MinValue;
        }
        else
        {
            return Int32.MaxValue;
        }
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

    public byte ToByte(IFormatProvider provider)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public short ToInt16(IFormatProvider provider)
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
        throw new Exception("The method or operation is not implemented.");
    }

    #endregion
}

/// <summary>
/// ToInt32(System.Object,System.IFormatProvider)
/// </summary>

public class ConvertToInt32_10
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Convert.ToInt32 when value implements IConvertible");

        try
        {
            retVal = VerificationHelper(true, null, 1, "001.1") && retVal;
            retVal = VerificationHelper(1, null, 1, "001.2") && retVal;
            retVal = VerificationHelper(0, null, 0, "001.3") && retVal;
            retVal = VerificationHelper(new TestConvertableClass(), null, Int32.MinValue, "001.4") && retVal;

            retVal = VerificationHelper(true, new TestFormatProvider(), 1, "001.5") && retVal;
            retVal = VerificationHelper(1, new TestFormatProvider(), 1, "001.6") && retVal;
            retVal = VerificationHelper(0, new TestFormatProvider(), 0, "001.7") && retVal;
            retVal = VerificationHelper(new TestConvertableClass(), new TestFormatProvider(), Int32.MaxValue, "001.8") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Convert.ToInt32 when value is null");

        try
        {
            retVal = VerificationHelper(null, null, 0, "002.1") && retVal;
            retVal = VerificationHelper(null, new TestFormatProvider(), 0, "002.2") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidCastException should be thrown when value does not implement IConvertible. ");

        try
        {
            int actual = Convert.ToInt32(new TestClass());

            TestLibrary.TestFramework.LogError("101.1", "InvalidCastException is not thrown when value does not implement IConvertible. ");
            retVal = false;
        }
        catch (InvalidCastException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToInt32_10 test = new ConvertToInt32_10();

        TestLibrary.TestFramework.BeginTestCase("ConvertToInt32_10");

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

    #region Private Methods
    private bool VerificationHelper(object obj, IFormatProvider provider, int desired, string errorno)
    {
        bool retVal = true;

        int actual = Convert.ToInt32(obj, provider);
        if (actual != desired)
        {
            TestLibrary.TestFramework.LogError(errorno, "Convert.ToInt32 returns unexpected values");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", desired = " + desired + ", obj = " + obj);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
