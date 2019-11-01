// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of lambda expressions in a peer ctor argument expression"
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
        public DerivedClass(int selector) : this(() => selector) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DerivedClass(Func<int> arg) : base(arg) { }
    }
}

