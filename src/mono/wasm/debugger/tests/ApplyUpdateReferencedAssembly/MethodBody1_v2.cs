// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System;

namespace ApplyUpdateReferencedAssembly
{
    public class MethodBody1 {
        public static string StaticMethod1 () {
            Console.WriteLine("v2");
            bool c = true;
            Debugger.Break();
            Console.WriteLine("passei v2");
            return "NEWEST STRING";
        }
    }
}
