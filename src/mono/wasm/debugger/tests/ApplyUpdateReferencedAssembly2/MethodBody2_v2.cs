// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;
//keep the same line number for class in the original file and the updates ones
namespace ApplyUpdateReferencedAssembly
{
    public class AddMethod {
        public static string StaticMethod1 () {
            Console.WriteLine("original");
            int a = 10;
            Debugger.Break();
            return "OLD STRING";
        }
        public static string StaticMethod2 () {
            Console.WriteLine("original");
            int a = 10;
            Debugger.Break();
            return "OLD STRING";
        }
        public static string StaticMethod3 () {
            Console.WriteLine("original");
            int a = 10;
            Debugger.Break();
            return "OLD STRING";
        }
    }
}