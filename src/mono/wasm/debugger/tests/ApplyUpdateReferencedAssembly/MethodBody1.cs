// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;

namespace ApplyUpdateReferencedAssembly
{
    public class MethodBody1 {
        public static string StaticMethod1 () {
            Console.WriteLine("original");
            int a = 10;
            Debugger.Break();
            Console.WriteLine("passei do break");
            return "OLD STRING";
        }
    }
}
