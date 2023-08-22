// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;


public interface ITypeChecker
{
    static abstract bool Test<T>();
}

public interface IHandler
{
    bool Test<T>();
}

public class TypeChecker : ITypeChecker
{
    public static bool Test<T>() => true;
}

public class Handler<TChecker> : IHandler where TChecker : ITypeChecker
{
    public bool Test<T>() => TChecker.Test<T>();
}

public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        var handler = GetHandler();
        bool test = handler.Test<int>();
        if (test) {
            Console.WriteLine("PASSED");
            return 100;
        }

        Console.WriteLine("FAILED");
        return 101;
    }

    public static IHandler GetHandler() => new Handler<TypeChecker>();
}

