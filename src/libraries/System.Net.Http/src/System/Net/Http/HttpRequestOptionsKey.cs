// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Net.Http
{
    public readonly struct HttpRequestOptionsKey<TValue>
    {
        private readonly string _key;
        public HttpRequestOptionsKey(string key)
        {
            _key = key;
        }
        public string Key => _key;
    }
}
