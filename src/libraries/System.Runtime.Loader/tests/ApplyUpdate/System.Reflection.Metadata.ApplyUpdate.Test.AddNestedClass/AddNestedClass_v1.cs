// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddNestedClass
    {
        public AddNestedClass()
        {
        }

        public string TestMethod()
        {
            var n = new Nested();
            n.f = "123";
            return n.M();
        }

        private class Nested {
            public Nested() { }
            internal string f;
            public string M () {
                return f + "456";
            }
        }
    }
}
