// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

namespace Bug565326
{
    /* 
        Compile this program using "csc /o+ ".
        It should produce the following output:
        Should see this line: B will return True
        Should see this line: B will return False
        Should see this line: In E()
    */

    class A
    {
        public static bool boolRetValue;

        public static int intValue = 2;

        // Case1: 
        // Forward jumping with distance 0 caused the entire condition tree
        // to be ignored. Therefore MethodReturningBool is not called.
        public void Case1()
        {
            if ((intValue != 3) && MethodReturningBool())
                return;
        }

        // This method can return true or false.
        public bool MethodReturningBool()
        {
            Console.WriteLine("Should see this line: B will return " + boolRetValue);
            return boolRetValue;
        }


        // Case2: 
        // Forward jumping with distance 0 caused the entire condition tree
        // to be ignored. Therefore MethodReturningInt is not called.  
        // Note: This case demonstrates that the CALL does NOT have to be evaluated right before the jump.
        public void Case2()
        {
            if (MethodReturningInt() + 10 > 3)
                return;
        }

        // This method returns an int.
        public int MethodReturningInt()
        {
            Console.WriteLine("Should see this line: In E()");
            return intValue;
        }

    }

    class Class1
    {
        static int Main(string[] args)
        {
            try
            {
                A a = new A();

                A.boolRetValue = true;
                a.Case1();
                A.boolRetValue = false;
                a.Case1();

                a.Case2();
                Console.WriteLine("Test Success");
                return 100;
            }
            catch
            {
                Console.WriteLine("Test Failed");
                return 101;
            }
        }
    }
}

