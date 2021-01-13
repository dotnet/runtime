// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public enum HttpKeepAlivePingPolicy
    {
        /// <summary>
        /// Sends keep alive ping for only when there are active streams on the connection.
        /// </summary>
        WithActiveRequests,

        /// <summary>
        /// Sends keep alive ping for whole connection lifetime.
        /// </summary>
        Always
    }
}
