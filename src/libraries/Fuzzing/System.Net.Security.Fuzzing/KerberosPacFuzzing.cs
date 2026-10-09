// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security.Fuzzing;

public static class KerberosPacFuzzing
{
    public static void Decode(ReadOnlySpan<byte> bytes)
    {
        _ = KerberosPacLogonInfo.Decode(bytes);
    }
}
