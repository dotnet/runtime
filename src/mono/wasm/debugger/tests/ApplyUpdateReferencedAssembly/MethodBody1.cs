// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;
//keep the same line number for class in the original file and the updates ones
namespace ApplyUpdateReferencedAssembly
{
    public class MethodBody1 {
        public static string StaticMethod1 () {
            Console.WriteLine("original");
            int a = 10;
            Debugger.Break();
            return "OLD STRING";
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
            int a = 10;
            Console.WriteLine("original");
            return "OLD STRING";
        }
    }



    public class MethodBody4 {
        public static void StaticMethod4 () {
        }
    }






    public class MethodBody5 {
        public static void StaticMethod1 () {
            Console.WriteLine("breakpoint in a line that will not be changed");
            Console.WriteLine("original");
        }
    }
}
