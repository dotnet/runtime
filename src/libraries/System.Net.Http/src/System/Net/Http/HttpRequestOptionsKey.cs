// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Net.Http
{
    /// <summary>
    /// Represents a key in the options for an HTTP request.
    /// </summary>
    /// <typeparam name="TValue">The type of the value of the option.</typeparam>
    public readonly struct HttpRequestOptionsKey<TValue>
    {
        /// <summary>
        /// Gets the name of the option.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Initializes a new instance of the HttpRequestOptionsKey using the supplied key name.
        /// </summary>
        /// <param name="key">Name of the HTTP request option.</param>
        public HttpRequestOptionsKey(string key)
        {
            Key = key;
        }
    }
}
