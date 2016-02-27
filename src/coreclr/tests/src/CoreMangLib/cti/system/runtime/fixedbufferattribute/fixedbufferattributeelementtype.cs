// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
/// <summary>
///ElementType
/// </summary>
public class FixedBufferAttributeElementType
{
    #region const
    int c_LENGTH = 1024;
    #endregion
    public static int Main(string[] args)
    {
        FixedBufferAttributeElementType FixedBufferAttributeElementType = new FixedBufferAttributeElementType();
        TestLibrary.TestFramework.BeginTestCase("FixedBufferAttributeElementType");

        if (FixedBufferAttributeElementType.RunTests())
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
        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Getting ElementType.");

        try
        {
            retVal = VerificationHelper(typeof(string), c_LENGTH,"001.1") && retVal;
            retVal = VerificationHelper(typeof(int), c_LENGTH + c_LENGTH, "001.2") && retVal;
            retVal = VerificationHelper(typeof(object), c_LENGTH * 3, "001.3") && retVal;
     
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
    #region private method
    private bool VerificationHelper(Type elementType,int length,string errorNO)
    {
        bool retVal = true;
        try
        {
            FixedBufferAttribute myDAttribute = new FixedBufferAttribute(elementType, length);
           
            if (myDAttribute.ElementType != elementType )
            {
                TestLibrary.TestFramework.LogError(errorNO, "Getting ElementType has an error occured");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errorNO+".0", "Unexpected exception occurs: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

}
