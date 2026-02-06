// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncCovariantReturnTest;

public static class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        // This test validates that async methods with covariant-like return type signatures
        // do not trigger TypeLoadException during type loading.
        // The issue is that async methods generate variant methods with different signatures,
        // and the covariant return type validation was incorrectly treating these as errors.
        
        try
        {
            var derived = new Derived();
            return 100;
        }
        catch (TypeLoadException)
        {
            Console.WriteLine("FAIL: TypeLoadException thrown when loading type with async override");
            return 101;
        }
    }
}

public abstract class Base
{
    public abstract Task HandleAsync();
}

public class Derived : Base
{
    // This method appears to have a covariant return type (Task<string> instead of Task),
    // but due to the async transformation, this should be valid.
    // The async variant will have a different signature that the covariant return type
    // validator should skip.
    public override async Task<string> HandleAsync()
    {
        await Task.Yield();
        return string.Empty;
    }
}
