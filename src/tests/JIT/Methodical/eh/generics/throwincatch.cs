// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;


public class GenException<T> : Exception
{
}

public interface IGen
{
    bool ExceptionTest();
}

public class Gen<T> : IGen
{
    public bool ExceptionTest()
    {
        try
        {
            Console.WriteLine("in try");
            try
            {
                Console.WriteLine("  in try");
                throw new GenException<T>();
            }
            catch (Exception exp)
            {
                Console.WriteLine("  in catch: " + exp.Message);
                throw exp;
            }
        }
        catch (GenException<T> exp)
        {
            Console.WriteLine("in catch: " + exp.Message);
            return true;
        }
        catch (Exception)
        {
            Console.WriteLine("in wrong catch!!");
            return false;
        }
    }
}

public class Test
{
    private static TestUtil.TestLog testLog;

    static Test()
    {
        // Create test writer object to hold expected output
        StringWriter expectedOut = new StringWriter();

        // Write expected output to string writer object
        Exception[] expList = new Exception[] {
            new GenException<int>(),
            new GenException<double>(),
            new GenException<string>(),
            new GenException<object>(),
            new GenException<Exception>()
        };
        for (int i = 0; i < expList.Length; i++)
        {
            expectedOut.WriteLine("in try");
            expectedOut.WriteLine("  in try");
            expectedOut.WriteLine("  in catch: " + expList[i].Message);
            expectedOut.WriteLine("in catch: " + expList[i].Message);
            expectedOut.WriteLine("{0}", true);
        }

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);

    }

    public static int Main()
    {
        //Start recording
        testLog.StartRecording();

        // create test list
        IGen[] genList = new IGen[] {
            new Gen<int>(),
            new Gen<double>(),
            new Gen<string>(),
            new Gen<object>(),
            new Gen<Exception>()
        };

        // run test
        for (int i = 0; i < genList.Length; i++)
        {
            Console.WriteLine(genList[i].ExceptionTest());
        }

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }

}
