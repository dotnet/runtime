// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Microsoft.NET.HostModel;

internal static class Base64Url
{
    internal static string EncodeToString(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('/', '_')
            .Replace('+', '-')
            .Replace("=", "");
    }
}
