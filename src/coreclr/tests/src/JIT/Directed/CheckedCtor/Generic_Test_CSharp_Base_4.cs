// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Instance field initializers preceding the generic base ctor callsite"
//

using System;
using System.Runtime.CompilerServices;

namespace Test
{
    static class App
    {
        static int Main()
        {
            new DerivedClass<int>(7);
            return 100;
        }
    }

    public class BaseClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public BaseClass(int arg) { Console.Write("BaseClass::.ctor -- `{0}'\r\n", arg); return; }
    }

    public class DerivedClass<T> : BaseClass
    {
        private static readonly Random Generator = new Random();
        private static string GetString() { return "Text"; }
        public int Field1 = ((Generator.Next(5, 8) == 10) ? 10 : 20);
        public string Field2 = (GetString() ?? "NeededToFallBack");
        public Func<int> Field3 = () => Generator.Next(5, 8);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DerivedClass(int selector) : base(selector) { }
    }
}

