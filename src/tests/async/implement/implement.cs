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
    [RuntimeAsyncMethodGeneration(false)] // Select the Task-returning interface implementations rather than runtime-async dispatch.
    public static async Task TestEntryPoint()
    {
        IBase1 b1 = new Derived1();
        Assert.Equal(2, await b1.M1());

        b1 = new Derived1a();
        Assert.Equal(3, await b1.M1());

        IBase2 b2 = new Derived2();
        Assert.Equal(12, await b2.M1());

        b2 = new Derived2a();
        Assert.Equal(22, await b2.M1());
    }
}
