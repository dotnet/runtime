// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    /// <summary>
    /// An <see cref="IWebProxy"/> capable of returning more than one proxy for a single <see cref="Uri"/>.
    /// </summary>
    internal interface IMultiWebProxy : IWebProxy
    {
        /// <summary>
        /// Gets the proxy URIs.
        /// </summary>
        public MultiProxy GetMultiProxy(Uri uri);
    }
}
