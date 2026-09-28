// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncCovariantReturnTest;

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        // This test validates that async methods with covariant-like return type signatures
        // do not trigger TypeLoadException during type loading.
        //
        // Background: The C# compiler allows overriding a Task method with an async Task<T> method
        // because the async keyword changes the method's semantics. The compiler generates
        // async variant methods with different signatures to support the async calling convention.
        // These variant methods have the unwrapped return type (T instead of Task<T>).
        //
        // The issue was that the runtime's covariant return type validator was incorrectly
        // treating these synthetic async variant methods as invalid overrides, causing a
        // TypeLoadException even though the code is valid and compiles successfully.
        //
        // This test ensures that such valid code can be loaded without throwing.
        
        GC.KeepAlive(new Derived());
    }
}

public abstract class Base
{
    public abstract Task HandleAsync();
}

public class Derived : Base
{
    // This override is valid C# code that compiles successfully.
    // The C# compiler allows overriding Task with async Task<T> because the async keyword
    // changes how the method is compiled. However, the runtime generates async variant methods
    // with different signatures (returning T instead of Task<T>) to support the async calling
    // convention. The covariant return type validator should skip these synthetic methods.
    public override async Task<string> HandleAsync()
    {
        await Task.Yield();
        return string.Empty;
    }
}
