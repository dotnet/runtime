// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.LastIndexOf(System.Array,System.Object,System.Int32,System.Int32)
/// </summary>
public class ArrayLastIndexOf1
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

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
        retVal = PosTest7() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the array of many elements which have the same value  ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            int value = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[length];
            for (int i = 0; i < length; i++)
            {
                i1[i] = value;
            }
            for (int i = length - 1; i >= 0; i--) // travel the array
            {
                if (Array.LastIndexOf(i1, value, i, i + 1) != i)
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the empty string  ");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                s1[i] = "";
            }
            for (int i = length - 1; i >= 0; i--) // travel the array
            {
                if (Array.LastIndexOf(s1, "", i, i + 1) != i)
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Test the null element of the string array ");

        try
        {
            int length;
            string value = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            int index1, index2;
            do
            {
                length = TestLibrary.Generator.GetInt16(-55);
            } while (10 > length);
            do
            {
                index1 = this.GetInt32(0, length);
                index2 = this.GetInt32(0, length);
            } while (index1 == index2);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                if (i != index1 && (i != index2))
                {
                    s1[i] = value;
                }
                else
                {
                    s1[i] = null;
                }

            }
            if (index1 < index2)
            {
                if (Array.LastIndexOf(s1, null, length - 1, length) != index2)
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
                    retVal = false;
                }
            }
            else
            {
                if (Array.LastIndexOf(s1, null, length - 1, length) != index1)
                {
                    TestLibrary.TestFramework.LogError("006", "The result is not the value as expected");
                    retVal = false;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest4: Find out no result  ");

        try
        {
            string[] s1 = new string[5]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Joan"};

            if (Array.LastIndexOf(s1, "Jaon", 4, 5) != -1)
            {
                TestLibrary.TestFramework.LogError("008", "The result is not the value as expected");
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

    public bool PosTest5()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest5: Find the second last value because of the startIndex ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Mary",
                "Joan"};

            if (Array.LastIndexOf(s1, "Mary", 3, 4) != 1)
            {
                TestLibrary.TestFramework.LogError("010", "The result is not the value as expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest6: Find out no value because of the count argument ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Mary",
                "Joan"};

            if (Array.LastIndexOf(s1, "Mike", 5, 3) != -1)
            {
                TestLibrary.TestFramework.LogError("012", "The result is not the value as expected");
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

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Find out no value because of different type ");

        try
        {
            int[] i1 = new int[5] { 1, 2, 34, 67, 10 };
            Int16 value = 34;
            int result = Array.LastIndexOf(i1, (object)value, 4, 5);
            if (result != -1)
            {
                TestLibrary.TestFramework.LogError("014", "The result is not the value as expected: Expected(-1) Actual("+result+")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is a null reference ");

        try
        {
            string[] s1 = null;
            int i1 = Array.LastIndexOf(s1, "", 1, 0);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Set the negative startIndex argument");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Mary",
                "Joan"};
            int i1 = Array.LastIndexOf(s1, "", -1, 3);
            TestLibrary.TestFramework.LogError("103", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: Set the  startIndex greater than the max index of the array");

        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            int i2 = Array.LastIndexOf(i1, 56, 6, 3);
            TestLibrary.TestFramework.LogError("105", "The ArgumentOutOfRangeException was not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: Count argument is less than zero");
        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            int i2 = Array.LastIndexOf(i1, 56, 3, -3);
            TestLibrary.TestFramework.LogError("107", "The ArgumentOutOfRangeException was not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: Count argument do not specify a valid section in array");
        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            int i2 = Array.LastIndexOf(i1, 56, 3, 5);
            TestLibrary.TestFramework.LogError("109", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest6: The array is not a one dimension array ");
        try
        {
            int[,] i1 = new int[2, 3] { { 2, 3, 5 }, { 34, 56, 77 } };
            int i2 = Array.LastIndexOf(i1, 3, 1, 0);
            TestLibrary.TestFramework.LogError("111", "The RankException was not thrown as expected");
            retVal = false;
        }
        catch (RankException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArrayLastIndexOf1 test = new ArrayLastIndexOf1();

        TestLibrary.TestFramework.BeginTestCase("ArrayLastIndexOf1");

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
    public Int32 GetInt32(int min, int max)
    {
        return min + TestLibrary.Generator.GetInt32(-55) % (max - min);
    }
}
