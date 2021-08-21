// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Net.Http
{
    public readonly struct HttpRequestOptionsKey<TValue>
    {
        public string Key { get; }
        ///<summary>
        ///Initializes a new instance of the <see cref="HttpRequestOptionsKey"/> struct using the supplied string key.
        ///</summary>
        public HttpRequestOptionsKey(string key)
        {
            Key = key;
        }
    }
}
