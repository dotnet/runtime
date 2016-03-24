// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayBinarySearch4
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayBinarySearch4 ac = new ArrayBinarySearch4();

        TestLibrary.TestFramework.BeginTestCase("Array.BinarySearch<T>(T[], T)");

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

    public bool PosTest1() { return PosBinarySearch<Int64>(1, TestLibrary.Generator.GetInt64(), TestLibrary.Generator.GetInt64()); }
    public bool PosTest2() { return PosBinarySearch<Int32>(2, TestLibrary.Generator.GetInt32(), TestLibrary.Generator.GetInt32()); }
    public bool PosTest3() { return PosBinarySearch<Int16>(3, TestLibrary.Generator.GetInt16(), TestLibrary.Generator.GetInt16()); }
    public bool PosTest4() { return PosBinarySearch<Byte>(4, TestLibrary.Generator.GetByte(), TestLibrary.Generator.GetByte()); }
    public bool PosTest5() { return PosBinarySearch<double>(5, TestLibrary.Generator.GetDouble(), TestLibrary.Generator.GetDouble()); }
    public bool PosTest6() { return PosBinarySearch<float>(6, TestLibrary.Generator.GetSingle(), TestLibrary.Generator.GetSingle()); }
    public bool PosTest7() { return PosBinarySearch<char>(7, TestLibrary.Generator.GetCharLetter(), TestLibrary.Generator.GetCharLetter()); }
    public bool PosTest8() { return PosBinarySearch<char>(8, TestLibrary.Generator.GetCharNumber(), TestLibrary.Generator.GetCharNumber()); }
    public bool PosTest9() { return PosBinarySearch<char>(9, TestLibrary.Generator.GetChar(), TestLibrary.Generator.GetChar()); }
    public bool PosTest10() { return PosBinarySearch<string>(10, TestLibrary.Generator.GetString(false, c_MIN_STRLEN, c_MAX_STRLEN), TestLibrary.Generator.GetString(false, c_MIN_STRLEN, c_MAX_STRLEN)); }

    public bool PosBinarySearch<T>(int id, T element, T otherElem) where T : IComparable<T>
    {
        bool  retVal = true;
        T[]   array;
        int   length;
        int   index;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Array.BinarySearch<T>(T[], int, int, T) (T == "+typeof(T)+") where element is found");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32() % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = new T[length];

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue(otherElem, i);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32() % length;

            // set the value
            array.SetValue( element, index);

            Array.Sort(array);

            newIndex = Array.BinarySearch<T>(array, 0, length, element);

            if (0 != element.CompareTo((T) array.GetValue(newIndex)))
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected value: Expected(" + element + ") Actual(" + array.GetValue(newIndex) + ")");
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

    public bool NegTest1()
    {
        bool  retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Array.BinarySearch<T>(T[], T) array is null");

        try
        {
            Array.BinarySearch<Int32>(null, 0, 0, 1);

            TestLibrary.TestFramework.LogError("002", "Should have thrown an expection");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
