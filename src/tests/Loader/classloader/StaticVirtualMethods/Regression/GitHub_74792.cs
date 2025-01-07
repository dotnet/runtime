// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// This regression test tracks an externally reported issue where this
// code fails at runtime with a BadImageFormatException because the
// constraint resolver fails to confirm that L1 and L2 implement
// ILogic when constructing instances of the struct G, cf:
//
// https://github.com/dotnet/roslyn/issues/63617
// https://github.com/dotnet/runtime/issues/74792
//
// Apparently it was failing in an older version of the CoreCLR
// runtime, as of now (around .NET 7 RC1 timeframe) the test passes.

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        new G<L1>().Test();
        new G<L2>().Test();
    }
}

public interface ILogic{
    abstract static void Run();
}

public class L1 : ILogic{
    public static void Run(){
        Console.WriteLine("L1");
    }
}

public class L2 : ILogic{
    public static void Run(){
        Console.WriteLine("L2");
    }
}

struct G<T> where T : ILogic
{
    public void Test()
    {
        T.Run();
    }
}
