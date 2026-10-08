// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Use of lambda expressions in a base ctor argument expression"
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace JitTest_Directed_CheckedCtor_Test_CSharp_Base_3
{
    public static class App
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            new DerivedClass(7);
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
