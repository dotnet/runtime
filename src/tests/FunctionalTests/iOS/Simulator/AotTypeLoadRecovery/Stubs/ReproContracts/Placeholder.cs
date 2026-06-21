// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ReproContracts;

internal static class Placeholder
{
}

public static class ContractBridge
{
    public static T FromPointer<T>(nint pointer)
        where T : class
        => default!;
}

public sealed class MissingFieldOwner
{
}
