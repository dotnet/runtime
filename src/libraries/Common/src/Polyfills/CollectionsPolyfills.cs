// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic;

/// <summary>Provides downlevel polyfills for <see cref="KeyValuePair{TKey, TValue}"/>.</summary>
internal static class KeyValuePairPolyfills
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey key, out TValue value)
    {
        key = source.Key;
        value = source.Value;
    }
}
