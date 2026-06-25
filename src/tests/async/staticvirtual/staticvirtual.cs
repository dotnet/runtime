// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class StaticVirtual
{
    interface IHaveStaticVirtuals
    {
        static abstract Task DoTask();
    }

    class ClassWithStaticVirtuals : IHaveStaticVirtuals
    {
        public static async Task DoTask() => await Task.Yield();
    }

    static async Task CallDoTask<T>() where T : IHaveStaticVirtuals => await T.DoTask();

    [Fact]
    public static void TestEntryPoint()
    {
        CallDoTask<ClassWithStaticVirtuals>().Wait();
    }
}
