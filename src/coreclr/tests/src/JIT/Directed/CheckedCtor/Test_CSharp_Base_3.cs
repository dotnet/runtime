// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of lambda expressions in a base ctor argument expression"
//

using System;
using System.Runtime.CompilerServices;

namespace Test
{
    static class App
    {
        static int Main()
        {
            new DerivedClass(7);
            return 100;
        }
    }

    public class BaseClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public BaseClass(Func<int> arg) { Console.Write("BaseClass::.ctor -- `{0}'\r\n", arg()); return; }
    }

    public class DerivedClass : BaseClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public DerivedClass(int selector) : base(() => selector) { }
    }
}

