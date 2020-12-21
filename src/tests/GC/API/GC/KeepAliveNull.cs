// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests KeepAlive()

using System;
using System.Runtime.CompilerServices;

public class Test
{
    public static bool visited;
    public class Dummy
    {
        ~Dummy()
        {
            Console.WriteLine("In Finalize() of Dummy");
            visited = true;
        }
    }

    public class CreateObj
    {
        public Dummy obj;

        public CreateObj()
        {
            obj = new Dummy();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void DestroyObj()
        {
            obj = null;     // this will collect the obj even if we have KeepAlive()		
        }

        public void RunTest()
        {
            DestroyObj();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            GC.KeepAlive(obj);  // will keep alive 'obj' till this point
        }
    }

    public static int Main()
    {
        CreateObj temp = new CreateObj();
        temp.RunTest();

        if (visited)
        {
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
