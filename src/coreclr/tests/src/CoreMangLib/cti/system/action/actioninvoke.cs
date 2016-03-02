// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class ActionInvoke
{
    private const int    c_DEFAULT_INT = 234;
    private const string c_DEFAULT_STRING = "The\0default string";
    private       bool g_testCasePassed;

    public static int Main()
    {
        ActionInvoke ai = new ActionInvoke();

        TestLibrary.TestFramework.BeginTestCase("ActionInvoke");

        if (ai.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    public void ActionFunction<T>(T obj)
    {
        g_testCasePassed = true;
        // validate input
        if (typeof(int) == obj.GetType())
        {
            if (c_DEFAULT_INT != (int)(object)obj)
            {
                TestLibrary.TestFramework.LogError("001", "Failed to get correct value: Expected(" + c_DEFAULT_INT + ") Actual(" + obj + ")");
                g_testCasePassed = false;
            }
        }
        else if (typeof(string) == obj.GetType())
        {
            if (!c_DEFAULT_STRING.Equals((string)(object)obj))
            {
                TestLibrary.TestFramework.LogError("002", "Failed to get correct value: Expected(" + c_DEFAULT_STRING + ") Actual(" + obj + ")");
                g_testCasePassed = false;
            }
        }
        else
        {
            TestLibrary.TestFramework.LogError("003", "Failed to get type as expected: " + obj.GetType());
            g_testCasePassed = false;
        }
    }

    public void AsyncCallback(IAsyncResult asyncRes)
    {
    }

    public bool PosTest1()
    {
        bool      retVal = true;
        Action<int> actionDelegate;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Action<int>.Invoke");

        try
        {
            // this variable will be set by the delegate if it is run
            g_testCasePassed = false;

            actionDelegate = new Action<int>( ActionFunction );

            actionDelegate.Invoke(c_DEFAULT_INT);

            retVal = g_testCasePassed;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool      retVal = true;
        Action<string> actionDelegate;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Action<string>.Invoke");

        try
        {
            // this variable will be set by the delegate if it is run
            g_testCasePassed = false;

            actionDelegate = new Action<string>( ActionFunction );

            actionDelegate.Invoke(c_DEFAULT_STRING);

            retVal = g_testCasePassed;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


    public bool NegTest1()
    {
        bool        retVal = true;
        Action<int> actionDelegate;
        IAsyncResult asyncRes;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Action<int>.BeingInvoke EndInvoke");

        try
        {
            // this variable will be set by the delegate if it is run
            g_testCasePassed = false;

            actionDelegate = new Action<int>( ActionFunction );

            asyncRes = actionDelegate.BeginInvoke(c_DEFAULT_INT, new AsyncCallback( AsyncCallback ), null);
            actionDelegate.EndInvoke(asyncRes);

            TestLibrary.TestFramework.LogError("003", "Exception expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool        retVal = true;
        Action<string> actionDelegate;
        IAsyncResult asyncRes;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Action<string>.BeingInvoke EndInvoke");

        try
        {
            // this variable will be set by the delegate if it is run
            g_testCasePassed = false;

            actionDelegate = new Action<string>( ActionFunction );

            asyncRes = actionDelegate.BeginInvoke(c_DEFAULT_STRING, new AsyncCallback( AsyncCallback ), null);
            actionDelegate.EndInvoke(asyncRes);

            TestLibrary.TestFramework.LogError("005", "Exception expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
