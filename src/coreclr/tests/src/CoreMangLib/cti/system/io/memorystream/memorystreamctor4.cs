using System.Security;
using System;
using System.IO;
using System.Reflection;

public class MemoryStreamCtor4
{
    public static int Main()
    {
        MemoryStreamCtor4 ac = new MemoryStreamCtor4();

        TestLibrary.TestFramework.BeginTestCase("MemoryStreamCtor4");

        if (ac.RunTests())
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

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;
        MemoryStream mem;
        int capacity;
        byte[] array;
        bool   canWrite;

        TestLibrary.TestFramework.BeginScenario("PosTest1: MemoryStream.Ctor(byte[], bool)");

        try
        {
            canWrite = (TestLibrary.Generator.GetByte(-55)  % 2) == 0;
            capacity = (TestLibrary.Generator.GetInt32(-55) % 2048) + 1;
            array    = new byte[ capacity ];

            for (int i=0; i<array.Length; i++) array[i] = TestLibrary.Generator.GetByte(-55);

            mem = new MemoryStream(array, canWrite);

            for (int i=0; i<array.Length; i++)
            {
                byte val = (byte)mem.ReadByte();
                if (array[i] != val)
                {
                    TestLibrary.TestFramework.LogError("001", "Stream mismatch["+i+"]: Expected("+array[i]+") Actual("+val+")");
                    retVal = false;
                }
            }

            if (capacity != mem.Capacity)
            {
                TestLibrary.TestFramework.LogError("002", "Capacity mixmatch: Expected("+capacity+") Actual("+mem.Capacity+")");
                retVal = false;
            }

            if (canWrite != mem.CanWrite)
            {
                TestLibrary.TestFramework.LogError("003", "Can write unexpected: Expected("+canWrite+") Actual("+mem.CanWrite+")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}

