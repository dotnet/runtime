// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public sealed class HttpRequestOptions : Dictionary<string, object?>
    {
        public bool TryGetValue<TValue>(HttpRequestOptionsKey<TValue> key, [MaybeNullWhen(false)] out TValue value)
        {
            if (base.TryGetValue(key.Key, out object? _value) && _value is TValue tvalue)
            {
                value = tvalue;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public void Set<TValue>(HttpRequestOptionsKey<TValue> key, TValue value)
        {
            base.Add(key.Key, value);
        }
    }
}