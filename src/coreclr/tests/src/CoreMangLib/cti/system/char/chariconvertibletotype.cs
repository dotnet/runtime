// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Char.System.IConvertible.ToType(Type, IFormatProvider)
/// Converts the value of the current Char object to an object of the specified type using 
/// the specified IFormatProvider object. 
/// </summary>
public class CharIConvertibleToType
{
    public static int Main()
    {
        CharIConvertibleToType testObj = new CharIConvertibleToType();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.System.IConvertible.ToType(Type, IFormatProvider)");
        if(testObj.RunTests())
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
        retVal = PosTest10() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;
	retVal = NegTest9() && retVal;
	retVal = NegTest10() && retVal;
        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Conversion to byte";
        string errorDesc;

        byte b;
        object expectedObj;
        object actualObj;
        char ch;
        b = TestLibrary.Generator.GetByte(-55);
        ch = (char)b;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = b;
            actualObj = converter.ToType(typeof(byte), numberFormat);
            
            if (((byte)expectedObj != (byte)actualObj) || !(actualObj is byte))
            {
                errorDesc = string.Format("Byte value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: Conversion to sbyte";
        string errorDesc;

        sbyte sb;
        object expectedObj;
        object actualObj;
        char ch;
        sb = (sbyte)(TestLibrary.Generator.GetByte(-55) % (sbyte.MaxValue + 1));
        ch = (char)sb;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = sb;
            actualObj = converter.ToType(typeof(sbyte), numberFormat);

            if (((sbyte)expectedObj != (sbyte)actualObj) || !(actualObj is sbyte))
            {
                errorDesc = string.Format("SByte value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: Conversion to Int16";
        string errorDesc;

        Int16 i;
        object expectedObj;
        object actualObj;
        char ch;
        i = (Int16)(TestLibrary.Generator.GetInt32(-55) % (Int16.MaxValue + 1));
        ch = (char)i;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = i;
            actualObj = converter.ToType(typeof(Int16), numberFormat);

            if (((Int16)expectedObj != (Int16)actualObj) || !(actualObj is Int16))
            {
                errorDesc = string.Format("Int16 value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        const string c_TEST_DESC = "PosTest4: Conversion to UInt16";
        string errorDesc;

        UInt16 i;
        object expectedObj;
        object actualObj;
        char ch;
        i = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        ch = (char)i;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = i;
            actualObj = converter.ToType(typeof(UInt16), numberFormat);

            if (((UInt16)expectedObj != (UInt16)actualObj) || !(actualObj is UInt16))
            {
                errorDesc = string.Format("UInt16 value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "P005";
        const string c_TEST_DESC = "PosTest5: Conversion to Int32";
        string errorDesc;

        int i;
        object expectedObj;
        object actualObj;
        char ch;
        i = TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1);
        ch = (char)i;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = i;
            actualObj = converter.ToType(typeof(int), numberFormat);

            if (((int)expectedObj != (int)actualObj) || !(actualObj is int))
            {
                errorDesc = string.Format("Int32 value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        const string c_TEST_ID = "P006";
        const string c_TEST_DESC = "PosTest6: Conversion to UInt32";
        string errorDesc;

        UInt32 i;
        object expectedObj;
        object actualObj;
        char ch;
        i = (UInt32)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        ch = (char)i;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = i;
            actualObj = converter.ToType(typeof(UInt32), numberFormat);

            if (((UInt32)expectedObj != (UInt32)actualObj) || !(actualObj is UInt32))
            {
                errorDesc = string.Format("UInt32 value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        const string c_TEST_ID = "P007";
        const string c_TEST_DESC = "PosTest7: Conversion to Int64";
        string errorDesc;

        Int64 i;
        object expectedObj;
        object actualObj;
        char ch;
        i = TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1);
        ch = (char)i;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = i;
            actualObj = converter.ToType(typeof(Int64), numberFormat);

            if (((Int64)expectedObj != (Int64)actualObj) || !(actualObj is Int64))
            {
                errorDesc = string.Format("Int64 value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        const string c_TEST_ID = "P008";
        const string c_TEST_DESC = "PosTest8: Conversion to UInt64";
        string errorDesc;

        UInt64 i;
        object expectedObj;
        object actualObj;
        char ch;
        i = (UInt64)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        ch = (char)i;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedObj = i;
            actualObj = converter.ToType(typeof(UInt64), numberFormat);

            if (((UInt64)expectedObj != (UInt64)actualObj) || !(actualObj is UInt64))
            {
                errorDesc = string.Format("UInt64 value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        const string c_TEST_ID = "P009";
        const string c_TEST_DESC = "PosTest9: Conversion to char";
        string errorDesc;

        object expectedObj;
        object actualObj;
        char ch;
        ch = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = ch;

            expectedObj = ch;
            actualObj = converter.ToType(typeof(char), null);

            if (((char)expectedObj != (char)actualObj) || !(actualObj is char))
            {
                errorDesc = string.Format("char value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("018" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        const string c_TEST_ID = "P010";
        const string c_TEST_DESC = "PosTest10: Conversion to string";
        string errorDesc;

        object expectedObj;
        object actualObj;
        char ch;
        ch = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = ch;

            expectedObj = new string(ch, 1);
            actualObj = converter.ToType(typeof(string), null);

            if (((string)expectedObj != (string)actualObj) || !(actualObj is string))
            {
                errorDesc = string.Format("string value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedObj + " as expected: Actual(" + actualObj + ")";
                TestLibrary.TestFramework.LogError("019" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("020" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative tests
    //ArgumentNullException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: type is a null reference (Nothing in Visual Basic).";
        string errorDesc;

        char ch = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = ch;
            converter.ToType(null, null);

            errorDesc = "ArgumentNullException is not thrown as expected.";
            errorDesc +=  string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError("021" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError("022" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    //InvalidCastException
    public bool NegTest2()
    {
        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: type is boolean";

        return this.DoInvalidCastTest(c_TEST_ID, c_TEST_DESC, "023", "024", typeof(bool));
    }

    public bool NegTest3()
    {
        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: type is double";

        return this.DoInvalidCastTest(c_TEST_ID, c_TEST_DESC, "025", "026", typeof(double));
    }

    public bool NegTest4()
    {
        const string c_TEST_ID = "N004";
        const string c_TEST_DESC = "NegTest4: type is single";

        return this.DoInvalidCastTest(c_TEST_ID, c_TEST_DESC, "027", "028", typeof(Single));
    }

    public bool NegTest5()
    {
        const string c_TEST_ID = "N005";
        const string c_TEST_DESC = "NegTest5: type is DateTime";

        return this.DoInvalidCastTest(c_TEST_ID, c_TEST_DESC, "029", "030", typeof(DateTime));
    }

    public bool NegTest6()
    {
        const string c_TEST_ID = "N006";
        const string c_TEST_DESC = "NegTest6: type is Decimal";

        return this.DoInvalidCastTest(c_TEST_ID, c_TEST_DESC, "031", "032", typeof(Decimal));
    }

    public bool NegTest7()
    {
        const string c_TEST_ID = "N007";
        const string c_TEST_DESC = "NegTest7: type is Decimal";

        return this.DoInvalidCastTest(c_TEST_ID, c_TEST_DESC, "033", "034", typeof(Decimal));
    }

    //bug
    //OverflowException
    public bool NegTest8()
    {
        const string c_TEST_ID = "N008";
        const string c_TEST_DESC = "NegTest8: Value is too large for destination type";
        return this.DoOverflowTest(c_TEST_ID, c_TEST_DESC, "035", typeof(Int16), Int16.MaxValue);
    }

    public bool NegTest9()
    {
        const string c_TEST_ID = "N009";
        const string c_TEST_DESC = "NegTest9: Value is too large for destination type";

        return this.DoOverflowTest(c_TEST_ID, c_TEST_DESC, "036", typeof(byte), byte.MaxValue);
    }

    public bool NegTest10()
    {
        const string c_TEST_ID = "N010";
        const string c_TEST_DESC = "NegTest10: Value is too large for destination type";

        return this.DoOverflowTest(c_TEST_ID, c_TEST_DESC, "037", typeof(sbyte), sbyte.MaxValue);
    }
    #endregion

    #region Helper methods for negative tests
    private bool DoInvalidCastTest(string testId,
                                                string testDesc,
                                                string errorNum1,
                                                string errorNum2,
                                                Type destType)
    {
        bool retVal = true;
        string errorDesc;

        char ch = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            IConvertible converter = ch;
            converter.ToType(destType, null);

            errorDesc = "InvalidCastException is not thrown as expected.";
            errorDesc += string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError(errorNum1 + " TestId-" + testId, errorDesc);
            retVal = false;
        }
        catch (InvalidCastException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError(errorNum2 + " TestId-" + testId, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    private bool DoOverflowTest(string testId,
                                            string testDesc,
                                            string errorNum,
                                            Type destType,
                                            int destTypeMaxValue)
    {
        bool retVal = true;
        string errorDesc;

        int i;
        char ch;
        i = destTypeMaxValue + 1 +
             TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue - destTypeMaxValue);
        ch = (char)i;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            IConvertible converter = ch;
            converter.ToType(destType, null);
	    TestLibrary.TestFramework.LogError(errorNum + "TestId-" +testId,
		"No OverflowException thrown when expected");
	    retVal = false;
        }
	catch (OverflowException)
	{
	    TestLibrary.TestFramework.LogInformation("Caught expected overflow.");
	}
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError(errorNum + " TestId-" + testId, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

