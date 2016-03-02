// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Array.Sort (T[]) 
/// </summary>
public class ArraySort6
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Sort a string array");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom",
                "Allin"};
            string[] s2 = new string[]{"Allin",
                "Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom"};
            Array.Sort<string>(s1);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array");

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
            Array.Sort<int>(i1);
            for (int i = 0; i < length - 1; i++)  //manually quich sort
            {
                for (int j = i + 1; j < length; j++)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            char[] c1 = new char[length];
            char[] c2 = new char[length];
            for (int i = 0; i < length; i++)
            {
                char value = TestLibrary.Generator.GetChar(-55);
                c1[i] = value;
                c2[i] = value;
            }
            Array.Sort<char>(c1);
            for (int i = 0; i < length - 1; i++)  //manually quich sort
            {
                for (int j = i + 1; j < length; j++)
                {
                    if (c2[i] > c2[j])
                    {
                        char temp = c2[i];
                        c2[i] = c2[j];
                        c2[j] = temp;
                    }
                }
            }
            for (int i = 0; i < length; i++)
            {
                if (c1[i] != c2[i])
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort a string array including null reference ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                 null,
                "Peter",
                "Tom",
                "Allin"};
            string[] s2 = new string[]{null,
                "Allin",
                "Jack",
                "Mary",
                "Peter",
                "Tom"};
            Array.Sort<string>(s1);
            for (int i = 0; i < 6; i++)
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Sort customized type array");

        try
        {
            C[] c_array = new C[5];
            C[] c_result = new C[5];
            for (int i = 0; i < 5; i++)
            {
                int value = TestLibrary.Generator.GetInt32(-55);
                C c1 = new C(value);
                c_array.SetValue(c1, i);
                c_result.SetValue(c1, i);
            }
            //sort manually
            C temp;
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (c_result[i].c > c_result[i + 1].c)
                    {
                        temp = c_result[i];
                        c_result[i] = c_result[i + 1];
                        c_result[i + 1] = temp;
                    }
                }
            }
            Array.Sort<C>(c_array);
            for (int i = 0; i < 5; i++)
            {
                if (c_result[i].c != c_array[i].c)
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
            Array.Sort<string>(s1);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2:  One or more elements in array do not implement the IComparable interface");

        try
        {
            A<int>[] i1 = new A<int>[5] { new A<int>(7), new A<int>(99), new A<int>(-23), new A<int>(0), new A<int>(345) };
            Array.Sort<A<int>>(i1);
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
    #endregion
    #endregion

    public static int Main()
    {
        ArraySort6 test = new ArraySort6();

        TestLibrary.TestFramework.BeginTestCase("ArraySort6");

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

    class C : IComparable
    {
        protected int c_value;
        public C(int a)
        {
            this.c_value = a;
        }
        public int c
        {
            get
            {
                return c_value;
            }
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (this.c_value <= ((C)obj).c)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        #endregion
    }

    class A<T>
    {
        protected T a_value;
        public A(T a)
        {
            this.a_value = a;
        }
    }
}
