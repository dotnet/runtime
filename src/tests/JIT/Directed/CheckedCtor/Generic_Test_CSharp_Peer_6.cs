// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of 'new T()' in a generic peer ctor argument expression"
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test
{
    public static class App
    {
        [Fact]
        public static int TestEntryPoint()
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

