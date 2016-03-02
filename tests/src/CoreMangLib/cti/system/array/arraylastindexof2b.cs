// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class ArrayLastIndexOf2
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayLastIndexOf2 ac = new ArrayLastIndexOf2();

        TestLibrary.TestFramework.BeginTestCase("Array.LastInexOf(T[] array, T value)");

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

    public bool NegTest1() { return NegIndexOf<Int32>(1, 1); }

    public bool PosIndexOf<T>(int id, T element, T otherElem)
    {
        bool  retVal = true;
        T[]   array;
        int   length;
        int   index;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Array.LastInexOf(T[] array, T value) (T=="+typeof(T)+") where value is found");

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

            newIndex = Array.LastIndexOf<T>(array, element);

            if (index > newIndex)
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

    public bool NegIndexOf<T>(int id, T defaultValue)
    {
        bool  retVal = true;
        T[]   array  = null;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Array.LastInexOf(T[] array, T value) (T == "+typeof(T)+" where array is null");

        try
        {
            Array.LastIndexOf<T>(array, defaultValue);

            TestLibrary.TestFramework.LogError("003", "Exepction should have been thrown");
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
}
