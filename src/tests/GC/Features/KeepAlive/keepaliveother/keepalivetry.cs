// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests KeepAlive() in try...catch...finally

using System;

public class Test_keepalivetry
{

    public class Dummy
    {

        public static bool visited;
        ~Dummy()
        {
            //Console.WriteLine("In Finalize() of Dummy");
            visited=true;
        }
    }

    public static int Main()
    {

        Dummy[] obj = new Dummy[100];

        try
        {
            for(int i=0;i<100;i++)
            {
                obj[i]= new Dummy();
            }
            throw new IndexOutOfRangeException();
        }
        catch(Exception)
        {
            Console.WriteLine("Caught exception");
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        finally
        {
            Console.WriteLine("Should come here..still keeping object alive");
            GC.KeepAlive(obj);
        }

        if(Dummy.visited == false)
        {  // has not visited the Finalize()
            Console.WriteLine("Test for KeepAlive() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for KeepAlive() failed!");
            return 1;
        }

    }
}
