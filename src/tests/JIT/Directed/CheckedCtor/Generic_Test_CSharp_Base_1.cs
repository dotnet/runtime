// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of ?: in a generic base ctor argument expression"
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
        public DerivedClass(int selector) : base((selector < 4) ? "LessThan4" : "AtLeast4") { }
    }
}

