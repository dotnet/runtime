// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;
//keep the same line number for class in the original file and the updates ones
namespace ApplyUpdateReferencedAssembly
{
    public class AddInstanceFields {
        public static string StaticMethod1 () {
            C c = new();
            c.Field2 = "spqr";
            Debugger.Break();
            return "OLD STRING";
        }

        public class C {
            public C()
            {
                Field1 = 123.0;
                Field2 = "abcd";
            }
            public double Field1;
            public string Field2;
            public string Field3Unused;
        }
    }
        
}
