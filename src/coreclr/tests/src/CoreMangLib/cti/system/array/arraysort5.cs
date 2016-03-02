// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Array.Sort(System.Array,System.Int32,System.Int32,System.Collections.IComparer)
/// </summary>
public class ArraySort5
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest8() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Sort a string array using default comparer ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom",
                "Allin"};
            string[] s2 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Allin",
                "Peter",
                "Tom"};
            Array.Sort(s1, 3, 3, null);
            for (int i = 0; i < 6; i++)
            {
                if (s1[i] != s2[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
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
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using comparer ");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetByte(-55);
                i1[i] = value;
                i2[i] = value;
            }
            IComparer a1 = new A();
            int startIdx = GetInt(0, length - 2);
            int endIdx = GetInt(startIdx, length - 1);
            int count = length == 0 ? length : endIdx - startIdx + 1;
            Array.Sort(i1, startIdx, count, a1);
            for (int i = startIdx; i < endIdx; i++)  //manually quich sort
            {
                for (int j = i + 1; j <= endIdx; j++)
                {
                    if (i2[i] > i2[j])
                    {
                        int temp = i2[i];
                        i2[i] = i2[j];
                        i2[j] = temp;
                    }
                }
            }
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,the start index is:" + startIdx.ToString() + "the end index is:" + endIdx.ToString());
                    retVal = false;
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array using reverse comparer ");

        try
        {
            char[] c1 = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] d1 = new char[10] { 'a', 'e', 'd', 'c', 'b', 'f', 'g', 'h', 'i', 'j' };
            IComparer b = new B();
            Array.Sort(c1, 1, 4, b);
            for (int i = 0; i < 10; i++)
            {
                if (c1[i] != d1[i])
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort an int array from the minimal index to the maximal index of the array ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetByte(-55);
                i1[i] = value;
                i2[i] = value;
            }
            int startIdx = 0;
            int count = length;
            Array.Sort(i1, startIdx, count, new A());
            Array.Sort(i2, new A());
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("007", string.Format("The result is not the value as expected, the length is:{0}", length));
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5:The implementation of comparer caused an error during the sort");

        try
        {
            int[] i1 = new int[9] { 2, 34, 56, 87, 34, 23, 209, 34, 87 };
            IComparer f = new F();
            Array.Sort(i1, 0, 9, f);
            for(int i=1; i<i1.Length; i++)
            {
                if (i1[i-1] > i1[i])
                {
                    TestLibrary.TestFramework.LogError("107", "The " + i + " element in the array is out of order: [i-1]=" + i1[i-1] + " [i]="+i1[i]);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("114", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array to be sorted is null reference ");

        try
        {
            string[] s1 = null;
            Array.Sort(s1, 0, 2, null);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The array is not one dimension ");

        try
        {
            int[,] i1 = new int[2, 3] { { 2, 3, 5 }, { 34, 56, 77 } };
            Array.Sort(i1, 0, 3, null);
            TestLibrary.TestFramework.LogError("103", "The RankException is not throw as expected ");
            retVal = false;
        }
        catch (RankException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: The start index is less than the minimal bound of the array");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Peter",
                "Mike",
                "Tom",
                "Allin"};
            Array.Sort(s1, -1, 4, null);
            TestLibrary.TestFramework.LogError("105", "The ArgumentOutOfRangeException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Length is less than zero");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Peter",
                "Mike",
                "Tom",
                "Allin"};
            Array.Sort(s1, 3, -3, null);
            TestLibrary.TestFramework.LogError("107", "The ArgumentOutOfRangeException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: The start index is greater than the maximal range of the array");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(-55, false, 0, 10);
                s1[i] = value;
            }
            int startIdx = GetInt(1, Byte.MaxValue);
            int increment = length;
            Array.Sort(s1, startIdx + increment, 0, null);
            TestLibrary.TestFramework.LogError("109", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: The start index and length do not specify a valid range in array.  ");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(-55, false, 0, 10);
                s1[i] = value;
            }
            int startIdx = GetInt(1, length - 1);
            int count = length;
            Array.Sort(s1, startIdx, count, null);
            TestLibrary.TestFramework.LogError("111", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest8: Elements in array do not implement the IComparable interface ");

        try
        {
            E[] a1 = new E[4] { new E(), new E(), new E(), new E() };
            IComparer d = null;
            Array.Sort(a1, 0, 4, d);
            TestLibrary.TestFramework.LogError("115", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("116", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArraySort5 test = new ArraySort5();

        TestLibrary.TestFramework.BeginTestCase("ArraySort5");

        if (test.RunTests())
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

    class A : IComparer
    {

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return ((int)x).CompareTo((int)y);
        }
        #endregion
    }
    class B : IComparer
    {

        #region IComparer Members

        public int Compare(object x, object y)
        {
            if (((char)x).CompareTo((char)y) > 0)
                return -1;
            else
            {
                if (x == y)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }
        #endregion
    }

    class C : IComparer
    {
        protected int c_value;
        public C(int a)
        {
            this.c_value = a;
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return (x as C).c_value.CompareTo((y as C).c_value);
        }
        #endregion
    }

    class D : IComparer
    {
        #region IComparer Members

        public int Compare(object x, object y)
        {
            return 0;
        }
        #endregion
    }

    class E
    {
        public E() { }
    }

    class F : IComparer
    {
        public int Compare(object x, object y)
        {
            int a = (int)x;
            int b = (int)y;
            return a > b ? 1 : (-1);
        }
    }
    #region Help method for geting test data

    private Int32 GetInt(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }

    #endregion
}
