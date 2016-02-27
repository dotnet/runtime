// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayCreateInstance1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayCreateInstance1 ac = new ArrayCreateInstance1();

        TestLibrary.TestFramework.BeginTestCase("Array.CreateInstance(Type, int)");

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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    public bool PosTest1() { return PosCreateInstance<Int64>(1, TestLibrary.Generator.GetInt64(-55)); }
    public bool PosTest2() { return PosCreateInstance<Int32>(2, TestLibrary.Generator.GetInt32(-55)); }
    public bool PosTest3() { return PosCreateInstance<Int16>(3, TestLibrary.Generator.GetInt16(-55)); }
    public bool PosTest4() { return PosCreateInstance<Byte>(4, TestLibrary.Generator.GetByte(-55)); }
    public bool PosTest5() { return PosCreateInstance<double>(5, TestLibrary.Generator.GetDouble(-55)); }
    public bool PosTest6() { return PosCreateInstance<float>(6, TestLibrary.Generator.GetSingle(-55)); }
    public bool PosTest7() { return PosCreateInstance<char>(7, TestLibrary.Generator.GetCharLetter(-55)); }
    public bool PosTest8() { return PosCreateInstance<char>(8, TestLibrary.Generator.GetCharNumber(-55)); }
    public bool PosTest9() { return PosCreateInstance<char>(9, TestLibrary.Generator.GetChar(-55)); }
    public bool PosTest10() { return PosCreateInstance<string>(10, TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN)); }



    public bool PosCreateInstance<T>(int id, T defaultValue)
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Array.CreateInstance(Type, int) (T == "+typeof(T)+")");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;

            array  = Array.CreateInstance(typeof(T), length);

            array.SetValue(defaultValue, 0);

            if (length != array.Length)
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected length: Expected(" + length + ") Actual(" + array.Length + ")");
                retVal = false;
            }

            if (typeof(T) != array.GetValue(0).GetType())
            {
                TestLibrary.TestFramework.LogError("001", "Type mismatch: Expected(" + typeof(T) + ") Actual(" + array.GetValue(0).GetType() + ")");
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

    public bool NegTest1()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.CreateInstance(Type, int) where T is null");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;

            array  = Array.CreateInstance(null, length);

            TestLibrary.TestFramework.LogError("003", "Execption expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool     retVal = true;
        Array    array;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Array.CreateInstance(Type, int) length < 0");

        try
        {
            // creat the array
            array  = Array.CreateInstance(typeof(Int32), -1);

            TestLibrary.TestFramework.LogError("005", "Exception expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
