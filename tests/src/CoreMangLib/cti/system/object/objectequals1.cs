// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class ObjectEquals1
{
    #region Private Constaints
    private const int c_MIN_STRING_LENGTH = 0;
    private const int c_MAX_STRING_LENGTH = 256;
    #endregion

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
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
                
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare two empty object instances");

        try
        {
            Object obj1 = new Object();
            Object obj2 = new Object();

            if (obj1.Equals(obj2))
            {
                TestLibrary.TestFramework.LogError("001", "Two empty object instances are equal");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Boxed value type");

        try
        {
            int i = TestLibrary.Generator.GetInt32(-55);
            Object obj = i; 

            if (!obj.Equals(i))
            {
                TestLibrary.TestFramework.LogError("003", "Boxed type is not equal with the underlying value type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare two object reference");

        try
        {
            Object obj1 = new Object();
            Object obj2 = obj1;

            if (!obj1.Equals(obj2))
            {
                TestLibrary.TestFramework.LogError("005", "Compare two object reference failed");
                retVal = false;
            }

            // Also make sure this case is true
            if (!obj2.Equals(obj1))
            {
                TestLibrary.TestFramework.LogError("006", "Compare two object reference failed");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Compare an object with itself");

        try
        {
            Object obj = new Object();

            if (!obj.Equals(obj))
            {
                TestLibrary.TestFramework.LogError("008", "An object does not equal with itself");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Compare two NaN instances");

        try
        {
            Object obj1 = Double.NaN;
            Object obj2 = Double.NaN;

            if (!obj1.Equals(Double.NaN))
            {
                TestLibrary.TestFramework.LogError("010", "An object initialized with Double.NaN does not equal with Double.NaN");
                retVal = false;
            }

            if (!obj1.Equals(obj2))
            {
                TestLibrary.TestFramework.LogError("011", "Two objects initialized with Double.NaN do not equal");
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

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Successive calls to Equals return the same value for reference types");

        try
        {
            Object obj1 = new Object();
            Object obj2 = new Object();

            if (obj1.Equals(obj2) || obj1.Equals(obj2) )
            {
                TestLibrary.TestFramework.LogError("013", "Successive calls to Equals do not return the same value");
                retVal = false;
            }

            Object obj3 = TestLibrary.Generator.GetString(-55, 
                false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            // use !! instead of || to make sure the statement obj1.Equals(obj3) is evaluated twice
            if ( !( !obj1.Equals(obj3) && !obj1.Equals(obj3)) )
            {
                TestLibrary.TestFramework.LogError("014", "Successive calls to Equals do not return the same value");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Successive calls to Equals return the same value for value types");

        try
        {
            int i = TestLibrary.Generator.GetInt32(-55);
            Object obj = i;

            if (!obj.Equals(i) && obj.Equals(i))
            {
                TestLibrary.TestFramework.LogError("016", "Successive calls to Equals for value types do not return the same value");
                retVal = false;
            }

            i++;
            // use !! instead of || to make sure the statement obj.Equals(i) is evaluated twice
            if (!(!obj.Equals(i) && !obj.Equals(i)))
            {
                TestLibrary.TestFramework.LogError("017", "Successive calls to Equals for value types do not return the same value");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest8: Upcast an type derivered from Object and compare with it");

        try
        {
            String str = TestLibrary.Generator.GetString(-55, 
                false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Object obj = str;

            if (!obj.Equals(str))
            {
                TestLibrary.TestFramework.LogError("019", "Failed to compare with an upcast type");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest9: Upcast an type derivered from Object and compare with a different reference");

        try
        {
            String str1 = TestLibrary.Generator.GetString(-55, 
                false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            String str2 = TestLibrary.Generator.GetString(-55, 
                false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            // Make sure the two strings are different
            if (str1.Length == str2.Length)
            {
                str1 += TestLibrary.Generator.GetCharLetter(-55);
            }

            Object obj1 = str1;
            Object obj2 = str2;

            if (obj1.Equals(obj2))
            {
                TestLibrary.TestFramework.LogError("021", "Upcast an type derivered from Object and compare with a different reference returns true");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest10: Compare two different value type");

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

            if (obj1.Equals(obj2))
            {
                TestLibrary.TestFramework.LogError("023", "Compare two different value type returns true");
                retVal = false;
            }

            // Compare different type of value type
            double d1 = TestLibrary.Generator.GetDouble(-55);
            Object obj3 = d1;

            if (obj1.Equals(d1))
            {
                TestLibrary.TestFramework.LogError("024", "Compare two different value type returns true");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("025", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest11: Compare an object instance with an array which contains the instance");

        try
        {
            Object obj1 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Object[] array = new Object[] {
                obj1
            };
            Object obj2 = array; // Upcast the array instance to an Object

            if (obj1.Equals(obj2))
            {
                TestLibrary.TestFramework.LogError("026", "Compare an object instance with an array which contains the instance returns true");
                retVal = false;
            }

            if (obj2.Equals(obj1))
            {
                TestLibrary.TestFramework.LogError("027", "Compare an object instance with an array which contains the instance returns true");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest12: Compare an value instance with an array which contains the instance");

        try
        {
            int i1 = TestLibrary.Generator.GetInt32(-55);
            int[] array = new int[] { i1 };
            Object obj1 = i1;
            Object obj2 = array; // Upcast the array instance to an Object

            if (obj1.Equals(obj2))
            {
                TestLibrary.TestFramework.LogError("029", "Compare an value instance with an array which contains the instance returns true");
                retVal = false;
            }

            if (obj2.Equals(obj1))
            {
                TestLibrary.TestFramework.LogError("030", "Compare an value instance with an array which contains the instance returns true");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("031", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Compare an object instance with null reference");

        try
        {
            Object obj = new Object();

            if (obj.Equals(null))
            {
                TestLibrary.TestFramework.LogError("101", "an object instance is equal with null reference");
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
    #endregion
    #endregion

    public static int Main()
    {
        ObjectEquals1 test = new ObjectEquals1();

        TestLibrary.TestFramework.BeginTestCase("ObjectEquals1");

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
