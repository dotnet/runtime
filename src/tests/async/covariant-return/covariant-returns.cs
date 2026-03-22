// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class CovariantReturns
{
    [Fact]
    public static void Test1EntryPoint()
    {
        Test1().Wait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test0()
    {
        Base b = new Base();
        await b.M1();
        Assert.Equal("Base.M1;", b.Trace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test1()
    {
        Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();
        await b.M1();
        Assert.Equal("Derived.M1;", b.Trace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test2()
    {
        Base b = DateTime.Now.Year > 0 ? new Derived2() : new Base();
        await b.M1();
        Assert.Equal("Derived2.M1;Derived.M1;", b.Trace);
    }

    struct S1
    {
        public Guid guid;
        public int num;

        public S1(int num)
        {
            this.guid = Guid.NewGuid();
            this.num = num;
        }
    }

    class Base
    {
        public string Trace;
        public virtual Task M1()
        {
            Trace += "Base.M1;";
            return Task.CompletedTask;
        }
    }

    class Derived : Base
    {
        public override Task<S1> M1()
        {
            Trace += "Derived.M1;";
            return Task.FromResult(new S1(42));
        }
    }

    class Derived2 : Derived
    {
        public override async Task<S1> M1()
        {
            Trace += "Derived2.M1;";
            await Task.Delay(1);
            await base.M1();
            return new S1(4242);
        }
    }
}
