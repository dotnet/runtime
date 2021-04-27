// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Http
{
    internal static partial class HttpUtilities
    {
        internal static bool IsSupportedNonSecureScheme(string scheme) =>
            string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase)
            || IsBlobScheme(scheme)
            || IsNonSecureWebSocketScheme(scheme);

        internal static bool IsBlobScheme(string scheme) =>
            string.Equals(scheme, "blob", StringComparison.OrdinalIgnoreCase);

        internal static string InvalidUriMessage => SR.net_http_client_http_browser_baseaddress_required;
    }
}
