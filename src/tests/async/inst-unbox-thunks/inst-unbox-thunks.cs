// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class InstUnBoxThunks
{
    interface I0
    {
        Task<string> M0();
    }

    struct Struct0 : I0
    {
        public Task<string> M0()
        {
            return Task.FromResult("hi");
        }
    }

    static I0 o0;
    static async Task<string> CallStruct0M0()
    {
        o0 = new Struct0();
        return await o0.M0();
    }

    [Fact]
    public static void NoArgUnbox()
    {
        Assert.Equal("hi", CallStruct0M0().Result);
    }


    interface I1
    {
        Task<string> M0();
    }

    struct Struct1<T> : I1
    {
        public Task<string> M0()
        {
            return Task.FromResult(typeof(T).ToString());
        }
    }

    static I1 o1;
    static async Task<string> CallStruct1M0()
    {
        o1 = new Struct1<string>();
        return await o1.M0();
    }

    [Fact]
    public static void NoArgGenericUnbox()
    {
        Assert.Equal("System.String", CallStruct1M0().Result);
    }
}
