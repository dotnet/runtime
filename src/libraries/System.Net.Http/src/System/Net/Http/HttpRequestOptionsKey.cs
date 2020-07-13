// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Net.Http
{
    public readonly struct HttpRequestOptionsKey<TKey>
    {
        private readonly TKey _key;
        public HttpRequestOptionsKey(TKey key)
        {
            _key = key;
        }
        public TKey Key => _key;
    }
}
