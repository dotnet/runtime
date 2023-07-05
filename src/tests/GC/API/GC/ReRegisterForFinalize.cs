// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests ReRegisterForFinalize()

using System;
using System.Runtime.CompilerServices;

public class Test_ReRegisterForFinalize {

    public class Dummy {

        public static int flag;
        ~Dummy() {
            Console.WriteLine("In Finalize() of Dummy");
            if(flag == 0) flag=1;  // one object has visited;
            else flag=0; //error-- both objects have visited
        }
    }

    public class CreateObj{
        Dummy obj1;
        Dummy obj2;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public CreateObj() {
            obj1 = new Dummy();
            obj2 = new Dummy();


            GC.SuppressFinalize(obj1);    // should not call the Finalize() for obj1
            GC.SuppressFinalize(obj2);    // should not call the Finalize() for obj2
        }

        // Make sure we have no references left to obj1 by doing this in a
        // non-inlined separate function.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReRegisterAndNullFields()
        {
            GC.ReRegisterForFinalize(obj1); // should call Finalize() for obj1 now.

            obj1=null;
            obj2=null;
        }

        public bool RunTest() {
            ReRegisterAndNullFields();

            GC.Collect();

            GC.WaitForPendingFinalizers();   // call all Finalizers.

            if(Dummy.flag==1) {
                return true;
            }
            else {
                return false;
            }
        }

    }
    public static int Main() {
        CreateObj temp = new CreateObj();
        bool passed = temp.RunTest();

        if(passed) {
            Console.WriteLine("Test for ReRegisterForFinalize() passed!");
            return 100;
        }
        else {
            Console.WriteLine("Test for ReRegisterForFinalize() failed!");
            return 1;
        }
    }
}
