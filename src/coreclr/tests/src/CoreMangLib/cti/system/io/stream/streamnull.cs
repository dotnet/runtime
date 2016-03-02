// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO; // For Stream
using System.Security;

[SecuritySafeCritical]
public class StreamNull
{
    #region Private Fields
    private const int c_ARRAY_SIZE = 1024;
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure Stream.Null can be used");

        try
        {
            if (null == Stream.Null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Stream.Null is a null reference");
                retVal = false;
            }
            
            if (!Stream.Null.CanRead)
            {
                TestLibrary.TestFramework.LogError("001.2", "Stream.Null.CanRead returns false");
                retVal = false;
            }

            if (!Stream.Null.CanWrite)
            {
                TestLibrary.TestFramework.LogError("001.3", "Stream.Null.CanWrite returns false");
                retVal = false;
            }

            if (!Stream.Null.CanSeek)
            {
                TestLibrary.TestFramework.LogError("001.4", "Stream.Null.CanSeek returns false");
                retVal = false;
            }

            if (Stream.Null.Length != 0 )
            {
                TestLibrary.TestFramework.LogError("001.5", "Stream.Null.Length returns " + Stream.Null.Length);
                retVal = false;
            }

            int ret = Stream.Null.ReadByte();
            if (ret != -1)
            {
                TestLibrary.TestFramework.LogError("001.6", "Stream.Null.ReadByte() returns " + ret);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.7", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure Stream.Null.Position can not be changed");

        try
        {
            long randValue = TestLibrary.Generator.GetInt64(-55);

            if (Stream.Null.Position != 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "Stream.Null.Position returns " + Stream.Null.Position);
                retVal = false;
            }

            Stream.Null.Position = randValue;
            if (Stream.Null.Position != 0)
            {
                TestLibrary.TestFramework.LogError("002.2", "Stream.Null.Position returns " + Stream.Null.Position);
                TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] randValue = " + randValue);
                retVal = false;
            }

            Stream.Null.Position = randValue;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Ensure Stream.Null.Read takes no effect");

        try
        {
            Byte[] randByteArray = new Byte[c_ARRAY_SIZE] ;
            int offset = TestLibrary.Generator.GetInt32(-55);
            int count = TestLibrary.Generator.GetInt32(-55);

            int readCount = Stream.Null.Read(randByteArray, offset, count);
            if (readCount != 0)
            {
                TestLibrary.TestFramework.LogError("003.1", "Stream.Null.Read returns " + readCount);
                TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] randByteArray = " + randByteArray + ", offset = " + offset + ", count = " + count);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        Byte[] randByteArray = null;
        int offset = 0;
        int count = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Ensure Stream.Null.Write takes no effect");

        try
        {
            randByteArray = new Byte[c_ARRAY_SIZE];
            offset = TestLibrary.Generator.GetInt32(-55);
            count = TestLibrary.Generator.GetInt32(-55);

            Stream.Null.Write(randByteArray, offset, count);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] randByteArray = " + randByteArray + ", offset = " + offset + ", count = " + count);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Ensure Stream.Null.Seek takes no effect");

        try
        {
            long offset = TestLibrary.Generator.GetInt64(-55);

            long retOffset = Stream.Null.Seek(offset, SeekOrigin.Begin);
            if (retOffset != 0)
            {
                TestLibrary.TestFramework.LogError("005.1", "Stream.Null.Seek returns " + retOffset);
                TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] offset = " + offset);
                retVal = false;
            }

            retOffset = Stream.Null.Seek(offset, SeekOrigin.Current);
            if (retOffset != 0)
            {
                TestLibrary.TestFramework.LogError("005.2", "Stream.Null.Seek returns " + retOffset);
                TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] offset = " + offset);
                retVal = false;
            }

            retOffset = Stream.Null.Seek(offset, SeekOrigin.End);
            if (retOffset != 0)
            {
                TestLibrary.TestFramework.LogError("005.3", "Stream.Null.Seek returns " + retOffset);
                TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] offset = " + offset);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.4", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StreamNull test = new StreamNull();

        TestLibrary.TestFramework.BeginTestCase("StreamNull");

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
