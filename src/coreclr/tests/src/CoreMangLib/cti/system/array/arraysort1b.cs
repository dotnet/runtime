using System;

/// <summary>
/// System.Array.Sort(System.Array)
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
            string[] s2 = new string[]{"Allin",
                "Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom"};
            Array.Sort(s1);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using default comparer ");

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
            Array.Sort(i1);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array using default comparer ");

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
            Array.Sort(c1);
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
            Array.Sort(s1);
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is null ");

        try
        {
            string[] s1 = null;
            Array.Sort(s1);
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
            Array.Sort(i1);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: One or more elements in array do not implement the IComparable interface ");

        try
        {
            A[] a1 = new A[4] { new A(), new A(), new A(), new A() };
            Array.Sort(a1);
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

    class A
    {
        public A() { }
    }
}
