// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    /// <summary>
    /// Defines names of DiagnosticListener and Write events for DiagnosticHandler
    /// </summary>
    internal static class DiagnosticsHandlerLoggingStrings
    {
        public const string DiagnosticListenerName = "HttpHandlerDiagnosticListener";
        public const string RequestWriteNameDeprecated = "System.Net.Http.Request";
        public const string ResponseWriteNameDeprecated = "System.Net.Http.Response";

        public const string ExceptionEventName = "System.Net.Http.Exception";
        public const string ActivityName = "System.Net.Http.HttpRequestOut";
        public const string ActivityStartName = "System.Net.Http.HttpRequestOut.Start";
    }
}
