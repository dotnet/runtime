// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Sandbox3
{
    public class Foo<F>
    {
        public static string Value;

        // [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Action<T>(T value)
        {
            Value = value.ToString();
        }
    }

    public class Dummy { }

    internal class Program
    {
        private static int Main(string[] args)
        {
            string s = "hello";

            Foo<object>.Action<string>(s);
            if (Foo<object>.Value != s)
                throw new Exception();

            int i = 10;
            Foo<Dummy>.Action<int>(i);
            if (Foo<object>.Value != s)
                throw new Exception();
            if (Foo<Dummy>.Value != i.ToString())
                throw new Exception();

            object o = new object();
            Foo<int>.Action<object>(o);
            if (Foo<int>.Value != o.ToString())
                throw new Exception();
            if (Foo<object>.Value != s)
                throw new Exception();
            if (Foo<Dummy>.Value != i.ToString())
                throw new Exception();

            Console.WriteLine("Test SUCCESS");
            return 100;
        }
    }
}
