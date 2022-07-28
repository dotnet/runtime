// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    /// <summary>
    /// Defines names of DiagnosticListener and Write events for DiagnosticHandler
    /// </summary>
    internal static class DiagnosticsHandlerLoggingStrings
    {
        public const string DiagnosticListenerName         = "HttpHandlerDiagnosticListener";
        public const string Namespace                      = "System.Net.Http";
        public const string RequestWriteNameDeprecated     = Namespace + ".Request";
        public const string ResponseWriteNameDeprecated    = Namespace + ".Response";
        public const string ExceptionEventName             = Namespace + ".Exception";
        public const string ActivityName                   = Namespace + ".HttpRequestOut";
        public const string ActivityStartName              = ActivityName + ".Start";
        public const string ActivityStopName               = ActivityName + ".Stop";
    }
}
