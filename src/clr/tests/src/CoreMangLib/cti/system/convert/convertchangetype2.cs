// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertChangeType1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ConvertChangeType1 ac = new ConvertChangeType1();

        TestLibrary.TestFramework.BeginTestCase("Convert.ChangeType(Object, Type, IFormatProvider)");

        if (ac.RunTests())
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
        retVal = PosTest3()  && retVal;
        retVal = PosTest4()  && retVal;
        retVal = PosTest5()  && retVal;
        retVal = PosTest6()  && retVal;
        retVal = PosTest7()  && retVal;
        retVal = PosTest9()  && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;
        retVal = PosTest17() && retVal;
        retVal = PosTest18() && retVal;
        retVal = PosTest19() && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    public bool PosTest1() { return PosTest(1, true, typeof(Boolean)); }
    public bool PosTest2() { return PosTest(2, false, typeof(Boolean)); }
    public bool PosTest3() { return PosTest(3, TestLibrary.Generator.GetByte(-55), typeof(Byte)); }
    public bool PosTest4() { return PosTest(4, TestLibrary.Generator.GetChar(-55), typeof(Char)); }
    public bool PosTest5() { return PosTest(5, DateTime.Now, typeof(DateTime)); } 
    public bool PosTest6() { return PosTest(6, new Decimal(TestLibrary.Generator.GetDouble(-55)), typeof(Decimal)); }
    public bool PosTest7() { return PosTest(7, TestLibrary.Generator.GetDouble(-55), typeof(Double)); }
    public bool PosTest9() { return PosTest(9, TestLibrary.Generator.GetInt16(-55), typeof(Int16)); }
    public bool PosTest10() { return PosTest(10, TestLibrary.Generator.GetInt32(-55), typeof(Int32)); }
    public bool PosTest11() { return PosTest(11, TestLibrary.Generator.GetInt64(-55), typeof(Int64)); }
    public bool PosTest12() { return PosTest(12, new MyConvertible(), typeof(Object)); }
    public bool PosTest13() { return PosTest(13, (SByte)TestLibrary.Generator.GetByte(-55), typeof(SByte)); }
    public bool PosTest14() { return PosTest(14, TestLibrary.Generator.GetSingle(-55), typeof(Single)); }
    public bool PosTest15() { return PosTest(15, TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN), typeof(String)); }
    public bool PosTest16() { return PosTest(16, (UInt16)TestLibrary.Generator.GetInt16(-55), typeof(UInt16)); }
    public bool PosTest17() { return PosTest(17, (UInt32)TestLibrary.Generator.GetInt32(-55), typeof(UInt32)); }
    public bool PosTest18() { return PosTest(18, (UInt64)TestLibrary.Generator.GetInt64(-55), typeof(UInt64)); }
    public bool PosTest19() { return PosTest(19, new object(), typeof(object)); }

    public bool NegTest1() { return NegTest(1, null, typeof(String)); }
    public bool NegTest2() { return NegTest(2, null, typeof(Object)); }
    public bool NegTest3() { return NegTest2(3, null, typeof(MyStruct)); }
    public bool NegTest4() { return NegTest2(4, this, typeof(object)); }
    public bool NegTest5() { return NegTest3(5, new object(), null); }

    public bool PosTest(int id, object curObject, Type type)
    {
        bool  retVal = true;
        Object newObject;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ChangeType(Object, Type, IFormatProvider) (type:"+GetType(curObject)+" "+GetType(type)+")");

        try
        {
            newObject = Convert.ChangeType(curObject, type, null);

            // check type
            switch(type.ToString())
            {
            case "System.Boolean":
                retVal = (typeof(System.Boolean) == GetType(newObject)) && retVal;
                break;
            case "System.Byte":
                retVal = (typeof(System.Byte) == GetType(newObject)) && retVal;
                break;
            case "System.Char":
                retVal = (typeof(System.Char) == GetType(newObject)) && retVal;
                break;
            case "System.DateTime":
                retVal = (typeof(System.DateTime) == GetType(newObject)) && retVal;
                break;
            case "System.Decimal":
                retVal = (typeof(System.Decimal) == GetType(newObject)) && retVal;
                break;
            case "System.Double":
                retVal = (typeof(System.Double) == GetType(newObject)) && retVal;
                break;
            case "System.Int16":
                retVal = (typeof(System.Int16) == GetType(newObject)) && retVal;
                break;
            case "System.Int32":
                retVal = (typeof(System.Int32) == GetType(newObject)) && retVal;
                break;
            case "System.Int64":
                retVal = (typeof(System.Int64) == GetType(newObject)) && retVal;
                break;
            case "System.Object":
                break;
            case "System.SByte":
                retVal = (typeof(System.SByte) == GetType(newObject)) && retVal;
                break;
            case "System.Single":
                retVal = (typeof(System.Single) == GetType(newObject)) && retVal;
                break;
            case "System.String":
                retVal = (typeof(System.String) == GetType(newObject)) && retVal;
                break;
            case "System.UInt16":
                retVal = (typeof(System.UInt16) == GetType(newObject)) && retVal;
                break;
            case "System.UInt32":
                retVal = (typeof(System.UInt32) == GetType(newObject)) && retVal;
                break;
            case "System.UInt64":
                retVal = (typeof(System.UInt64) == GetType(newObject)) && retVal;
                break;
            default:
                retVal = false;
                break;
            }

            if (!retVal)
            {
                TestLibrary.TestFramework.LogError("000", "Type mismatch: Expected(" + type.ToString() + ") Actual(" + GetType(newObject) + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest(int id, object curObject, Type type)
    {
        bool            retVal = true;
        IFormatProvider myfp;
        object          newType;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ChangeType(Object, Type, IFormatProvider) (type:"+GetType(curObject)+" "+GetType(type)+")");

        try
        {
            myfp    = null;
            newType = Convert.ChangeType(curObject, type, myfp);

            if (null != newType)
            {
                TestLibrary.TestFramework.LogError("002", "Unexpected value: Expected(null) Actual(" + newType + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2(int id, object curObject, Type type)
    {
        bool            retVal = true;
        IFormatProvider myfp;
        object          newType;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ChangeType(Object, Type, IFormatProvider) (type:"+GetType(curObject)+" "+GetType(type)+")");

        try
        {
            myfp    = null;
            newType = Convert.ChangeType(curObject, type, myfp);

            TestLibrary.TestFramework.LogError("004", "Exception expected");
            retVal = false;
        }
        catch (InvalidCastException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3(int id, object curObject, Type type)
    {
        bool            retVal = true;
        IFormatProvider myfp;
        object          newType;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ChangeType(Object, Type, IFormatProvider) (type:"+GetType(curObject)+" "+GetType(type)+")");

        try
        {
            myfp    = null;
            newType = Convert.ChangeType(curObject, type, myfp);

            TestLibrary.TestFramework.LogError("006", "Exception expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public Type GetType(object o)
    {
        if (null != o) return o.GetType();

        return null;
    }

    public struct MyStruct {}

    public class MyConvertible : IConvertible
    {
        public TypeCode GetTypeCode() { return TypeCode.Empty; }
        public Boolean ToBoolean(System.IFormatProvider f) { return true; }
        public Char ToChar(System.IFormatProvider f) { return ' '; }
        public SByte ToSByte(System.IFormatProvider f) { return 1; }
        public Byte ToByte(System.IFormatProvider f) { return 1; }
        public Int16 ToInt16(System.IFormatProvider f) { return 1; }
        public UInt16 ToUInt16(System.IFormatProvider f) { return 1; }
        public Int32 ToInt32(System.IFormatProvider f) { return 1; }
        public UInt32 ToUInt32(System.IFormatProvider f) { return 1; }
        public Int64 ToInt64(System.IFormatProvider f) { return 1; }
        public UInt64 ToUInt64(System.IFormatProvider f) { return 1; }
        public Single ToSingle(System.IFormatProvider f) { return 1; }
        public Double ToDouble(System.IFormatProvider f) { return 1; }
        public Decimal ToDecimal(System.IFormatProvider f) { return 1; }
        public DateTime ToDateTime(System.IFormatProvider f) { return DateTime.Now; }
        public String ToString(System.IFormatProvider f) { return ""; }
        public object ToType(System.Type t, System.IFormatProvider f) { return new object(); }

    }
}

