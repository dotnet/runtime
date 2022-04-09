// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddNestedClass
    {
        public static Action<string> X; // make the linker happy
        public static Delegate Y;
        public event Action<string> Evt;
        public void R () { Evt ("123"); }
        public AddNestedClass()
        {
        }

        public string TestMethod()
        {
            var n = new Nested<string, int>();
            n.Eff = "123";
            n.g = 456;
            n.Evt += new Action<string> (n.DefaultHandler);
            n.RaiseEvt();
            return n.M() + n.buf;
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

            public event Action<string> Evt;

            public void RaiseEvt () {
                Evt ("789");
            }

            public string buf;

            public void DefaultHandler (string s) {
                this.buf = s;
            }
        }
    }
}
