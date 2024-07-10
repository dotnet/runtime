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
        public const string RequestNamespace               = "System.Net.Http";
        public const string RequestWriteNameDeprecated     = RequestNamespace + ".Request";
        public const string ResponseWriteNameDeprecated    = RequestNamespace + ".Response";
        public const string ExceptionEventName             = RequestNamespace + ".Exception";
        public const string RequestActivityName            = RequestNamespace + ".HttpRequestOut";
        public const string RequestActivityStartName       = RequestActivityName + ".Start";
        public const string RequestActivityStopName        = RequestActivityName + ".Stop";

        public const string ConnectionNamespace            = "Experimental.System.Net.Http.Connections";
        public const string ConnectionSetupActivityName    = ConnectionNamespace + ".ConnectionSetup";
        public const string WaitForConnectionNamespace     = ConnectionNamespace;
        public const string WaitForConnectionActivityName  = WaitForConnectionNamespace + ".WaitForConnection";
    }
}
