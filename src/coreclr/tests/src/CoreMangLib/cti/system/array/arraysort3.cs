// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayBinarySort2
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_NUM_LOOPS = 50;

    public static int Main()
    {
        ArrayBinarySort2 ac = new ArrayBinarySort2();

        TestLibrary.TestFramework.BeginTestCase("Array.Sort(Array, Array, int, int, IComparer)");

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool      retVal = true;
        Array     keys;
        Array     items;
        int       length;
        IComparer myc;
        byte      element;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.Sort(Array, Array, int, int, IComparer) ");

        try
        {
            myc = new MyComparer();

            for (int j=0; j<c_NUM_LOOPS; j++)
            {
                // creat the array
                length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
                keys   = Array.CreateInstance(typeof(byte), length);
                items  = Array.CreateInstance(typeof(byte), length);

                // fill the array
                for (int i=0; i<keys.Length; i++)
                {
                    keys.SetValue((object)TestLibrary.Generator.GetByte(-55), i);
                    items.SetValue(keys.GetValue(i), i);
                }

                Array.Sort(keys, items, 0, length, myc);

                // ensure that all the elements are sorted
                element = (byte)keys.GetValue(0);
                for(int i=0; i<keys.Length; i++)
                {
                    if (element > (byte)keys.GetValue(i))
                    {
                        TestLibrary.TestFramework.LogError("000", "Unexpected key: Element (" + element + ") is greater than (" + (byte)keys.GetValue(i) + ")");
                        retVal = false;
                    }
                    if ((byte)items.GetValue(i) != (byte)keys.GetValue(i))
                    {
                        TestLibrary.TestFramework.LogError("001", "Unexpected item: Expected(" + (byte)keys.GetValue(i) + ") Actual(" + (byte)items.GetValue(i) + ")");
                        retVal = false;
                    }
                }
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
        bool      retVal = true;
        Array     keys;
        Array     items;
        int       length;
        IComparer myc;
        byte      element;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Array.Sort(Array, Array, int, int, IComparer) items is null");

        try
        {
            myc = new MyComparer();

            for (int j=0; j<c_NUM_LOOPS; j++)
            {
                // creat the array
                length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
                keys   = Array.CreateInstance(typeof(byte), length);
                items  = null;

                // fill the array
                for (int i=0; i<keys.Length; i++)
                {
                    keys.SetValue((object)TestLibrary.Generator.GetByte(-55), i);
                }

                Array.Sort(keys, items, 0, length, myc);

                // ensure that all the elements are sorted
                element = (byte)keys.GetValue(0);
                for(int i=0; i<keys.Length; i++)
                {
                    if (element > (byte)keys.GetValue(i))
                    {
                        TestLibrary.TestFramework.LogError("003", "Unexpected key: Element (" + element + ") is greater than (" + (byte)keys.GetValue(i) + ")");
                        retVal = false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool  retVal = true;
        Array keys;
        Array items;
        IComparer myc;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.Sort(Array, Array, int, int, IComparer) keys is null");

        try
        {
            keys  = null;
            items = null;
            myc   = new MyComparer();

            Array.Sort(keys, items, 0, 0, myc);

            TestLibrary.TestFramework.LogError("005", "Exception expected.");
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

    public bool NegTest2()
    {
        bool  retVal = true;
        Array keys;
        Array items;
        IComparer myc;
        int       length;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Array.Sort(Array, Array, int, int, IComparer) length < 0");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            keys   = Array.CreateInstance(typeof(byte), length);
            items  = Array.CreateInstance(typeof(byte), length);
            myc   = new MyComparer();

            Array.Sort(keys, items, 0, -1, myc);

            TestLibrary.TestFramework.LogError("007", "Exception expected.");
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

    public bool NegTest3()
    {
        bool  retVal = true;
        Array keys;
        Array items;
        IComparer myc;
        int       length;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Array.Sort(Array, Array, int, int, IComparer) length too long");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            keys   = Array.CreateInstance(typeof(byte), length);
            items  = Array.CreateInstance(typeof(byte), length);
            myc   = new MyComparer();

            Array.Sort(keys, items, length+10, length, myc);

            TestLibrary.TestFramework.LogError("009", "Exception expected.");
            retVal = false;
        }
        catch (ArgumentException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public class MyComparer : IComparer
    {
        public int Compare(object obj1, object obj2)
        {

            if ((byte)obj1 == (byte)obj2) return 0;

            return ((byte)obj1 < (byte)obj2) ? -1 : 1;
        }
    }

}
