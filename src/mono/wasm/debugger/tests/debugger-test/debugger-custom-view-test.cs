// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

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

    [DebuggerDisplay("FirstName: {FirstName}, SurName: {SurName}, Age: {Age}")]
    public class Person {
        public string FirstName { get; set; }
        public string SurName { get; set; }
        public int Age { get; set; }
    }

    class DebuggerCustomViewTest
    {
        public static void run()
        {
            var a = new WithDisplayString();
            var b = new WithProxy();
            var c = new DebuggerDisplayMethodTest();
            List<int> myList = new List<int>{ 1, 2, 3, 4 };
            var listToTestToList = System.Linq.Enumerable.Range(1, 11);

            Dictionary<string, string> openWith = new Dictionary<string, string>();

            openWith.Add("txt", "notepad");
            openWith.Add("bmp", "paint");
            openWith.Add("dib", "paint");
            var person1 = new Person { FirstName = "Anton", SurName="Mueller", Age = 44};
            var person2 = new Person { FirstName = "Lisa", SurName="Müller", Age = 41};

            Console.WriteLine("break here");

            Console.WriteLine("break here");
        }
    }
}
