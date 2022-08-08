// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type EventPipeSessionIDImpl = number;

/// Options to configure EventPipe sessions that will be created and started at runtime startup
export type DiagnosticOptions = {
    /// An array of sessions to start at runtime startup
    sessions?: EventPipeSessionOptions[],
    /// If true, the diagnostic server will be started.  If "wait", the runtime will wait at startup until a diagnsotic session connects to the server
    server?: DiagnosticServerOptions,
}

/// Options to configure the event pipe session
/// The recommended method is to MONO.diagnostics.SesisonOptionsBuilder to create an instance of this type
export interface EventPipeSessionOptions {
    /// Whether to collect additional details (such as method and type names) at EventPipeSession.stop() time (default: true)
    /// This is required for some use cases, and may allow some tools to better understand the events.
    collectRundownEvents?: boolean;
    /// The providers that will be used by this session.
    /// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    providers: string;
}

/// Options to configure the diagnostic server
export type DiagnosticServerOptions = {
    connectUrl: string, // websocket URL to connect to.
    suspend: string | boolean, // if true, the server will suspend the app when it starts until a diagnostic tool tells the runtime to resume.
}
