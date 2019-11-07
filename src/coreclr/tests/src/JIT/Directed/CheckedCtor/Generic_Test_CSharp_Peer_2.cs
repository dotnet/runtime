// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of ?? in a generic peer ctor argument expression"
//

using System;
using System.Runtime.CompilerServices;

namespace Test
{
    static class App
    {
        static int Main()
        {
            new DerivedClass<int>("NotNull");
            new DerivedClass<int>(null);
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
        public DerivedClass(string selector) : this(selector ?? "NeededToFallBack", 0) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DerivedClass(string arg, int marker) : base(arg) { }
    }
}

