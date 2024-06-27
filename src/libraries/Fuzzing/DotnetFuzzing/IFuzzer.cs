// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DotnetFuzzing;

internal interface IFuzzer
{
    /// <summary>Friendly name to identify this fuzz target in OneFuzz configuration.</summary>
    string Name => GetType().Name;

    /// <summary>List of assemblies that should be instrumented.
    /// If the code under test is only in CoreLib, you may return an empty array.</summary>
    string[] TargetAssemblies { get; }

    /// <summary>List of prefixes (CoreLib namespaces/types) that should be instrumented.
    /// If the code under test is outside CoreLib, you may return an empty array.</summary>
    string[] TargetCoreLibPrefixes { get; }

    /// <summary>Optional name of the dictionary to use to better guide the fuzzer.</summary>
    string? Dictionary => null;

    /// <summary>Entry point for the fuzzer. Should exercise code paths in <see cref="TargetAssemblies"/> and/or <see cref="TargetCoreLibPrefixes"/>.</summary>
    void FuzzTarget(ReadOnlySpan<byte> bytes);
}
