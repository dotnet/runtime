// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class AsyncGvm
{
    interface I2
    {
        Task<string> M0<T>();
        Task<string> M1<T>(object a0, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8);
    }

    class Class2 : I2
    {
        public async Task<string> M0<T>()
        {
            await Task.Yield();
            return typeof(T).ToString();
        }

        public async Task<string> M1<T>(object a0, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
        {
            await Task.Yield();
            return typeof(T).ToString();
        }
    }

    static I2 o2;
    static async Task<string> CallClass2M0()
    {
        o2 = new Class2();
        return await o2.M0<string>();
    }

    static async Task<string> CallClass2M1()
    {
        o2 = new Class2();
        return await o2.M1<string>(default, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public static void NoArgGVM()
    {
        Assert.Equal("System.String", CallClass2M0().Result);
    }

    [Fact]
    public static void ManyArgGVM()
    {
        Assert.Equal("System.String", CallClass2M1().Result);
    }
}
