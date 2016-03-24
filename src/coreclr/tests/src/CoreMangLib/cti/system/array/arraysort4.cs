// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Array.Sort(System.Array,System.Collections.IComparer)
/// </summary>
public class ArraySort4
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
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Sort a string array using string comparer ");

        try
        {
            string[] s1 = new string[7]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Boy",
                "Tom",
                "Allin"};
            IComparer a = new A();
            Array.Sort(s1, a);
            string[] s2 = new string[7]{"Allin",
                "Boy",
                "Jack",
                "Mary",
                "Mike",
                "Peter",            
                "Tom"};
            for (int i = 0; i < 7; i++)
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using reverse comparer ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            TestLibrary.TestFramework.LogInformation("Using random length: " + length);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetByte(-55);
                i1[i] = value;
                i2[i] = value;
            }
            IComparer b = new B();
            Array.Sort(i1, b);
            for (int i = 0; i < length - 1; i++)  //manually quich sort
            {
                for (int j = i + 1; j < length; j++)
                {
                    if (i2[i] < i2[j])
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
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array using default comparer ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            char[] i1 = new char[length];
            char[] i2 = new char[length];
            for (int i = 0; i < length; i++)
            {
                char value = TestLibrary.Generator.GetChar(-55);
                i1[i] = value;
                i2[i] = value;
            }
            IComparer c = null;
            Array.Sort(i1, c);
            for (int i = 0; i < length - 1; i++)  //manually quich sort
            {
                for (int j = i + 1; j < length; j++)
                {
                    if (i2[i] > i2[j])
                    {
                        char temp = i2[i];
                        i2[i] = i2[j];
                        i2[j] = temp;
                    }
                }
            }
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort an array which has same elements ");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            string[] s2 = new string[length];
            string value = TestLibrary.Generator.GetString(-55, false, 0, 10);
            for (int i = 0; i < length; i++)
            {
                s1[i] = value;
                s2[i] = value;
            }
            IComparer c = null;
            Array.Sort(s1, c);
            for (int i = 0; i < length; i++)
            {
                if (s1[i] != s2[i])
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Sort a string array including null reference and using customized comparer interface");

        try
        {
            string[] s1 = new string[9]{"Jack",
                "Mary",
                "Mike",
                 null,
                "Peter",
                "Boy",
                "Tom",
                null,
                "Allin"};
            IComparer d = new D();
            Array.Sort(s1, d);
            string[] s2 = new string[9]{"Allin",
                "Boy",
                "Jack",
                "Mary",
                "Mike",
                "Peter",            
                "Tom",
                 null,
                 null};
            for (int i = 0; i < 7; i++)
            {
                if (s1[i] != s2[i])
                {
                    TestLibrary.TestFramework.LogError("009", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6:The implementation of comparer caused an error during the sort");

        try
        {
            int[] i1 = new int[9] { 2, 34, 56, 87, 34, 23, 209, 34, 87 };
            IComparer f = new F();
            Array.Sort(i1, f);

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
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is null ");

        try
        {
            string[] s1 = null;
            IComparer a = new A();
            Array.Sort(s1, a);
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
            IComparer a = new A();
            Array.Sort(i1, a);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: Elements in array do not implement the IComparable interface ");

        try
        {
            E[] a1 = new E[4] { new E(), new E(), new E(), new E() };
            IComparer d = null;
            Array.Sort(a1,d);
            TestLibrary.TestFramework.LogError("105", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
    #endregion

    public static int Main()
    {
        ArraySort4 test = new ArraySort4();

        TestLibrary.TestFramework.BeginTestCase("ArraySort4");

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
            return ((string)x).CompareTo((string)y);
        }
        #endregion
    }

    class B : IComparer
    {

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return (-((int)x).CompareTo((int)y));
        }
        #endregion
    }

    class D : IComparer
    {

        #region IComparer Members

        public int Compare(object x, object y)
        {
            if (x == null)
            {
                return 1;
            }
            if (y == null)
                return -1;

            return ((string)x).CompareTo((string)y);
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
}
