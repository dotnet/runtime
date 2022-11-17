// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type EventPipeSessionIDImpl = number;

/// Options to configure EventPipe sessions that will be created and started at runtime startup
export type DiagnosticOptions = {
    /// If true, the diagnostic server will be started.  If "wait", the runtime will wait at startup until a diagnsotic session connects to the server
    server?: DiagnosticServerOptions,
}

/// Options to configure the diagnostic server
export type DiagnosticServerOptions = {
    connectUrl: string, // websocket URL to connect to.
    suspend: string | boolean, // if true, the server will suspend the app when it starts until a diagnostic tool tells the runtime to resume.
}
