// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayBinarySearch1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayBinarySearch1 ac = new ArrayBinarySearch1();

        TestLibrary.TestFramework.BeginTestCase("Array.BinarySearch(Array, int, int, object, IComparer)");

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
        retVal = NegTest11() && retVal;

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.BinarySearch(Array, int, int, object, IComparer) where element is found");

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
                while(element == (int)array.GetValue(i));
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            Array.Sort(array);

            newIndex = Array.BinarySearch(array, 0, array.Length, (object)element, null);

            if (element != (int)array.GetValue(newIndex))
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected value: Expected(" + element + ") Actual(" + (int)array.GetValue(newIndex) + ")");
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

    public bool PosTest2()
    {
        bool             retVal = true;
        Array            array;
        int              length;
        MyStruct         element;
        int              index;
        int              newIndex;
	MyStructComparer msc = new MyStructComparer();

        TestLibrary.TestFramework.BeginScenario("PosTest2: Array.BinarySearch(Array, int, int, object, IComparer) non-primitive type (not derived from object)");

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
                while(element.f == ((MyStruct)array.GetValue(i)).f);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            Array.Sort(array);

            newIndex = Array.BinarySearch(array, 0, array.Length, (object)element, msc);

            if (element.f != ((MyStruct)array.GetValue(newIndex)).f)
            {
                TestLibrary.TestFramework.LogError("002", "Unexpected value: Expected(" + element.f + ") Actual(" + ((MyStruct)array.GetValue(newIndex)).f + ")");
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

    public bool NegTest1()
    {
        bool  retVal = true;
        Array array;
        int   length;
        int   element;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.BinarySearch(Array, int, int, object, IComparer) where element is not found");

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
                while(element == (int)array.GetValue(i));
            }

            Array.Sort(array);

            newIndex = Array.BinarySearch(array, 0, array.Length, (object)element, null);

            if (0 <= newIndex)
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected index: Expected(<0) Actual(" + newIndex + ")");
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

    public bool NegTest2()
    {
        bool     retVal = true;
        Array    array;
        int      length;
        MyStruct element;
        int      newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Array.BinarySearch(Array, int, int, object, IComparer) non-primitive type (not derived from object) not found");

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
                while(element.f == ((MyStruct)array.GetValue(i)).f);
            }

            Array.Sort(array);

            newIndex = Array.BinarySearch(array, 0, array.Length, (object)element, null);

            if (0 <= newIndex)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected index: Expected(<0) Actual(" + newIndex + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool  retVal = true;
        Array array;
        int   length;
        int   element;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Array.BinarySearch(Array, int, int, object, IComparer) array full of null's");

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

            newIndex = Array.BinarySearch(array, 0, array.Length, (object)element, null);

            if (0 <= newIndex)
            {
                TestLibrary.TestFramework.LogError("008", "Unexpected index: Actual(" + newIndex + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool  retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Array.BinarySearch(Array, int, int, object, IComparer) array is null");

        try
        {
            Array.BinarySearch(null, 0, 0, (object)1, null);

            TestLibrary.TestFramework.LogError("010", "Should have thrown an expection");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


    public bool NegTest5()
    {
        bool  retVal = true;
        Array array;
        int   newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest5: Array.BinarySearch(Array, int, int, object, IComparer) array of length 0");

        try
        {
            // creat the array
            array  = Array.CreateInstance(typeof(Int32), 0);

            newIndex = Array.BinarySearch(array, 0, array.Length, (object)null, null);

            if (0 <= newIndex)
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

    public bool NegTest6()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest6: Array.BinarySearch(Array, int, int, object, IComparer) start index less than lower bound");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.BinarySearch(array, -1, array.Length, (object)null, null);

            TestLibrary.TestFramework.LogError("014", "Should have thrown an exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest7: Array.BinarySearch(Array, int, int, object, IComparer) start index greater than upper bound");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.BinarySearch(array, array.Length, array.Length, (object)null, null);

            TestLibrary.TestFramework.LogError("016", "Should have thrown an exception");
            retVal = false;
        }
        catch (ArgumentException)
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

    public bool NegTest8()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest8: Array.BinarySearch(Array, int, int, object, IComparer) count less than 0");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.BinarySearch(array, 0, -1, (object)null, null);

            TestLibrary.TestFramework.LogError("018", "Should have thrown an exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("019", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest9()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest9: Array.BinarySearch(Array, int, int, object, IComparer) (startindex < 0)");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            Array.BinarySearch(array, -1, array.Length, (object)null, null);

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

    public bool NegTest10()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest10: Array.BinarySearch(Array, int, int, object, IComparer) multi dim array");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), new int[2] {length, length});

            Array.BinarySearch(array, 0, array.Length, (object)null, null);

            TestLibrary.TestFramework.LogError("022", "Should have thrown an exception");
            retVal = false;
        }
        catch (RankException)
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

    public bool NegTest11()
    {
        bool      retVal = true;
        Array     array;
        int       length;
        MyStruct2 element;
        int       index;
        int       newIndex;

        TestLibrary.TestFramework.BeginScenario("NegTest11: Array.BinarySearch(Array, int, int, object, IComparer) non-primitive type (not derived from object or IComparable)");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(MyStruct2), length);

            element = new MyStruct2(TestLibrary.Generator.GetSingle(-55));

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                do
                {
                    array.SetValue(new MyStruct2(TestLibrary.Generator.GetSingle(-55)), i);
                }
                while(element.f == ((MyStruct2)array.GetValue(i)).f);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            Array.Sort(array);

            newIndex = Array.BinarySearch(array, 0, array.Length, (object)element, null);

            TestLibrary.TestFramework.LogError("024", "Exception expected");
            retVal = false;
        }
        catch (InvalidOperationException)
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


    public struct MyStruct : IComparable
    {
        public float f;

        public MyStruct(float ff) { f = ff; }

        public int CompareTo(object obj)
        {
             MyStruct m1 = (MyStruct)obj;

             if (m1.f == f) return 0;

             return ((m1.f < f)?1:-1);
         }
    }

    public struct MyStruct2
    {
        public float f;

        public MyStruct2(float ff) { f = ff; }
    }

    public class MyStructComparer : IComparer
    {
         public int Compare(object x, object y)
         {
             MyStruct m1 = (MyStruct)x;
             MyStruct m2 = (MyStruct)y;

             if (m1.f == m2.f) return 0;

             return ((m1.f < m2.f)?-1:1);
         }
    }
}
