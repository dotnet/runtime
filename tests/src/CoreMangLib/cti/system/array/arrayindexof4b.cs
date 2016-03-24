// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class ArrayIndexOf4
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayIndexOf4 ac = new ArrayIndexOf4();

        TestLibrary.TestFramework.BeginTestCase("Array.IndexOf(T[] array, T value)");

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
        retVal = PosTest11() && retVal;

        TestLibrary.TestFramework.LogInformation("");

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

    public bool PosTest1() { return PosIndexOf<Int64>(1, TestLibrary.Generator.GetInt64(-55), TestLibrary.Generator.GetInt64(-55)); }
    public bool PosTest2() { return PosIndexOf<Int32>(2, TestLibrary.Generator.GetInt32(-55), TestLibrary.Generator.GetInt32(-55)); }
    public bool PosTest3() { return PosIndexOf<Int16>(3, TestLibrary.Generator.GetInt16(-55), TestLibrary.Generator.GetInt16(-55)); }
    public bool PosTest4() { return PosIndexOf<Byte>(4, TestLibrary.Generator.GetByte(-55), TestLibrary.Generator.GetByte(-55)); }
    public bool PosTest5() { return PosIndexOf<double>(5, TestLibrary.Generator.GetDouble(-55), TestLibrary.Generator.GetDouble(-55)); }
    public bool PosTest6() { return PosIndexOf<float>(6, TestLibrary.Generator.GetSingle(-55), TestLibrary.Generator.GetSingle(-55)); }
    public bool PosTest7() { return PosIndexOf<char>(7, TestLibrary.Generator.GetCharLetter(-55), TestLibrary.Generator.GetCharLetter(-55)); }
    public bool PosTest8() { return PosIndexOf<char>(8, TestLibrary.Generator.GetCharNumber(-55), TestLibrary.Generator.GetCharNumber(-55)); }
    public bool PosTest9() { return PosIndexOf<char>(9, TestLibrary.Generator.GetChar(-55), TestLibrary.Generator.GetChar(-55)); }
    public bool PosTest10() { return PosIndexOf<string>(10, TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN), TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN)); }

    public bool PosTest11() { return PosIndexOf2<Int32>(11, 1, 0, 0, 0); }

    public bool NegTest1() { return NegIndexOf<Int32>(1, 1); }
                                                      // id, defaultValue, length, startIndex, count
    public bool NegTest2() { return NegIndexOf2<Int32>(  2,  1,            0,       1,          0); }
    public bool NegTest3() { return NegIndexOf2<Int32>(  3,  1,            0,      -2,          0); }
    public bool NegTest4() { return NegIndexOf2<Int32>(  4,  1,            0,      -1,          1); }
    public bool NegTest5() { return NegIndexOf2<Int32>(  5,  1,            0,       0,          1); }
    public bool NegTest6() { return NegIndexOf2<Int32>(  6,  1,            1,      -1,          1); }
    public bool NegTest7() { return NegIndexOf2<Int32>(  7,  1,            1,       2,          1); }
    public bool NegTest8() { return NegIndexOf2<Int32>(  8,  1,            1,       0,         -1); }
    public bool NegTest9() { return NegIndexOf2<Int32>(  9,  1,            1,       0,         -1); }
    public bool NegTest10() { return NegIndexOf2<Int32>(10,  1,            1,       1,          2); }

    public bool PosIndexOf<T>(int id, T element, T otherElem)
    {
        bool  retVal = true;
        T[]   array;
        int   length;
        int   index;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Array.IndexOf(T[] array, T value, int startIndex, int count) (T=="+typeof(T)+") where value is found");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = new T[length];

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array[i] = otherElem;
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            newIndex = Array.IndexOf<T>(array, element, 0, array.Length);

            if (index < newIndex)
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected index: Expected(" + index + ") Actual(" + newIndex + ")");
                retVal = false;
            }

            if (!element.Equals(array[newIndex]))
            {
                TestLibrary.TestFramework.LogError("001", "Unexpected value: Expected(" + element + ") Actual(" + array[newIndex] + ")");
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

    public bool  PosIndexOf2<T>(int id, T defaultValue, int length, int startIndex, int count)
    {
        bool  retVal = true;
        T[]   array  = null;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Array.IndexOf(T["+length+"] array, T value, "+startIndex+", "+count+") (T == "+typeof(T)+" where array is null");

        try
        {
            array = new T[ length ];

            newIndex = Array.IndexOf<T>(array, defaultValue, startIndex, count);

            if (-1 != newIndex)
            {
                TestLibrary.TestFramework.LogError("003", "Unexpected value: Expected(-1) Actual("+newIndex+")");
                retVal = false;
            }
        }
        catch (ArgumentOutOfRangeException)
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

    public bool NegIndexOf<T>(int id, T defaultValue)
    {
        bool  retVal = true;
        T[]   array  = null;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Array.IndexOf(T[] array, T value, int startIndex, int count) (T == "+typeof(T)+" where array is null");

        try
        {
            Array.IndexOf<T>(array, defaultValue, 0, 0);

            TestLibrary.TestFramework.LogError("005", "Exepction should have been thrown");
            retVal = false;
        }
        catch (ArgumentNullException)
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

    public bool  NegIndexOf2<T>(int id, T defaultValue, int length, int startIndex, int count)
    {
        bool  retVal = true;
        T[]   array  = null;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Array.IndexOf(T["+length+"] array, T value, "+startIndex+", "+count+") (T == "+typeof(T)+" where array is null");

        try
        {
            array = new T[ length ];

            Array.IndexOf<T>(array, defaultValue, startIndex, count);

            TestLibrary.TestFramework.LogError("007", "Exepction should have been thrown");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}

