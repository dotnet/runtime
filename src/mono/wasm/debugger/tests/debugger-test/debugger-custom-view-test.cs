
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DebuggerTests
{
    [DebuggerDisplay("Some {Val1} Value {Val2} End")]
    class WithDisplayString
    {
        internal string Val1 = "one";

        public int Val2 { get { return 2; } }
    }

    class WithToString
    {
        public override string ToString ()
        {
            return "SomeString";
        }
    }

    [DebuggerTypeProxy(typeof(TheProxy))]
    class WithProxy
    {
        public string Val1 {
            get { return "one"; }
        }
    }

    class TheProxy
    {
        WithProxy wp;

        public TheProxy (WithProxy wp)
        {
            this.wp = wp;
        }

        public string Val2 {
            get { return wp.Val1; }
        }
    }

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    class DebuggerDisplayMethodTest
    {
        int someInt = 32;
        int someInt2 = 43;

        string GetDebuggerDisplay ()
        {
            return "First Int:" + someInt + " Second Int:" + someInt2;
        }
    }

    class DebuggerCustomViewTest
    {
        public static void run()
        {
            var a = new WithDisplayString();
            var b = new WithProxy();
            var c = new DebuggerDisplayMethodTest();
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);
        }
    }
}
