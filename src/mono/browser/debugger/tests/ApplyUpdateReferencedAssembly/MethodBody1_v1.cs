// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;
//keep the same line number for class in the original file and the updates ones
namespace ApplyUpdateReferencedAssembly
{
    public class MethodBody1 {
        public static string StaticMethod1 () {
                Console.WriteLine("v1");
                double b = 15;
                Debugger.Break();
                return "NEW STRING";
        }
    }

    public class MethodBody2 {
        public static string StaticMethod1 () {
            Console.WriteLine("original");
            int a = 10;
            Debugger.Break();
            return "OLD STRING";
        }
    }

    public class MethodBody3 {
        public static string StaticMethod3 () {
            float b = 15;
            Console.WriteLine("v1");
            return "NEW STRING";
        }
    }



    public class MethodBody4 {
        public static void StaticMethod4 () {
            int a = 10;
            int b = 20;
            Console.WriteLine(a + b);
            Console.WriteLine(a + b);
            Console.WriteLine(a + b);
        }
    }

    public class MethodBody5 {
        public static void StaticMethod1 () {
            Console.WriteLine("breakpoint in a line that will not be changed");
            Console.WriteLine("beforeoriginal");
            Console.WriteLine("original");
        }
    }
    public class MethodBody6 {
        public static void StaticMethod1 () {
            Console.WriteLine("breakpoint in a line that will not be changed");
            Console.WriteLine("original");
        }
        public static void NewMethodStatic () {
            int i = 20;
            newStaticField = 10;
            Console.WriteLine($"add a breakpoint in the new static method, look at locals {newStaticField}");
            /*var newvar = new MethodBody6();
            newvar.NewMethodInstance (10);*/
        }
        public static int newStaticField;
    }

    public class MethodBody7 {
        public static int staticField;
        int attr1;
        string attr2;
        public static void StaticMethod1 () {
            Console.WriteLine("breakpoint in a method in a new class");
            Console.WriteLine("original");
            MethodBody7 newvar = new MethodBody7();
            staticField = 80;            
            newvar.InstanceMethod();
        }
        public void InstanceMethod () {
            int aLocal = 50;
            attr1 = 15;
            attr2 = "20";
            Console.WriteLine($"add a breakpoint the instance method of the new class");
        }
    }
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
// DO NOT CHANGE
    // public class MethodBody9 {
    //     public static int M1(int x, int y) {
    //         return x + y;
    //     }
        
    //     public static int test() {
    //         return M1(1, 2);
    //     }
    // }

    public class MethodBody10 {
        public static void StaticMethod1 () {
            Console.WriteLine("breakpoint in a method in a new class");
            StaticMethod2();
            Console.WriteLine("do not step into StaticMethod2");
        }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static void StaticMethod2 () {
            Console.WriteLine($"do not step into here");
        }
    }

    public class MethodBody11 {
        public static void StaticMethod1 () {
            Console.WriteLine("breakpoint in a line that will not be changed");
            Console.WriteLine("original");
        }
        public static void NewMethodStaticWithThrow () {
            int i = 20;
            Console.WriteLine($"add a breakpoint in the new static method, look at locals {i}");
            throw new Exception("my exception");
            /*var newvar = new MethodBody6();
            newvar.NewMethodInstance (10);*/
        }
    }
}
