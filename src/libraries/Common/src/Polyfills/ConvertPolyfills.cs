// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="Convert"/>.</summary>
internal static class ConvertPolyfills
{
    extension(Convert)
    {
        public static string ToHexStringLower(ReadOnlySpan<byte> bytes) =>
            HexConverter.ToString(bytes, HexConverter.Casing.Lower);
    }
}
