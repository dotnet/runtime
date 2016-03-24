// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using TestLibrary;

public enum EnumInt32 : int
{
    One, Two, Three
}

public enum EnumByte : byte
{
    One, Two, Three
}

class EnumIsDefined
{
    static int Main()
    {
        EnumIsDefined test = new EnumIsDefined();

        TestFramework.BeginTestCase("Enum.IsDefined(enumType,value)");

        if (test.RunTests())
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("FAIL");
            return 0;
        }

    }

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

        TestLibrary.TestFramework.LogInformation("");
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        //Type ctor are now internal, so inheritance of Type class is not allowed on SL
        
        return retVal;
    }



    public bool PosTest1()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest1: use enumType and value that is contained in the enum (1)");

        try
        {
            if (Enum.IsDefined(typeof(EnumInt32), 1))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("001", "Enum.IsDefined(typeof(EnumInt32), 1) returned false");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest2: use enumType and value that is not contained in the enum (5)");

        try
        {
            if (!Enum.IsDefined(typeof(EnumInt32), 5))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("003", "Enum.IsDefined(typeof(EnumInt32), 5) returned true");
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest3: use enumType and value that is contained in the enum (One)");

        try
        {
            if (Enum.IsDefined(typeof(EnumInt32), "One"))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("005", "Enum.IsDefined(typeof(EnumInt32), \"One\") returned false");
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest4: use enumType and value that is not contained in the enum (ONE)");

        try
        {
            if (!Enum.IsDefined(typeof(EnumInt32), "ONE"))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("007", "Enum.IsDefined(typeof(EnumInt32), \"ONE\") returned true");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest5: use enumType and value that is contained in the enum (Enum1.One)");

        try
        {
            if (Enum.IsDefined(typeof(EnumInt32), EnumInt32.One))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("009", "Enum.IsDefined(typeof(EnumInt32), EnumInt32.One) returned false");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Try different underlying type, use enumType and value that is contained in the enum (One)");

        try
        {
            if (Enum.IsDefined(typeof(EnumByte), "One"))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("011", "Enum.IsDefined(typeof(EnumByte), \"One\") returned false");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Try different underlying type, use enumType and value that is not contained in the enum (Five)");

        try
        {
            if (!Enum.IsDefined(typeof(EnumByte), "Five"))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("013", "Enum.IsDefined(typeof(EnumByte), \"Five\") returned true");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest8: Try different underlying type, use enumType and value that is contained in the enum ((byte)1)");

        try
        {
            if (Enum.IsDefined(typeof(EnumByte), (byte)1))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.TestFramework.LogError("015", "Enum.IsDefined(typeof(EnumByte), (byte)1) returned false");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
        }

        return retVal;
    }
    
    public bool NegTest1()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("NegTest1: enumType is null");

        try
        {
            Enum.IsDefined(null,1);
            TestLibrary.TestFramework.LogError("017", "Did not catch expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // caught expected exception
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("NegTest2: value is null");

        try
        {
            Enum.IsDefined(typeof(EnumInt32), null);
            TestLibrary.TestFramework.LogError("019", "Did not catch expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // caught expected exception
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("NegTest3: enumType is not an Enum");

        try
        {
            Enum.IsDefined(typeof(string), EnumInt32.One);
            TestLibrary.TestFramework.LogError("021", "Did not catch expected ArgumentException");
        }
        catch (ArgumentException)
        {
            // caught expected exception
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("NegTest4: type of value is not an enumType");

        try
        {
            Enum.IsDefined(typeof(EnumInt32), new Object());
            TestLibrary.TestFramework.LogError("023", "Did not catch expected ArgumentException");
        }
        catch (InvalidOperationException)
        {
            // caught expected exception
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception: " + e);
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("NegTest5: type of value is not an underlying type of enumType");

        try
        {
            Enum.IsDefined(typeof(EnumInt32), (byte)1);
            TestLibrary.TestFramework.LogError("025", "Expected ArgumentException is not thrown");
        }
        catch (ArgumentException)
        {
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception: " + e);
        }

        return retVal;
    }
}
