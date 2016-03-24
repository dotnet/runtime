// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class ObjectEquals2
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
        retVal = PosTest7() && retVal;
        
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare two different object instances");

        try
        {
            Object obj1 = new Object();
            Object obj2 = new Object();

            if (Object.Equals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("001", "Object.Equals returns true when comparing two different object instances");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare two same object references");

        try
        {
            Object obj1 = new Object();
            Object obj2 = obj1;

            if (!Object.Equals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("003", "Object.Equals returns false when comparing two same object references");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare two same value type");

        try
        {
            int i1 = TestLibrary.Generator.GetInt32(-55);
            int i2 = i1;

            Object obj1 = i1;
            Object obj2 = i2;

            if (!Object.Equals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("005", "Object.Equals returns false when comparing two same value type");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Compare two different instance of same value type");

        try
        {
            int i1 = TestLibrary.Generator.GetInt32(-55);
            int i2 = i1;

            // Generate a different value type
            while (i1 == i2)
            {
                i2 = TestLibrary.Generator.GetInt32(-55);
            }

            Object obj1 = i1;
            Object obj2 = i2;

            if (Object.Equals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("007", "Object.Equals returns true when comparing two different instance of same value type");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Compare two different instance of different value type");

        try
        {
            int i1 = TestLibrary.Generator.GetInt32(-55);
            double d1 = TestLibrary.Generator.GetDouble(-55);

            Object obj1 = i1;
            Object obj2 = d1;

            if (Object.Equals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("009", "Object.Equals returns true when comparing two different instance of different value type");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Compare two same instances of different value type");

        try
        {
            int i1 = TestLibrary.Generator.GetInt32(-55);
            double d1 = (double)i1;

            Object obj1 = i1;
            Object obj2 = d1;

            if (Object.Equals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("009", "Object.Equals returns true when comparing two same instances of different value type");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Compare an object with itself");

        try
        {
            Object obj = new Object();

            if (!Object.Equals(obj, obj))
            {
                TestLibrary.TestFramework.LogError("011", "An object does not equal with itself");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Compare two null references");

        try
        {
            if (!Object.Equals(null, null))
            {
                TestLibrary.TestFramework.LogError("101", "Object.Equals return false when compare two null references");
                retVal = false;
            }

            // Check successive call to Equals does not return different result
            if (!Object.Equals(null, null))
            {
                TestLibrary.TestFramework.LogError("102", "Object.Equals return false when compare two null references");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Compare an object instance with null references");

        try
        {
            Object obj1 = new Object();

            if (Object.Equals(obj1, null))
            {
                TestLibrary.TestFramework.LogError("104", "Object.Equals return true when comparing an object instance with null references");
                retVal = false;
            }

            // Check successive call to Equals does not return different result
            if (Object.Equals(obj1, null))
            {
                TestLibrary.TestFramework.LogError("105", "Object.Equals return true when comparing an object instance with null references");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Compare null references with an object instance");

        try
        {
            Object obj1 = new Object();

            if (Object.Equals(null, obj1))
            {
                TestLibrary.TestFramework.LogError("107", "Object.Equals return true when comparing null references with an object instance");
                retVal = false;
            }

            // Check successive call to Equals does not return different result
            if (Object.Equals(null, obj1))
            {
                TestLibrary.TestFramework.LogError("108", "Object.Equals return true when comparing null references with an object instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("109", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Compare a value instance with null reference");

        try
        {
            Object obj1 = TestLibrary.Generator.GetInt32(-55);

            if (Object.Equals(obj1, null))
            {
                TestLibrary.TestFramework.LogError("110", "Object.Equals return true when comparing a value instance with null reference");
                retVal = false;
            }

            // Check successive call to Equals does not return different result
            if (Object.Equals(obj1, null))
            {
                TestLibrary.TestFramework.LogError("111", "Object.Equals return true when comparing a value instance with null reference");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ObjectEquals2 test = new ObjectEquals2();

        TestLibrary.TestFramework.BeginTestCase("ObjectEquals2");

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
