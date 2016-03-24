// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayReverse2
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayReverse2 ac = new ArrayReverse2();

        TestLibrary.TestFramework.BeginTestCase("Array.Reverse(Array, int, int)");

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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool  retVal = true;
        Array afterArray;
        Array beforeArray;
        int   length;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.Reverse(Array, int, int)");

        try
        {
            // creat the array
            length       = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            beforeArray  = Array.CreateInstance(typeof(Int32), length);
            afterArray   = Array.CreateInstance(typeof(Int32), length);

            // fill the array
            for (int i=0; i<beforeArray.Length; i++)
            {
                beforeArray.SetValue((object)TestLibrary.Generator.GetInt32(-55), i);
            }

            // copy the array
            Array.Copy(beforeArray, afterArray, length);

            Array.Reverse(afterArray, 0, length);

            if (beforeArray.Length != afterArray.Length)
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected length: Expected(" + beforeArray.Length + ") Actual(" + afterArray.Length + ")");
                retVal = false;
            }

            for (int i=0; i<beforeArray.Length; i++)
            {
                if ((int)beforeArray.GetValue(length-i-1) != (int)afterArray.GetValue(i))
                {
                    TestLibrary.TestFramework.LogError("001", "Unexpected value: Expected(" + beforeArray.GetValue(length-i-1) + ") Actual(" + afterArray.GetValue(i) + ")");
                    retVal = false;
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
        bool  retVal = true;
        Array afterArray;
        Array beforeArray;
        int   length;
        int   startIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Array.Reverse(Array, int, int) only reverse half of the array");

        try
        {
            // creat the array
            length       = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            beforeArray  = Array.CreateInstance(typeof(Int32), length);
            afterArray   = Array.CreateInstance(typeof(Int32), length);

            startIndex   = length/2;

            // fill the array
            for (int i=0; i<beforeArray.Length; i++)
            {
                beforeArray.SetValue((object)TestLibrary.Generator.GetInt32(-55), i);
            }

            // copy the array
            Array.Copy(beforeArray, afterArray, length);

            Array.Reverse(afterArray, startIndex, length-startIndex);

            if (beforeArray.Length != afterArray.Length)
            {
                TestLibrary.TestFramework.LogError("003", "Unexpected length: Expected(" + beforeArray.Length + ") Actual(" + afterArray.Length + ")");
                retVal = false;
            }

            for (int i=0; i<beforeArray.Length; i++)
            {
                int beforeIndex;

                if (i < startIndex)
                {
                    beforeIndex = i;
                }
                else
                {
                    beforeIndex = (length-(i-startIndex)-1);
                }

                if ((int)beforeArray.GetValue(beforeIndex) != (int)afterArray.GetValue(i))
                {
                    TestLibrary.TestFramework.LogError("004", "Unexpected value: Expected(" + beforeArray.GetValue(beforeIndex) + ") Actual(" + afterArray.GetValue(i) + ")");
                    retVal = false;
                }
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
        bool  retVal = true;
        Array afterArray;
        Array beforeArray;
        int   length;
        int   endIndex;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Array.Reverse(Array, int, int) only reverse half of the array (begining)");

        try
        {
            // creat the array
            length       = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            beforeArray  = Array.CreateInstance(typeof(Int32), length);
            afterArray   = Array.CreateInstance(typeof(Int32), length);

            endIndex   = length/2;

            // fill the array
            for (int i=0; i<beforeArray.Length; i++)
            {
                beforeArray.SetValue((object)TestLibrary.Generator.GetInt32(-55), i);
            }

            // copy the array
            Array.Copy(beforeArray, afterArray, length);

            Array.Reverse(afterArray, 0, endIndex);

            if (beforeArray.Length != afterArray.Length)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected length: Expected(" + beforeArray.Length + ") Actual(" + afterArray.Length + ")");
                retVal = false;
            }

            for (int i=0; i<beforeArray.Length; i++)
            {
                int beforeIndex;

                if (i >= endIndex)
                {
                    beforeIndex = i;
                }
                else
                {
                    beforeIndex = endIndex-i-1;
                }

                if ((int)beforeArray.GetValue(beforeIndex) != (int)afterArray.GetValue(i))
                {
                    TestLibrary.TestFramework.LogError("007", "Unexpected value: Expected(" + beforeArray.GetValue(beforeIndex) + ") Actual(" + afterArray.GetValue(i) + ")");
                    retVal = false;
                }
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
        bool  retVal = true;
        Array afterArray;
        Array beforeArray;
        int   length;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Array.Reverse(Array, int, int) array of strings");

        try
        {
            // creat the array
            length       = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            beforeArray  = Array.CreateInstance(typeof(string), length);
            afterArray   = Array.CreateInstance(typeof(string), length);

            // fill the array
            for (int i=0; i<beforeArray.Length; i++)
            {
                beforeArray.SetValue(TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN), i);
            }

            // copy the array
            Array.Copy(beforeArray, afterArray, length);

            Array.Reverse(afterArray, 0, length);

            if (beforeArray.Length != afterArray.Length)
            {
                TestLibrary.TestFramework.LogError("009", "Unexpected length: Expected(" + beforeArray.Length + ") Actual(" + afterArray.Length + ")");
                retVal = false;
            }

            for (int i=0; i<beforeArray.Length; i++)
            {
                if (!beforeArray.GetValue(length-i-1).Equals(afterArray.GetValue(i)))
                {
                    TestLibrary.TestFramework.LogError("010", "Unexpected value: Expected(" + beforeArray.GetValue(length-i-1) + ") Actual(" + afterArray.GetValue(i) + ")");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool  retVal = true;
        Array afterArray;
        Array beforeArray;
        int   length;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Array.Reverse(Array, int, int) array of MyStructs");

        try
        {
            // creat the array
            length       = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            beforeArray  = Array.CreateInstance(typeof(MyStruct), length);
            afterArray   = Array.CreateInstance(typeof(MyStruct), length);

            // fill the array
            for (int i=0; i<beforeArray.Length; i++)
            {
                beforeArray.SetValue(new MyStruct(TestLibrary.Generator.GetSingle(-55)), i);
            }

            // copy the array
            Array.Copy(beforeArray, afterArray, length);

            Array.Reverse(afterArray, 0, length);

            if (beforeArray.Length != afterArray.Length)
            {
                TestLibrary.TestFramework.LogError("012", "Unexpected length: Expected(" + beforeArray.Length + ") Actual(" + afterArray.Length + ")");
                retVal = false;
            }

            for (int i=0; i<beforeArray.Length; i++)
            {
                if (((MyStruct)beforeArray.GetValue(length-i-1)).f != ((MyStruct)afterArray.GetValue(i)).f)
                {
                    TestLibrary.TestFramework.LogError("013", "Unexpected value: Expected(" + ((MyStruct)beforeArray.GetValue(length-i-1)).f + ") Actual(" + ((MyStruct)afterArray.GetValue(i)).f + ")");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool  retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.Reverse(Array, int, int) where array is null");

        try
        {
            Array.Reverse(null, 0, 0);

            TestLibrary.TestFramework.LogError("015", "Exception expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.Reverse(Array, int, int) where index is negative");

        try
        {
             length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
             array  = Array.CreateInstance(typeof(Int32), length);

            Array.Reverse(array, -1, length);

            TestLibrary.TestFramework.LogError("017", "Exception expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Array.Reverse(Array, int, int) where index is greater than length");

        try
        {
             length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
             array  = Array.CreateInstance(typeof(Int32), length);

            Array.Reverse(array, length, length);

            TestLibrary.TestFramework.LogError("019", "Exception expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Array.Reverse(Array, int, int) multi dimension array");

        try
        {
             length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
             array  = Array.CreateInstance(typeof(Int32), new int[2] {length, length});

            Array.Reverse(array, length, length);

            TestLibrary.TestFramework.LogError("021", "Exception expected");
            retVal = false;
        }
        catch (RankException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public struct MyStruct
    {
        public float f;

        public MyStruct(float ff) { f = ff; }
    }
}
