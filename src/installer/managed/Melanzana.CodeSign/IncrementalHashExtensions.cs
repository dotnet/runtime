// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Melanzana.CodeSign;

public static class IncrementalHashExtensions
{
    public static void AppendData(this IncrementalHash hash, Span<byte> buffer)
    {
        hash.AppendData(buffer.ToArray());
    }
}
