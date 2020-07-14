// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public struct MyStruct<TRequest, TResponse>
{
    int _id;
    public MyStruct(int id) { _id = id; }
    public override string ToString() => this.GetType().ToString() + " = " + _id;
}

public struct GenStruct<T> { }

public sealed class MyStructWrapper<TRequest, TResponse>
{
    public MyStruct<TRequest, TResponse> _field;

    public MyStructWrapper(MyStruct<TRequest, TResponse> value)
    {
        _field = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override string ToString() => _field.ToString();
}

public abstract class BaseStructCreator
{
    public abstract MyStructWrapper<TRequest, TResponse> GetMyStructWrapper<TRequest, TResponse>() where TRequest : class;
}

public class StructCreator : BaseStructCreator
{
    public override MyStructWrapper<TRequest, TResponse> GetMyStructWrapper<TRequest, TResponse>()
    {
        return new MyStructWrapper<TRequest, TResponse>(CreateCall<TRequest, TResponse>());
    }
    protected virtual MyStruct<TRequest, TResponse> CreateCall<TRequest, TResponse>() where TRequest : class
    {
        return new MyStruct<TRequest, TResponse>(123);
    }
}

class DerivedCreator : StructCreator
{
    protected override MyStruct<TRequest, TResponse> CreateCall<TRequest, TResponse>()
    {
        return new MyStruct<TRequest, TResponse>(456);
    }
}

public class Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static string RunTest()
    {
        var creator = new DerivedCreator();
        var wrapper = creator.GetMyStructWrapper<Exception, GenStruct<string>>();
        return wrapper.ToString();
    }
    public static int Main()
    {
        Console.WriteLine("Expected: MyStruct`2[System.Exception,GenStruct`1[System.String]] = 456");

        string result = RunTest();
        Console.WriteLine("Actual  : " + result);
        
        string expected = "MyStruct`2[System.Exception,GenStruct`1[System.String]] = 456";
        return result == expected ? 100 : -1;
    }
}
