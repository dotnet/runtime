// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of ?: in a generic peer ctor argument expression"
//

using System;
using System.Runtime.CompilerServices;

namespace Test
{
    static class App
    {
        static int Main()
        {
            new DerivedClass<int>(3);
            new DerivedClass<int>(8);
            return 100;
        }
    }

    public class BaseClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public BaseClass(string arg) { Console.Write("BaseClass::.ctor -- `{0}'\r\n", arg); return; }
    }

    public class DerivedClass<T> : BaseClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public DerivedClass(int selector) : this((selector < 4) ? "LessThan4" : "AtLeast4") { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DerivedClass(string arg) : base(arg) { }
    }
}

