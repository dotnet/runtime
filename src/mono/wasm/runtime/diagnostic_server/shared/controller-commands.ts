// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventPipeSessionDiagnosticServerID, EventPipeSessionIDImpl, DiagnosticMessage } from "./types";


export type DiagnosticServerControlCommand =
    DiagnosticServerControlCommandStart
    | DiagnosticServerControlCommandSetSessionID
    ;

export interface DiagnosticServerControlCommandStart extends DiagnosticMessage {
    cmd: "start",
    url: string, // websocket url to connect to
}

export interface DiagnosticServerControlCommandSetSessionID extends DiagnosticMessage {
    cmd: "set_session_id";
    diagnostic_server_id: EventPipeSessionDiagnosticServerID;
    session_id: EventPipeSessionIDImpl;
}
