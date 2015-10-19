using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.GetEnumerator
/// </summary>
public class ListGetEnumerator
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The generic type is int");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            List<int>.Enumerator enumerator = listObject.GetEnumerator();
            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                if (enumerator.Current != iArray[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,i is: " + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The generic type is type of string");

        try
        {
            string[] strArray = { "apple", "banana", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            List<string>.Enumerator enumerator = listObject.GetEnumerator();
            int i = 0;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != strArray[i])
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,The i is: " + i);
                    retVal = false;
                }
                i++;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: The generic type is a custom type");

        try
        {
            MyClass myclass1 = new MyClass();
            MyClass myclass2 = new MyClass();
            MyClass myclass3 = new MyClass();
            MyClass[] mc = new MyClass[3] { myclass1, myclass2, myclass3 };
            List<MyClass> listObject = new List<MyClass>(mc);
            List<MyClass>.Enumerator enumerator = listObject.GetEnumerator();
            int i = 0;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != mc[i])
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,The i is: " + i);
                    retVal = false;
                }
                i++;
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: The List is empty");

        try
        {
            List<string> listObject = new List<string>();
            List<string>.Enumerator enumerator = listObject.GetEnumerator();
            if (enumerator.MoveNext())
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
                retVal = false;
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
    #endregion
    #endregion

    public static int Main()
    {
        ListGetEnumerator test = new ListGetEnumerator();

        TestLibrary.TestFramework.BeginTestCase("ListGetEnumerator");

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
}
public class MyClass
{
}