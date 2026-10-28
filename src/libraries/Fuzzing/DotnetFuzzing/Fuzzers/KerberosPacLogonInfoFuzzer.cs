// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace DotnetFuzzing.Fuzzers;

internal sealed class KerberosPacLogonInfoFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        _ = KerberosPacLogonInfo.Decode(bytes);
    }
}
