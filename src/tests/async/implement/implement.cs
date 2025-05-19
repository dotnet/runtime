// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Implement
{
    interface IBase1
    {
        public Task<int> M1();
    }

    class Derived1 : IBase1
    {
        [RuntimeAsyncMethodGeneration(false)]
        public async Task<int> M1()
        {
            await Task.Yield();
            return 2;
        }
    }

    class Derived1a : IBase1
    {
        public async Task<int> M1()
        {
            await Task.Yield();
            return 3;
        }
    }

    interface IBase2
    {
        public Task<int> M1();
    }

    class Derived2 : IBase2
    {
        public async Task<int> M1()
        {
            await Task.Yield();
            return 12;
        }
    }

    class Derived2a : IBase2
    {
        [RuntimeAsyncMethodGeneration(false)]
        public async Task<int> M1()
        {
            await Task.Yield();
            return 22;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        IBase1 b1 = new Derived1();
        Assert.Equal(2, b1.M1().Result);

        b1 = new Derived1a();
        Assert.Equal(3, b1.M1().Result);

        IBase2 b2 = new Derived2();
        Assert.Equal(12, b2.M1().Result);

        b2 = new Derived2a();
        Assert.Equal(22, b2.M1().Result);
    }
}
