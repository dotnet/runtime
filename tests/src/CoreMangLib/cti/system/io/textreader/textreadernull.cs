// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;

public class TextReaderNull
{

    public static int Main()
    {
        TextReaderNull TextReaderNull = new TextReaderNull();

        TestLibrary.TestFramework.BeginTestCase("TextReaderNull");
        if (TextReaderNull.RunTests())
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
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong

    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare  TextReader.Null to null.");
        try
        {
            TextReader myReader = TextReader.Null;
            if (myReader.ReadLine()!=null  )
            {
                TestLibrary.TestFramework.LogError("001.1", "myReader.ReadLine() should return  null.");
                retVal = false;
               
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}
