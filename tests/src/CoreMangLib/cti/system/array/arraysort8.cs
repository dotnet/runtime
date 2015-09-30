using System;
using System.Collections.Generic;

/// <summary>
/// System.Array.Sort<T>(T[],System.Comparison<T>)
/// </summary>
public class ArraySort8
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
        //Bug 385712: Won’t fix
        //retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1:Sort a string array including null reference and using customized comparison delegate ");

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
            string[] s2 = new string[9]{"Allin",
                "Boy",
                "Jack",
                "Mary",
                "Mike",
                "Peter",            
                "Tom",
                 null,
                 null};
            Array.Sort<string>(s1, this.M1_compare);
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
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using reverse comparison delegate ");

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
            Array.Sort<int>(i1, ArraySort8.M2_compare);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort an array which has same elements ");

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
            Array.Sort<string>(s1, this.M1_compare);
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort customized type array using customized comparison");

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
                    if (c_result[i].value > c_result[i + 1].value)
                    {
                        temp = c_result[i];
                        c_result[i] = c_result[i + 1];
                        c_result[i + 1] = temp;
                    }
                }
            }
            Array.Sort<C>(c_array, ArraySort8.M3_compare);
            for (int i = 0; i < 5; i++)
            {
                if (c_result[i].value != c_array[i].value)
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
            Array.Sort<string>(s1, this.M1_compare);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The comparison is null");

        try
        {
            string[] s1 = new string[7]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Boy",
                "Tom",
                "Allin"};
            Array.Sort<string>(s1, (Comparison<string>)null);
            TestLibrary.TestFramework.LogError("103", "The ArgumentNullException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentNullException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The implementation of comparison caused an error during the sort");

        try
        {
            string[] s1 = new string[7]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Boy",
                "Tom",
                "Allin"};
            Array.Sort<string>(s1, this.M4_compare);
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
        ArraySort8 test = new ArraySort8();

        TestLibrary.TestFramework.BeginTestCase("ArraySort8");

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

    private int M1_compare(string s1, string s2)
    {
        if (s1 == null)
        {
            if (s2 == null)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        else
        {
            if (s2 == null)
            {
                return -1;
            }
        }
        return s1.CompareTo(s2);
    }

    private static int M2_compare(int s1, int s2)
    {
        return -(s1.CompareTo(s2));
    }

    private static int M3_compare(C s1, C s2)
    {
        if (s1 == null)
        {
            if (s2 == null)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            if (s2 == null)
            {
                return 1;
            }
        }
        return s1.value.CompareTo(s2.value);
    }

    private int M4_compare(string s1, string s2)
    {
        if (s1.CompareTo(s1) == 0)
        {
            return -1;
        }
        return s1.CompareTo(s2);
    }

    class C
    {
        protected int c_value;
        public C(int a)
        {
            this.c_value = a;
        }
        public int value
        {
            get
            {
                return c_value;
            }
        }

    }
}
