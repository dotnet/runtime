// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class ArrayLastIndexOf1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayLastIndexOf1 ac = new ArrayLastIndexOf1();

        TestLibrary.TestFramework.BeginTestCase("Array.LastInexOf(Array, object, int, int)");

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

    public bool PosTest1()
    {
        bool  retVal = true;
        Array array;
        int   length;
        int   element;
        int   index;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.LastInexOf(Array, object, int, int) where element is found");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            element = TestLibrary.Generator.GetInt32(-55);

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue((object)TestLibrary.Generator.GetInt32(-55), i);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            newIndex = Array.LastIndexOf(array, (object)element, array.Length-1, array.Length);

            if (index > newIndex)
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected index: Expected(" + index + ") Actual(" + newIndex + ")");
                retVal = false;
            }

            if (element != (int)array.GetValue(newIndex))
            {
                TestLibrary.TestFramework.LogError("001", "Unexpected value: Expected(" + element + ") Actual(" + (int)array.GetValue(newIndex) + ")");
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
        bool   retVal = true;
        Array  array;
        int    length;
        string element;
        int    index;
        int    newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Array.LastInexOf(Array, object, int, int) non-primitive type");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(string), length);

            element = TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN);

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue(TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN), i);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            newIndex = Array.LastIndexOf(array, (object)element, array.Length-1, array.Length);

            if (index > newIndex)
            {
                TestLibrary.TestFramework.LogError("003", "Unexpected index: Expected(" + index + ") Actual(" + newIndex + ")");
                retVal = false;
            }

            if (!element.Equals(array.GetValue(newIndex)))
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected value: Expected(" + element + ") Actual(" + array.GetValue(newIndex) + ")");
                retVal = false;
            }
            
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool   retVal = true;
        Array  array;
        int    length;
        object element;
        int    index;
        int    newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Array.LastInexOf(Array, object, int, int) non-primitive type (with value == null)");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(string), length);

            element = null;

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue(TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN), i);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            newIndex = Array.LastIndexOf(array, (object)element, array.Length-1, array.Length);

            if (index > newIndex)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected index: Expected(" + index + ") Actual(" + newIndex + ")");
                retVal = false;
            }

            if (element != array.GetValue(newIndex))
            {
                TestLibrary.TestFramework.LogError("007", "Unexpected value: Expected(" + element + ") Actual(" + array.GetValue(newIndex) + ")");
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

    public bool PosTest4()
    {
        bool     retVal = true;
        Array    array;
        int      length;
        MyStruct element;
        int      index;
        int      newIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Array.LastInexOf(Array, object, int, int) non-primitive type (not derived from object)");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(MyStruct), length);

            element = new MyStruct(TestLibrary.Generator.GetSingle(-55));

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue(new MyStruct(TestLibrary.Generator.GetSingle(-55)), i);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            newIndex = Array.LastIndexOf(array, (object)element, array.Length-1, array.Length);

            if (index > newIndex)
            {
                TestLibrary.TestFramework.LogError("009", "Unexpected index: Expected(" + index + ") Actual(" + newIndex + ")");
                retVal = false;
            }

            if (element.f != ((MyStruct)array.GetValue(newIndex)).f)
            {
                TestLibrary.TestFramework.LogError("010", "Unexpected value: Expected(" + element.f + ") Actual(" + ((MyStruct)array.GetValue(newIndex)).f + ")");
                retVal = false;
            }
            
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool  retVal = true;
        Array array;
        int   length;
        int   element;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.LastInexOf(Array, object, int, int) where element is not found");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            element = TestLibrary.Generator.GetInt32(-55);

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                do
                {
                    array.SetValue((object)TestLibrary.Generator.GetInt32(-55), i);
                }
                while (element == (int)array.GetValue(i));
            }

            newIndex = Array.LastIndexOf(array, (object)element, array.Length-1, array.Length);

            if (-1 != newIndex)
            {
                TestLibrary.TestFramework.LogError("012", "Unexpected index: Actual(" + newIndex + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool  retVal = true;
        Array array;
        int   length;
        int   element;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Array.LastInexOf(Array, object, int, int) array full of null's");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            element = TestLibrary.Generator.GetInt32(-55);

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue(null, i);
            }

            newIndex = Array.LastIndexOf(array, (object)element, array.Length-1, array.Length);

            if (-1 != newIndex)
            {
                TestLibrary.TestFramework.LogError("014", "Unexpected index: Actual(" + newIndex + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool  retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Array.LastInexOf(Array, object, int, int) array is null");

        try
        {
            Array.LastIndexOf(null, (object)1, 0, 0);

            TestLibrary.TestFramework.LogError("016", "Should have thrown an expection");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("017", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


    public bool NegTest4()
    {
        bool  retVal = true;
        Array array;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Array.LastInexOf(Array, object, int, int) array of length 0");

        try
        {
            // creat the array
            array  = Array.CreateInstance(typeof(Int32), 0);

            newIndex = Array.LastIndexOf(array, (object)null, array.Length-1, array.Length);

            if (-1 != newIndex)
            {
                TestLibrary.TestFramework.LogError("018", "Unexpected index: Actual(" + newIndex + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("019", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest5: Array.LastInexOf(Array, object, int, int) start index less than lower bound");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.LastIndexOf(array, (object)null, -1, array.Length);

            TestLibrary.TestFramework.LogError("020", "Should have thrown an exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("021", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest6: Array.LastInexOf(Array, object, int, int) start index greater than upper bound");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.LastIndexOf(array, (object)null, array.Length, array.Length);

            TestLibrary.TestFramework.LogError("022", "Should have thrown an exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("023", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest7: Array.LastInexOf(Array, object, int, int) count less than 0");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.LastIndexOf(array, (object)null, array.Length-1, -1);

            TestLibrary.TestFramework.LogError("024", "Should have thrown an exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("025", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest8()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest8: Array.LastInexOf(Array, object, int, int) (count > startIndex - lb + 1)");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.LastIndexOf(array, (object)null, 0, array.Length);

            TestLibrary.TestFramework.LogError("026", "Should have thrown an exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("027", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest9()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest9: Array.LastInexOf(Array, object, int, int) multi dim array");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), new int[2] {length, length});

            Array.LastIndexOf(array, (object)null, array.Length-1, array.Length);

            TestLibrary.TestFramework.LogError("028", "Should have thrown an exception");
            retVal = false;
        }
        catch (RankException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("029", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public struct MyStruct
    {
        public float f;

        public MyStruct(float ff) { f = ff; }
    }

    public bool NegTest10()
    {
        bool     retVal = true;
        Array    array;
        int      length;
        MyStruct element;
        int      newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest10: Array.LastInexOf(Array, object, int, int) non-primitive type (not derived from object) not found");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(MyStruct), length);

            element = new MyStruct(TestLibrary.Generator.GetSingle(-55));

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                do
                {
                    array.SetValue(new MyStruct(TestLibrary.Generator.GetSingle(-55)), i);
                }
                while(element.Equals((MyStruct)array.GetValue(i)));
            }

            newIndex = Array.LastIndexOf(array, (object)element, array.Length-1, array.Length);

            if (-1 != newIndex)
            {
                TestLibrary.TestFramework.LogError("030", "Unexpected index: Expected(-1) Actual(" + newIndex + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("031", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
