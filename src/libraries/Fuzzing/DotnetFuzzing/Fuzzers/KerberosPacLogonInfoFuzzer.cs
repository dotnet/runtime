// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security.Fuzzing;

namespace DotnetFuzzing.Fuzzers;

internal sealed class KerberosPacLogonInfoFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.Net.Security.Fuzzing"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        KerberosPacFuzzing.Decode(bytes);
    }
}
