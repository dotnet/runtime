using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System.Array.Sort<T>(T[],System.Collections.Generic.IComparer<T>)
/// </summary>
public class ArraySort7
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
        //Bug 385712: Won’t fix
        //retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Sort a string array using string comparer<string>");

        try
        {
            string[] s1 = new string[7]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Boy",
                "Tom",
                "Allin"};
            IComparer<string> a = new A<string>();
            Array.Sort<string>(s1, a);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using reverse comparer<int>");

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
            IComparer<int> b = new B<int>();
            Array.Sort<int>(i1, b);
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
            IComparer<char> c = null;
            Array.Sort<char>(i1, c);
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort an array which has same elements using default Icomparer ");

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
            IComparer<string> c = null;
            Array.Sort<string>(s1, c);
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Sort a string array including null reference and using customized comparer<T> interface");

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
            IComparer<string> d = new D<string>();
            Array.Sort<string>(s1, d);
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is null ");

        try
        {
            string[] s1 = null;
            IComparer<string> a = new A<string>();
            Array.Sort<string>(s1, a);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Elements in array do not implement the IComparable<T> interface ");

        try
        {
            E<int>[] a1 = new E<int>[4] { new E<int>(), new E<int>(), new E<int>(), new E<int>() };
            IComparer<E<int>> d = null;
            Array.Sort<E<int>>(a1, d);
            TestLibrary.TestFramework.LogError("103", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3:The implementation of comparer<T> caused an error during the sort");

        try
        {
            int[] i1 = new int[9] { 2, 34, 56, 87, 34, 23, 209, 34, 87 };
            F f = new F();
            Array.Sort<int>(i1, f);
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
        ArraySort7 test = new ArraySort7();

        TestLibrary.TestFramework.BeginTestCase("ArraySort7");

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

        #region IComparer Members

        public int Compare(T x, T y)
        {
            return x.CompareTo(y);
        }
        #endregion
    }

    class B<T> : IComparer<T> where T : IComparable
    {

        #region IComparer Members

        public int Compare(T x, T y)
        {
            return (-(x).CompareTo(y));
        }
        #endregion
    }

    class D<T> : IComparer<T> where T : IComparable
    {

        #region IComparer Members

        public int Compare(T x, T y)
        {
            if (x == null)
            {
                return 1;
            }
            if (y == null)
            {
                return -1;
            }

            return x.CompareTo(y);
        }
        #endregion
    }
    class E<T>
    {
        public E() { }
    }

    class F : IComparer<int>
    {


        #region IComparer<int> Members

        int IComparer<int>.Compare(int a, int b)
        {
            if (a.CompareTo(a) == 0)
            {
                return -1;
            }

            return a.CompareTo(b);

        }

        #endregion
    }
}
