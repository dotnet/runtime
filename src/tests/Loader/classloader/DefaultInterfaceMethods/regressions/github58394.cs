// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace GenericDimValuetypeBug
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            if (RunOne() != 17)
                return 1;
            if (RunTwo() != 23)
                return 2;
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int RunOne()
        {
            return (new Foo() { x = 17 } as IFoo).NoCrash();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int RunTwo()
        {
            return (new Foo() { x = 23 } as IFoo).Crash<int>();
        }
    }

    interface IFoo
    {
        int Crash<T>() => Bla();

        int NoCrash() => Bla();

        int Bla();
    }

    struct Foo: IFoo
    {
        public int x;
        public int Bla() => x;
    }
}
