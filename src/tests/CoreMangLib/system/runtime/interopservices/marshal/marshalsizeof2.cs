// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Security;
using Xunit;


[SecuritySafeCritical]
public struct TestStruct
{
    public int TestInt;
}


[SecuritySafeCritical]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TestUnicodeStringStruct
{
    public string TestString;
}


[SecuritySafeCritical]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct TestAnsiStringStruct
{
    public string TestString;
}


[SecuritySafeCritical]
public struct TestMultiMemberStruct1
{
    public double TestDouble;
    public int TestInt;
}


[SecuritySafeCritical]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TestMultiMemberStruct2
{
    public int TestInt;
    public string TestString;
}


[SecuritySafeCritical]
public struct TestMultiStructs1
{
    public float TestFloat;
    public TestUnicodeStringStruct TestUnicodeStringStruct;
}


[SecuritySafeCritical]
public struct TestMultiStructs2
{
    public float TestFloat;
    public TestMultiMemberStruct2 TestMultiMemberStruct2;
}


[SecuritySafeCritical]
public enum TestEnum
{
    ENUM_VALUE1,
    ENUM_VALUE2
}

[SecuritySafeCritical]
public struct TestGenericStruct<T>
{
    public T TestVal;
}


[SecuritySafeCritical]
public class MarshalSizeOf2
{

    private int NextHighestMultipleOf(int n, int k)
    {
        return k * ((int)Math.Ceiling(((double)n) / ((double)k)));
    }

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Get size of an instance of struct contains one field");

        try
        {
            Type obj = typeof(TestStruct);
            int expectedSize = 4;

            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("001.1", "Get size of an instance of struct contains one field returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Get size of an instance of struct contains unicode string field");

        try
        {
            Type obj = typeof(TestUnicodeStringStruct);
            int expectedSize = IntPtr.Size;

            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("002.1", "Get size of an instance of struct contains unicode string field returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Get size of an instance of struct contains ansi string field");

        try
        {
            Type obj = typeof(TestAnsiStringStruct);
            int expectedSize = IntPtr.Size;

            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("003.1", "Get size of an instance of struct contains ansi string field returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Get size of an instance of struct contains multiple fields");

        try
        {
            Type obj = typeof(TestMultiMemberStruct1);
            int expectedSize;

            if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
            {
                expectedSize = 16; // sizeof(double) + sizeof(int) + padding
            }
            else
            {
                // The System V ABI for i386 defines double as having 4-byte alignment
                expectedSize = 12; // sizeof(double) + sizeof(int)
            }

            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("004.1", "Get size of an instance of struct contains multiple fields returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Get size of an instance of struct contains value type and reference type fields");

        try
        {
            Type obj = typeof(TestMultiMemberStruct2);
            int expectedSize = NextHighestMultipleOf(IntPtr.Size + Marshal.SizeOf(typeof(int)), TestLibrary.Utilities.Is64 ? 8 : 4); // sizeof(object) + sizeof(int)

            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("005.1", "Get size of an instance of struct contains value type and reference type fields returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Get size of an instance of struct contains nested one field struct");

        try
        {
            Type obj = typeof(TestMultiStructs1);
            int expectedSize = NextHighestMultipleOf(IntPtr.Size + Marshal.SizeOf(typeof(int)), TestLibrary.Utilities.Is64 ? 8 : 4); // sizeof(object) + sizeof(int)

            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("006.1", "Get size of an instance of struct contains nested one field struct returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Get size of an instance of struct contains nested multiple fields struct");

        try
        {
            Type obj = typeof(TestMultiStructs2);
            int expectedSize = NextHighestMultipleOf(Marshal.SizeOf(typeof(TestMultiMemberStruct2)) + Marshal.SizeOf(typeof(float)),
                TestLibrary.Utilities.Is64 ? 8 : 4); // sizeof(int) + sizeof(float) + sizeof(string)
            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("007.1", "Get size of an instance of struct contains nested multiple fields struct returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest8: Get size of an instance of value type");

        try
        {
            Type obj = typeof(int);
            int expectedSize = 4;

            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("008.1", "Get size of an instance of value type returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest9: Get size of an instance of generic struct type");

        try
        {
            Type obj = typeof(TestGenericStruct<int>);
            int expectedSize = 4;
            int actualSize = Marshal.SizeOf(obj);

            if (expectedSize != actualSize)
            {
                TestLibrary.TestFramework.LogError("009.1", "Get size of an instance of generic struct type returns wrong size");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expectedSize = " + expectedSize + ", actualSize = " + actualSize + ", obj = " + obj);
                retVal = false;
            }
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009.0", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when The structure parameter is a null reference.");

        try
        {
            int size = Marshal.SizeOf(null);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when The structure parameter is a null reference.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentException should be thrown when the value is a enum type");

        try
        {
            TestEnum obj = TestEnum.ENUM_VALUE1;
            int size = Marshal.SizeOf(obj);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentException is not thrown when the value is a enum type");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentException should be thrown when the value is a reference type");

        try
        {
            Type obj = typeof(Object);
            int size = Marshal.SizeOf(obj);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentException is not thrown when the value is a reference type");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentException should be thrown when the value is a generic type");

        try
        {
            Type obj = typeof(TestGenericStruct<>);
            int size = Marshal.SizeOf(obj);

            TestLibrary.TestFramework.LogError("104.1", "ArgumentException is not thrown when the value is a generic type");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    [Fact]
    public static int TestEntryPoint()
    {
        MarshalSizeOf2 test = new MarshalSizeOf2();

        TestLibrary.TestFramework.BeginTestCase("MarshalSizeOf2");

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
