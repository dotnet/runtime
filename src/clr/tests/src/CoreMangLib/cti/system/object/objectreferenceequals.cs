// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class ObjectReferenceEquals
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare an object with itself");

        try
        {
            Object obj = new Object();
            if (!Object.ReferenceEquals(obj, obj))
            {
                TestLibrary.TestFramework.LogError("001", "ReferenceEquals returns false when comparing an object with itself");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare two object with same reference");

        try
        {
            Object obj1 = new Object();
            Object obj2 = obj1;
            if (!Object.ReferenceEquals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("003", "ReferenceEquals returns false when comparing two object with same reference");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare two value type instance with same value");

        try
        {
            int i1 = TestLibrary.Generator.GetInt32(-55);
            int i2 = i1;

            while (i1 == i2)
            {
                i2 = TestLibrary.Generator.GetInt32(-55);
            }

            Object obj1 = i1;
            Object obj2 = i2;

            if (Object.ReferenceEquals(obj1, obj2))
            {
                TestLibrary.TestFramework.LogError("005", "ReferenceEquals returns true when compare two value type instance with same value");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Compare two null references");

        try
        {
            if (!Object.ReferenceEquals(null, null))
            {
                TestLibrary.TestFramework.LogError("101", "Comparing two null references returns false");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Compare null reference with an object instance");

        try
        {
            Object obj = new Object();
            if (Object.ReferenceEquals(obj, null))
            {
                TestLibrary.TestFramework.LogError("103", "Compare null reference with an object instance returns true");
                retVal = false;
            }

            // make sure this case is true
            if (Object.ReferenceEquals(null, obj))
            {
                TestLibrary.TestFramework.LogError("104", "Compare null reference with an object instance returns true");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("105", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ObjectReferenceEquals test = new ObjectReferenceEquals();

        TestLibrary.TestFramework.BeginTestCase("ObjectReferenceEquals");

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
