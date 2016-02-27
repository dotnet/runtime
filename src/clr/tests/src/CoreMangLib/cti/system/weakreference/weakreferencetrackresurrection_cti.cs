// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;

public struct TestStruct
{
    public int IntValue;
    public string StringValue;
}

public class DisposableClass : IDisposable
{
    public void Dispose()
    {
    }
}

/// <summary>
/// TrackResurrection
/// </summary>

[SecuritySafeCritical]
public class WeakReferenceTrackResurrection
{
    #region Private Fields
    private const int c_MIN_STRING_LENGTH = 8;
    private const int c_MAX_STRING_LENGTH = 1024;
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: TrackResurrection will return correct value when construct the WeakReference for an object instance");

        try
        {
            Object obj = new Object();
            WeakReference reference = new WeakReference(obj, true);

            if (!reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("001.1", "TrackResurrection returns false when construct the WeakReference for an object instance");
                retVal = false;
            }

            reference = new WeakReference(obj, false);

            if (reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("001.2", "TrackResurrection returns true when construct the WeakReference for an object instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: TrackResurrection will return correct value when construct the WeakReference for an value type instance");

        try
        {
            int randValue = TestLibrary.Generator.GetInt32(-55);
            WeakReference reference = new WeakReference(randValue, true);

            if (!reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("002.1", "TrackResurrection returns false when construct the WeakReference for an value type instance");
                retVal = false;
            }

            reference = new WeakReference(randValue, false);

            if (reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("002.2", "TrackResurrection returns true when construct the WeakReference for an value type instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: TrackResurrection will return correct value when construct the WeakReference for an reference type instance");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            WeakReference reference = new WeakReference(randValue, true);

            if (!reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("003.1", "TrackResurrection returns false when construct the WeakReference for an reference type instance");
                retVal = false;
            }

            reference = new WeakReference(randValue, false);

            if (reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("003.2", "TrackResurrection returns true when construct the WeakReference for an reference type instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: TrackResurrection will return correct value when construct the WeakReference for null reference");

        try
        {
            WeakReference reference = new WeakReference(null, true);

            if (!reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("004.1", "TrackResurrection returns false when construct the WeakReference for null reference");
                retVal = false;
            }

            reference = new WeakReference(null, false);

            if (reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("004.2", "TrackResurrection returns true when construct the WeakReference for null reference");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    
    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: TrackResurrection will return correct value when construct the WeakReference for a disposable instance");

        try
        {
            DisposableClass dc = new DisposableClass();
            Object desiredValue = dc;
            WeakReference reference = new WeakReference(desiredValue, true);

            if (!reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("005.1", "TrackResurrection returns false when construct the WeakReference for a disposable instance");
                retVal = false;
            }

            reference = new WeakReference(desiredValue, false);

            if (reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("005.2", "TrackResurrection returns true when construct the WeakReference for a disposable instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: TrackResurrection will return correct value when construct the WeakReference for a IntPtr instance");

        try
        {
            Object desiredValue = IntPtr.Zero;
            WeakReference reference = new WeakReference(desiredValue, true);

            if (!reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("006.1", "TrackResurrection returns false when construct the WeakReference for a IntPtr instance");
                retVal = false;
            }

            desiredValue = new IntPtr(TestLibrary.Generator.GetInt32(-55));
            reference = new WeakReference(desiredValue, false);

            if (reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("006.2", "TrackResurrection returns true when construct the WeakReference for a IntPtr instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: TrackResurrection will return correct value when construct the WeakReference for a struct instance");

        try
        {
            TestStruct ts = new TestStruct();
            ts.IntValue = TestLibrary.Generator.GetInt32(-55);
            ts.StringValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Object desiredValue = ts;
            WeakReference reference = new WeakReference(desiredValue, true);

            if (!reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("007.1", "TrackResurrection returns false when construct the WeakReference for a struct instance");
                retVal = false;
            }

            reference = new WeakReference(desiredValue, false);

            if (reference.TrackResurrection)
            {
                TestLibrary.TestFramework.LogError("007.2", "TrackResurrection returns true when construct the WeakReference for a struct instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        WeakReferenceTrackResurrection test = new WeakReferenceTrackResurrection();

        TestLibrary.TestFramework.BeginTestCase("WeakReferenceTrackResurrection");

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
