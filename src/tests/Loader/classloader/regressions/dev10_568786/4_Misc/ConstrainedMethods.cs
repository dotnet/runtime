// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

interface I<S> { string Method(S param); string Method<M>(S param); }

struct MyStruct : I<string>, I<object>
{
    public string Method(string param) { return "string"; }
    public string Method(object param) { return "object"; }
    public string Method<M>(string param) { return "GEN-string"; }
    public string Method<M>(object param) { return "GEN-object"; }
}

class Conversion1<T, U> where U : I<T>, new()
{
    public string Caller1(T param)
    {
        U instance = new U();
        return instance.Method(param);
    }

    public string Caller2(T param)
    {
        U instance = new U();
        return instance.Method<object>(param);
    }
}

class Conversion2<U> where U : I<string>, new()
{
    public string Caller1()
    {
        U instance = new U();
        return instance.Method("mystring");
    }

    public string Caller2()
    {
        U instance = new U();
        return instance.Method<object>("mystring");
    }
}

public class Test_ConstrainedMethods
{
    static string Caller1<T, U>(T param) where U : I<T>, new()
    {
        U instance = new U();
        return instance.Method(param);
    }

    static string Caller2<T, U>(T param) where U : I<T>, new()
    {
        U instance = new U();
        return instance.Method<object>(param);
    }

    static string Caller3<U>() where U : I<string>, new()
    {
        U instance = new U();
        return instance.Method("mystring");
    }

    static string Caller4<U>() where U : I<string>, new()
    {
        U instance = new U();
        return instance.Method<object>("mystring");
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int numFailures = 0;
        
        Conversion1<string, MyStruct> c1 = new Conversion1<string, MyStruct>();
        Conversion2<MyStruct> c2 = new Conversion2<MyStruct>();

        string res1 = Caller1<string, MyStruct>("mystring");
        string res2 = Caller2<string, MyStruct>("mystring");
        Console.WriteLine(res1);
        Console.WriteLine(res2);
        if(res1 != "string" && res2 != "GEN-string") numFailures++;

        res1 = Caller3<MyStruct>();
        res2 = Caller4<MyStruct>();
        Console.WriteLine(res1);
        Console.WriteLine(res2);
        if(res1 != "string" && res2 != "GEN-string") numFailures++;

        res1 = c1.Caller1("mystring");
        res2 = c1.Caller2("mystring");
        Console.WriteLine(res1);
        Console.WriteLine(res2);
        if(res1 != "string" && res2 != "GEN-string") numFailures++;

        
        res1 = c2.Caller1();
        res2 = c2.Caller2();
        Console.WriteLine(res1);
        Console.WriteLine(res2);
        if(res1 != "string" && res2 != "GEN-string") numFailures++;

        return ((numFailures == 0)?(100):(-1));
    }
}
