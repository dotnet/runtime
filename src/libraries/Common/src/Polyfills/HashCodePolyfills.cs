// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="HashCode"/>.</summary>
internal static class HashCodePolyfills
{
    public static void AddBytes(this ref HashCode hashCode, ReadOnlySpan<byte> value)
    {
        while (value.Length >= sizeof(int))
        {
            hashCode.Add(BitConverter.ToInt32(value));
            value = value.Slice(sizeof(int));
        }

        foreach (byte b in value)
        {
            hashCode.Add(b);
        }
    }
}
