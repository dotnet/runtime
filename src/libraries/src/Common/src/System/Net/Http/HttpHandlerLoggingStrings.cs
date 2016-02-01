// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    /// <summary>
    /// Defines names of DiagnosticListener and Write events for WinHttpHandler, CurlHandler, and HttpHandlerToFilter.
    /// </summary>
    internal static class HttpHandlerLoggingStrings
    {
        public const string DiagnosticListenerName = "HttpHandlerDiagnosticListener";
        public const string RequestWriteName = "System.Net.Http.Request";
        public const string ResponseWriteName = "System.Net.Http.Response";
    }
}
