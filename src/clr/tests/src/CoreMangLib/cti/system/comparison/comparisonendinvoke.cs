// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

///<summary>
///System.Comparison.EndInvoke
///</summary>

public class ComparisonEndInvoke
{
    private const int c_MIN_ARRAY_LENGTH = 0;
    private const int c_MAX_ARRAY_LENGTH = byte.MaxValue;

    public static int Main()
    {
        ComparisonEndInvoke testObj = new ComparisonEndInvoke();
        TestLibrary.TestFramework.BeginTestCase("for method of Comparison.EndInvoke");
        if (testObj.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;


        return retVal;
    }
    
    public bool NegTest1()
    {
        bool retVal = true;

        char[] charsA, charsB;

        charsA = new char[TestLibrary.Generator.GetInt32(-55) % (c_MAX_ARRAY_LENGTH + 1)];
        charsB = new char[TestLibrary.Generator.GetInt32(-55) % (c_MAX_ARRAY_LENGTH + 1)];

        TestLibrary.TestFramework.BeginScenario("NegTest1: call the method EndInvoke");

        Comparison<Array> comparison = CompareByLength;
        IAsyncResult asyncResult = null;

        
        try
        {
            comparison.EndInvoke(asyncResult);

            retVal = false;
            TestLibrary.TestFramework.LogError("003", "NotSupportedException expected");
        }
        catch (NotSupportedException)
        {
            // expected     
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #region Helper methods
    private int CompareByLength(Array arrayA, Array arrayB)
    {
        if (null == arrayA)
        {
            if (null == arrayB) return 0;
            else return -1;
        }
        else
        {
            if (null == arrayB) return 1;
            else
            {
                if (arrayA.Length == arrayB.Length) return 0;
                else return (arrayA.Length > arrayB.Length) ? 1 : -1;
            }
        }
    }
    #endregion
}
