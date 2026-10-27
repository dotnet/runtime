// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Override
{
    class Base
    {
        public virtual async Task<int> M1()
        {
            await Task.Yield();
            return 1;
        }

        public virtual async Task<T> M2<T>(T first, T second)
        {
            await Task.Yield();
            return default(T);
        }
    }

    class Derived1 : Base
    {
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        public override async Task<int> M1()
        {
            await Task.Yield();
            return 2;
        }

        public override async Task<T> M2<T>(T first, T second)
        {
            await Task.Yield();
            return first;
        }
    }

    class Derived2 : Derived1
    {
        public override async Task<int> M1()
        {
            await Task.Yield();
            return 3;
        }

        public override async Task<T> M2<T>(T first, T second)
        {
            await Task.Yield();
            return second;
        }
    }


    class Base1
    {
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        public virtual async Task<int> M1()
        {
            await Task.Yield();
            return 11;
        }

        public virtual async Task<T> M2<T>(T first, T second)
        {
            await Task.Yield();
            return default(T);
        }
    }

    class Derived11 : Base1
    {
        public override async Task<int> M1()
        {
            await Task.Yield();
            return 12;
        }

        public override async Task<T> M2<T>(T first, T second)
        {
            await Task.Yield();
            return first;
        }
    }

    class Derived12 : Derived11
    {
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        public override async Task<int> M1()
        {
            await Task.Yield();
            return 13;
        }

        public override async Task<T> M2<T>(T first, T second)
        {
            await Task.Yield();
            return second;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> AwaitBaseM1(Base b) => await b.M1();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> AwaitBaseM1(Base1 b) => await b.M1();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<T> AwaitBaseM2<T>(Base b, T first, T second) => await b.M2(first, second);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<T> AwaitBaseM2<T>(Base1 b, T first, T second) => await b.M2(first, second);

    [Fact]
    [RuntimeAsyncMethodGeneration(false)] // Select the Task-returning overrides; the AwaitBase helpers exercise runtime-async dispatch.
    public static async Task TestEntryPoint()
    {
        Base b = new Derived1();
        Assert.Equal(2, await b.M1());
        Assert.Equal(2, await AwaitBaseM1(b));
        Assert.Equal(2, await b.M2(2, 3));
        Assert.Equal(2, await AwaitBaseM2(b, 2, 3));

        b = new Derived2();
        Assert.Equal(3, await b.M1());
        Assert.Equal(3, await AwaitBaseM1(b));
        Assert.Equal(3, await b.M2(2, 3));
        Assert.Equal(3, await AwaitBaseM2(b, 2, 3));

        Derived1 d = new Derived2();
        Assert.Equal(3, await d.M1());
        Assert.Equal(3, await d.M2(2, 3));


        Base1 b1 = new Derived11();
        Assert.Equal(12, await b1.M1());
        Assert.Equal(12, await AwaitBaseM1(b1));
        Assert.Equal(12, await b1.M2(12, 13));
        Assert.Equal(12, await AwaitBaseM2(b1, 12, 13));

        b1 = new Derived12();
        Assert.Equal(13, await b1.M1());
        Assert.Equal(13, await AwaitBaseM1(b1));
        Assert.Equal(13, await b1.M2(12, 13));
        Assert.Equal(13, await AwaitBaseM2(b1, 12, 13));

        Derived11 d1 = new Derived12();
        Assert.Equal(13, await d1.M1());
        Assert.Equal(13, await d1.M2(12, 13));

    }
}
