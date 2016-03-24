// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.Runtime.InteropServices;

[SecuritySafeCritical]
/// <summary>
/// Target
/// </summary>
public class GCHandleTarget
{
    #region Private Fields
    private const int c_SIZE_OF_ARRAY = 256;
    #endregion

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Target should return correct object value passed to handle for blittable types");

        try
        {
            retVal = VerificationHelper(TestLibrary.Generator.GetByte(-55), "001.1") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetDouble(-55), "001.2") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetInt16(-55), "001.3") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetInt32(-55), "001.4") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetInt64(-55), "001.5") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetSingle(-55), "001.6") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Target should return correct object value passed to handle for blittable types");

        try
        {
            retVal = VerificationHelper(TestLibrary.Generator.GetChar(-55), "002.1") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetString(-55, false, 1, c_SIZE_OF_ARRAY), "002.2") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Target should return correct object value passed to handle for blittable types");

        try
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            double[] doubles = new double[c_SIZE_OF_ARRAY];
            short[] shorts = new short[c_SIZE_OF_ARRAY];
            int[] ints = new int[c_SIZE_OF_ARRAY];
            long[] longs = new long[c_SIZE_OF_ARRAY];
            float[] floats = new float[c_SIZE_OF_ARRAY];

            TestLibrary.Generator.GetBytes(-55, bytes);
            for (int i = 0; i < doubles.Length; ++i)
            {
                doubles[i] = TestLibrary.Generator.GetDouble(-55);
            }
            for (int i = 0; i < shorts.Length; ++i)
            {
                shorts[i] = TestLibrary.Generator.GetInt16(-55);
            }
            for (int i = 0; i < ints.Length; ++i)
            {
                ints[i] = TestLibrary.Generator.GetInt32(-55);
            }
            for (int i = 0; i < longs.Length; ++i)
            {
                longs[i] = TestLibrary.Generator.GetInt64(-55);
            }
            for (int i = 0; i < floats.Length; ++i)
            {
                floats[i] = TestLibrary.Generator.GetSingle(-55);
            }

            retVal = VerificationHelper<byte>(bytes, "003.1") && retVal;
            retVal = VerificationHelper<double>(doubles, "003.2") && retVal;
            retVal = VerificationHelper<short>(shorts, "003.3") && retVal;
            retVal = VerificationHelper<int>(ints, "003.4") && retVal;
            retVal = VerificationHelper<long>(longs, "003.5") && retVal;
            retVal = VerificationHelper<float>(floats, "003.6") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidOperationException should be thrown when The handle was freed.");

        try
        {
            GCHandle handle = GCHandle.Alloc(TestLibrary.Generator.GetInt32(-55));
            handle.Free();

            object target = handle.Target;
            TestLibrary.TestFramework.LogError("101.1", "InvalidOperationException is not thrown when The handle was freed.");
            retVal = false;
        }
        catch (InvalidOperationException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: InvalidOperationException should be thrown when The handle was never initialized.");

        try
        {
            GCHandle handle = (GCHandle)IntPtr.Zero;

            object target = handle.Target;
            TestLibrary.TestFramework.LogError("102.1", "InvalidOperationException is not thrown when The handle was never initialized.");
            retVal = false;
        }
        catch (InvalidOperationException)
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
    #endregion
    #endregion

    public static int Main()
    {
        GCHandleTarget test = new GCHandleTarget();

        TestLibrary.TestFramework.BeginTestCase("GCHandleTarget");

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

    #region Private Method
    private bool VerificationHelper(object obj, string errorNo)
    {
        bool retVal = true;

        GCHandle handle = GCHandle.Alloc(obj);

        if (handle.Target == null)
        {
            TestLibrary.TestFramework.LogError(errorNo, "Target returns null for valid GCHandle");
            retVal = false;
        }
        else
        {
            if (!obj.Equals(handle.Target))
            {
                TestLibrary.TestFramework.LogError(errorNo, "Target returns from valid GCHandle is wrong");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] obj = " + obj + ", handle.Target = " + handle.Target);
                retVal = false;
            }
        }
        handle.Free();

        return retVal;
    }

    private bool VerificationHelper<T>(T[] obj, string errorNo)
    {
        bool retVal = true;

        GCHandle handle = GCHandle.Alloc(obj);

        T[] actual = handle.Target as T[];
        if (actual == null)
        {
            TestLibrary.TestFramework.LogError(errorNo, "Target returns null for valid GCHandle");
            retVal = false;
        }
        else if (actual.Length != obj.Length)
        {
            TestLibrary.TestFramework.LogError(errorNo, "Target returns wrong array for valid GCHandle for object array");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] obj.Length = " + obj.Length + ", actual.Length = " + actual.Length);
            retVal = false;
        }
        else
        {
            for (int i = 0; i < obj.Length; ++i)
            {
                if (!obj[i].Equals(actual[i]))
                {
                    TestLibrary.TestFramework.LogError(errorNo, "Target returns from valid GCHandle is wrong");
                    TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] obj[i] = " + obj[i] + ", actual[i] = " + actual[i] + ", i = " + i);
                    retVal = false;
                }
            }
        }

        handle.Free();

        return retVal;
    }
    #endregion
}
