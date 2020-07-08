// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests IO in Finalize()

using System;
using System.IO;
using System.Runtime.CompilerServices;

public class Test {

    public class Dummy {

        public static bool visited;

        ~Dummy() {

            Console.WriteLine("In Finalize() of Dummy");

            visited=true;

            try
            {
                FileStream test = new FileStream("temp.txt", FileMode.Open, FileAccess.Read);
                using (StreamReader read = new StreamReader(test))
                {
                    // while not at the end of the file
                    while (read.Peek() > -1)
                        Console.WriteLine(read.ReadLine());
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception handled: " + e);
                visited=false;
            }

        }
    }

    public class CreateObj{
        public Dummy obj;

        public CreateObj() {
            obj = new Dummy();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void RunTest() {
            obj=null;
        }
    }

    public static int Main() {
        CreateObj temp = new CreateObj();

        using (StreamWriter writer = File.CreateText("temp.txt"))
        {
writer.WriteLine(@"***************** START ************************
This is a test file for testing IO in Finalizers.
Line 1
Line 2
Line 3
Line 4
Line 5
Line 6
Line 7
Line 8
Line 9
Line 10
Line 11
Line 12
Line 13
Line 14
Line 15
Line 16
Line 17
Line 18
Line 19
Line 20
Line 21
Line 22
Line 23
******************* END *****************************");
        }

        temp.RunTest();

        GC.Collect();
        GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.
        GC.Collect();


        if (Dummy.visited) 
        {
            Console.WriteLine("Test for Finalize() & WaitForPendingFinalizers() passed!");
            return 100;
        }

        Console.WriteLine("Test for Finalize() & WaitForPendingFinalizers() failed!");
        return 1;

    }
}
