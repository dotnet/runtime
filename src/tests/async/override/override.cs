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
    public static void TestEntryPoint()
    {
        Base b = new Derived1();
        Assert.Equal(2, b.M1().Result);
        Assert.Equal(2, AwaitBaseM1(b).Result);
        Assert.Equal(2, b.M2(2, 3).Result);
        Assert.Equal(2, AwaitBaseM2(b, 2, 3).Result);

        b = new Derived2();
        Assert.Equal(3, b.M1().Result);
        Assert.Equal(3, AwaitBaseM1(b).Result);
        Assert.Equal(3, b.M2(2, 3).Result);
        Assert.Equal(3, AwaitBaseM2(b, 2, 3).Result);

        Derived1 d = new Derived2();
        Assert.Equal(3, d.M1().Result);
        Assert.Equal(3, d.M2(2, 3).Result);


        Base1 b1 = new Derived11();
        Assert.Equal(12, b1.M1().Result);
        Assert.Equal(12, AwaitBaseM1(b1).Result);
        Assert.Equal(12, b1.M2(12, 13).Result);
        Assert.Equal(12, AwaitBaseM2(b1, 12, 13).Result);

        b1 = new Derived12();
        Assert.Equal(13, b1.M1().Result);
        Assert.Equal(13, AwaitBaseM1(b1).Result);
        Assert.Equal(13, b1.M2(12, 13).Result);
        Assert.Equal(13, AwaitBaseM2(b1, 12, 13).Result);

        Derived11 d1 = new Derived12();
        Assert.Equal(13, d1.M1().Result);
        Assert.Equal(13, d1.M2(12, 13).Result);

    }
}
