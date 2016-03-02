// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Array.Sort(System.Array,System.Array,System.Collections.IComparer)
/// </summary>
public class ArrayIndexOf1
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

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
            int[] i1 = new int[6] { 24, 30, 28, 26, 32, 23 };
            string[] s2 = new string[6]{"Allin",
                "Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom"};
            int[] i2 = new int[6] { 23, 24, 30, 28, 26, 32 };
            Array.Sort(s1, i1, null);
            for (int i = 0; i < 6; i++)
            {
                if (s1[i] != s2[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                    retVal = false;
                }
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("002", "The result is not the value as expected");
                    retVal = false;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
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
                int value = TestLibrary.Generator.GetInt32(-55);
                i1[i] = value;
                i2[i] = value;
            }
            IComparer a1 = new A();
            Array.Sort(i1, i2, a1);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("004", "The result is not the value as expected");
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
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array using reverse comparer ");

        try
        {
            char[] c1 = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10] { 'j', 'i', 'h', 'g', 'f', 'e', 'd', 'c', 'b', 'a' };
            char[] d1 = new char[10];
            char[] d2 = new char[10];
            c1.CopyTo(d2, 0);
            c2.CopyTo(d1, 0);
            IComparer b = new B();
            Array.Sort(c1, c2, b);
            for (int i = 0; i < 10; i++)
            {
                if (c1[i] != d1[i])
                {
                    TestLibrary.TestFramework.LogError("006", "The result is not the value as expected");
                    retVal = false;
                }
                if (c2[i] != d2[i])
                {
                    TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
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
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort a customized array  ");

        try
        {
            C c1 = new C(100);
            C c2 = new C(16);
            C c3 = new C(11);
            C c4 = new C(9);
            C c5 = new C(7);
            C c6 = new C(2);
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            int[] i2 = new int[6] { 6, 5, 4, 3, 2, 1 };
            Array myarray = Array.CreateInstance(typeof(C), 6);
            myarray.SetValue(c1, 0);
            myarray.SetValue(c2, 1);
            myarray.SetValue(c3, 2);
            myarray.SetValue(c4, 3);
            myarray.SetValue(c5, 4);
            myarray.SetValue(c6, 5);
            Array.Sort(myarray, i1, (IComparer)myarray.GetValue(0));
            for (int i = 0; i < 6; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The first argument is null reference ");

        try
        {
            string[] s1 = null;
            int[] i1 = { 1, 2, 3, 4, 5 };
            Array.Sort(s1, i1, null);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The keys array is not one dimension ");

        try
        {
            int[,] i1 = new int[2, 3] { { 2, 3, 5 }, { 34, 56, 77 } };
            int[] i2 = { 1, 2, 3, 4, 5 };
            Array.Sort(i1, i2, null);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The items array is not one dimension ");

        try
        {
            int[,] i1 = new int[2, 3] { { 2, 3, 5 }, { 34, 56, 77 } };
            int[] i2 = { 1, 2, 3, 4, 5 };
            Array.Sort(i2, i1, null);
            TestLibrary.TestFramework.LogError("105", "The RankException is not throw as expected ");
            retVal = false;
        }
        catch (RankException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: The length of items array is not equal to the length of keys array");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom",
                "Allin"};
            int[] i1 = new int[5] { 24, 30, 28, 26, 32 };
            Array.Sort(s1, i1, null);
            TestLibrary.TestFramework.LogError("107", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: The Icomparer is null and keys array does not implement the IComparer interface ");

        try
        {
            D d1 = new D();
            D d2 = new D();
            D d3 = new D();
            D d4 = new D();
            int[] i2 = { 1, 2, 3, 4 };
            D[] d = new D[4] { d1, d2, d3, d4 };
            Array.Sort(d, i2, null);
            TestLibrary.TestFramework.LogError("109", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArrayIndexOf1 test = new ArrayIndexOf1();

        TestLibrary.TestFramework.BeginTestCase("ArrayIndexOf1");

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
}
