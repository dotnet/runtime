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
    }

    class Derived1 : Base
    {
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        public override async Task<int> M1()
        {
            await Task.Yield();
            return 2;
        }
    }

    class Derived2 : Derived1
    {
        public override async Task<int> M1()
        {
            await Task.Yield();
            return 3;
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
    }

    class Derived11 : Base1
    {
        public override async Task<int> M1()
        {
            await Task.Yield();
            return 12;
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
    }


    [Fact]
    public static void TestEntryPoint()
    {
        Base b = new Derived1();
        Assert.Equal(2, b.M1().Result);

        b = new Derived2();
        Assert.Equal(3, b.M1().Result);

        Derived1 d = new Derived2();
        Assert.Equal(3, d.M1().Result);


        Base1 b1 = new Derived11();
        Assert.Equal(12, b1.M1().Result);

        b1 = new Derived12();
        Assert.Equal(13, b1.M1().Result);

        Derived11 d1 = new Derived12();
        Assert.Equal(13, d1.M1().Result);

    }
}
