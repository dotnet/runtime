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
            var n = new Nested<string, int>();
            n.Eff = "123";
	    n.g = 456;
            return n.M();
        }

        private class Nested<T, U> {
            public Nested() { }
            internal T f;
	    internal U g;
	    public T Eff {
		get => f;
		set { f = value; }
	    }
            public string M () {
                return Eff.ToString() + g.ToString();
            }
        }
    }
}
