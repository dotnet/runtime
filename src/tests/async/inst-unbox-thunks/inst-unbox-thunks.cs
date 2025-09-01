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
        Task<string> M1(object a0, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8);
    }

    struct Struct0 : I0
    {
        public async Task<string> M0()
        {
            await Task.Yield();
            return "hi";
        }

        public async Task<string> M1(object a0, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
        {
            await Task.Yield();
            return "hello";
        }
    }

    static I0 o00;
    static async Task<string> CallStruct0M0()
    {
        o00 = new Struct0();
        return await o00.M0();
    }

    static I0 o01;
    static async Task<string> CallStruct0M1()
    {
        o01 = new Struct0();
        return await o01.M1(default, default, default, default, default, default, default, default, default);
    }

    struct Struct1<T> : I0
    {
        public async Task<string> M0()
        {
            await Task.Yield();
            return typeof(T).ToString();
        }

        public async Task<string> M1(object a0, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
        {
            await Task.Yield();
            return typeof(T).ToString();
        }
    }

    static I0 o10;
    static async Task<string> CallStruct1M0()
    {
        o10 = new Struct1<string>();
        return await o10.M0();
    }

    static I0 o11;
    static async Task<string> CallStruct1M1()
    {
        o11 = new Struct1<string>();
        return await o11.M1(default, default, default, default, default, default, default, default, default);
    }

    class Box<U> where U : I0
    {
        public U f;
    }

    static async Task<string> CallStruct1M0Field<T>(Box<T> arg) where T : I0
    {
        return await arg.f.M0();
    }

    static async Task<string> CallStruct1M1Field<T>(Box<T> arg) where T : I0
    {
        return await arg.f.M1(default, default, default, default, default, default, default, default, default);
    }

    static Box<Struct1<string>> b1 = new();
    static async Task<string> CallStruct1M0b()
    {
        b1.f = new Struct1<string>();
        return await CallStruct1M0Field(b1);
    }

    static Box<Struct1<string>> b2 = new();
    static async Task<string> CallStruct1M1b()
    {
        b2.f = new Struct1<string>();
        return await CallStruct1M1Field(b2);
    }


    [Fact]
    public static void NoArgUnbox()
    {
        Assert.Equal("hi", CallStruct0M0().Result);
    }

    [Fact]
    public static void ManyArgUnbox()
    {
        Assert.Equal("hello", CallStruct0M1().Result);
    }

    [Fact]
    public static void NoArgGenericUnbox()
    {
        Assert.Equal("System.String", CallStruct1M0().Result);
    }

    [Fact]
    public static void ManyArgGenericUnbox()
    {
        Assert.Equal("System.String", CallStruct1M1().Result);
    }

    [Fact]
    public static void NoArgGenericInstantiating()
    {
        Assert.Equal("System.String", CallStruct1M0b().Result);
    }

    [Fact]
    public static void ManyArgGenericInstantiating()
    {
        Assert.Equal("System.String", CallStruct1M1b().Result);
    }
    
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
