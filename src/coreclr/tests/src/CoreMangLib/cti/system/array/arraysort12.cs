using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Array.Sort<Tkey,Tvalue>(Tkey[],Tvalue[],IComparer)
/// </summary>
public class ArraySort12
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
        //Bug 385712: Won’t fix
        //retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Sort a string array using customizes comparer ");

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
            IComparer<string> a = new A<string>();
            Array.Sort<string, int>(s1, i1, a);
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
            int length = GetInt(1, Byte.MaxValue);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetInt32();
                i1[i] = value;
                i2[i] = value;
            }
            IComparer<int> a1 = new A<int>();
            Array.Sort<int, int>(i1, i2, a1);
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
            IComparer<char> a2 = new A<char>();
            Array.Sort<char, char>(c1, c2, a2);
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
            C c5 = new C(0);
            C c6 = new C(-100);
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            int[] i2 = new int[6] { 1, 2, 3, 4, 5, 6 };
            C[] c = new C[6] { c1, c2, c3, c4, c5, c6 };
            IComparer<C> b1 = new B<C>();
            Array.Sort<C, int>(c, i1, b1);
            for (int i = 0; i < 6; i++)
            {
                if (i1[i] != i2[i])
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Sort a customized array and the comparer argument is null  ");

        try
        {
            C c1 = new C(100);
            C c2 = new C(16);
            C c3 = new C(11);
            C c4 = new C(9);
            C c5 = new C(0);
            C c6 = new C(-100);
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            int[] i2 = new int[6] { 6, 5, 4, 3, 2, 1 };
            C[] c = new C[6] { c1, c2, c3, c4, c5, c6 };
            IComparer<C> b1 = null;
            Array.Sort<C, int>(c, i1, b1);
            for (int i = 0; i < 6; i++)
            {
                if (i1[i] != i2[i])
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: The length of items array is greater than the length of keys array");

        try
        {
            int[] i1 = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int[] i2 = { 6, 5, 4, 3, 2, 1, 7, 8, 9 };
            C[] c = { new C(100), new C(16), new C(11), new C(9), new C(0), new C(-100)};
            IComparer<C> b1 = null;
            Array.Sort<C, int>(c, i1, b1);
            for (int i = 0; i < i1.Length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("011", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The first argument is null reference ");

        try
        {
            string[] s1 = null;
            int[] i1 = { 1, 2, 3, 4, 5 };
            IComparer<string> a = new A<string>();
            Array.Sort<string, int>(s1, i1, a);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2:The length of items array is less than the length of keys array ");

        try
        {
            int length_array = GetInt(2, Int16.MaxValue);
            int length_value = GetInt(1, length_array-1);
            string[] s1 = new string[length_array];
            int[] i1 = new int[length_value];
            for (int i = 0; i < length_array; i++)
            {
                string value = TestLibrary.Generator.GetString(false, 0, 10);
                s1[i] = value;
            }
            for (int i = 0; i < length_value; i++)
            {
                int value = TestLibrary.Generator.GetInt32();
                i1[i] = value;
            }
            IComparer<string> a = new A<string>();
            Array.Sort<string, int>(s1, i1, a);
            TestLibrary.TestFramework.LogError("103", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The keys array does not implement the IComparable interface ");

        try
        {
            D d1 = new D();
            D d2 = new D();
            D d3 = new D();
            D d4 = new D();
            int[] i2 = { 1, 2, 3, 4 };
            D[] d = new D[4] { d1, d2, d3, d4 };
            Array.Sort<D, int>(d, i2, null);
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

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: The implementation of comparison caused an error during the sort");

        try
        {
            string[] s1 = new string[7]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Boy",
                "Tom",
                "Allin"};
            int[] i2 = { 1, 2, 3, 4, 6, 7, 88 };
            IComparer<string> a5 = new E<string>();
            Array.Sort<string, int>(s1, i2, a5);
            TestLibrary.TestFramework.LogError("105", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
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
        ArraySort12 test = new ArraySort12();

        TestLibrary.TestFramework.BeginTestCase("ArraySort12");

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

    class A<T> : IComparer<T> where T : IComparable
    {

        #region IComparer<T> Members

        int IComparer<T>.Compare(T x, T y)
        {
            if (typeof(T) == typeof(char))
            {
                return -(x.CompareTo(y));
            }
            return x.CompareTo(y);
        }

        #endregion
    }
    class B<T> : IComparer<T> where T : IComparable
    {

        #region IComparer<T> Members

        int IComparer<T>.Compare(T x, T y)
        {
            return -(x.CompareTo(y));
        }

        #endregion
    }

    class C : IComparable
    {
        protected int c_value;
        public C(int a)
        {
            this.c_value = a;
        }

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            return this.c_value.CompareTo((obj as C).c_value);
        }

        #endregion
    }


    class D
    {
        public D()
        {
        }
    }
    class E<T> : IComparer<T> where T : IComparable
    {
        #region IComparer<T> Members

        int IComparer<T>.Compare(T x, T y)
        {
            if (x.CompareTo(x) == 0)
            {
                return -1;
            }
            return 1;
        }

        #endregion
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
                return minValue + TestLibrary.Generator.GetInt32() % (maxValue - minValue);
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
    #endregion