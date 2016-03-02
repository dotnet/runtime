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
/// ctor(System.Object)
/// </summary>

[SecuritySafeCritical]
public class WeakReferenceCtor2
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Ctor with valid target reference");

        try
        {
            Object obj = new Object();
            WeakReference reference = new WeakReference(obj);

            if ((reference.TrackResurrection != false) || (!reference.Target.Equals(obj)))
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString() +
                                                                                                                  ", obj = " + obj.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Ctor with IntPtr.Zero reference");

        try
        {
            Object desiredValue = IntPtr.Zero;
            WeakReference reference = new WeakReference(desiredValue);

            if ((reference.TrackResurrection != false) || (!reference.Target.Equals(desiredValue)))
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString() +
                                                                                                                  ", desiredValue = " + desiredValue.ToString());
                retVal = false;
            }

            desiredValue = new IntPtr(TestLibrary.Generator.GetInt32(-55));
            reference = new WeakReference(desiredValue);

            if ((reference.TrackResurrection != false) || (!reference.Target.Equals(desiredValue)))
            {
                TestLibrary.TestFramework.LogError("002.2", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString() +
                                                                                                                  ", desiredValue = " + desiredValue.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Ctor with value type target reference");

        try
        {
            int randValue = TestLibrary.Generator.GetInt32(-55);
            WeakReference reference = new WeakReference(randValue);

            if ((reference.TrackResurrection != false) || (!reference.Target.Equals(randValue)))
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString() +
                                                                                                                  ", randValue = " + randValue.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call Ctor with reference type target reference");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            WeakReference reference = new WeakReference(randValue);

            if ((reference.TrackResurrection != false) || (!reference.Target.Equals(randValue)))
            {
                TestLibrary.TestFramework.LogError("004.1", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString() +
                                                                                                                  ", randValue = " + randValue.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call Ctor with null reference");

        try
        {
            WeakReference reference = new WeakReference(null);

            if ((reference.TrackResurrection != false) || (reference.Target != null))
            {
                TestLibrary.TestFramework.LogError("005.1", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: Call Ctor with IntPtr reference and set trackResurrection to true");

        try
        {
            DisposableClass dc = new DisposableClass();
            Object desiredValue = dc;
            WeakReference reference = new WeakReference(desiredValue);

            if ((reference.TrackResurrection != false) || (!reference.Target.Equals(desiredValue)))
            {
                TestLibrary.TestFramework.LogError("006.1", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString() +
                                                                                                                  ", desiredValue = " + desiredValue.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.7", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Call Ctor with IntPtr reference and set trackResurrection to true");

        try
        {
            TestStruct ts = new TestStruct();
            ts.IntValue = TestLibrary.Generator.GetInt32(-55);
            ts.StringValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Object desiredValue = ts;
            WeakReference reference = new WeakReference(desiredValue);

            if ((reference.TrackResurrection != false) || (!reference.Target.Equals(desiredValue)))
            {
                TestLibrary.TestFramework.LogError("007.1", "Calling Ctor with valid target reference and set trackResurrection to false constructs wrong instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] reference.TrackResurrection = " + reference.TrackResurrection.ToString() +
                                                                                                                  ", reference.Target = " + reference.Target.ToString() +
                                                                                                                  ", desiredValue = " + desiredValue.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        WeakReferenceCtor2 test = new WeakReferenceCtor2();

        TestLibrary.TestFramework.BeginTestCase("WeakReferenceCtor2");

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
