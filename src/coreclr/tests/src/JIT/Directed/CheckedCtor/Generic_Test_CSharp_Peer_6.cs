// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of 'new T()' in a generic peer ctor argument expression"
//

using System;
using System.Runtime.CompilerServices;

namespace Test
{
    static class App
    {
        static int Main()
        {
            new DerivedClass<Reftype>();
            new DerivedClass<Valuetype>();
            return 100;
        }
    }

    public class BaseClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public BaseClass(T arg) { Console.Write("BaseClass::.ctor -- `{0}'\r\n", arg.ToString()); return; }
    }

    public class DerivedClass<T> : BaseClass<T> where T : new()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public DerivedClass() : this(new T()) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DerivedClass(T arg) : base(arg) { }
    }

    public class Reftype
    {
        public override string ToString() { return "Reftype instance"; }
    }

    public struct Valuetype
    {
        public override string ToString() { return "Valuetype instance"; }
    }
}

