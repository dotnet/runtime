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
            value = default(TValue);
            var ourValueResult = base.TryGetValue(key.Key, out object? _value);
            if (ourValueResult && _value is TValue)
            {
                value = (TValue)_value;
                return true;
            }
            return false;
        }

        public void Set<TValue>(HttpRequestOptionsKey<TValue> key, TValue value)
        {
            base.Add(key.Key, value);
        }
    }
}