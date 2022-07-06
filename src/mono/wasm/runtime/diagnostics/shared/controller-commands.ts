// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DiagnosticMessage } from "./types";

/// Commands from the main thread to the diagnostic server
export type DiagnosticServerControlCommand =
    | DiagnosticServerControlCommandStart
    | DiagnosticServerControlCommandStop
    | DiagnosticServerControlCommandAttachToRuntime
    ;

interface DiagnosticServerControlCommandSpecific<Cmd extends string> extends DiagnosticMessage {
    cmd: Cmd;
}

export type DiagnosticServerControlCommandStart = DiagnosticServerControlCommandSpecific<"start">;
export type DiagnosticServerControlCommandStop = DiagnosticServerControlCommandSpecific<"stop">;
export type DiagnosticServerControlCommandAttachToRuntime = DiagnosticServerControlCommandSpecific<"attach_to_runtime">;

export function makeDiagnosticServerControlCommand<T extends DiagnosticServerControlCommand["cmd"]>(cmd: T): DiagnosticServerControlCommandSpecific<T> {
    return {
        type: "diagnostic_server",
        cmd: cmd,
    };
}

export type DiagnosticServerControlReply =
    | DiagnosticServerControlReplyStartupResume
    ;

export interface DiagnosticServerControlReplyStartupResume extends DiagnosticMessage {
    cmd: "startup_resume",
}

export function makeDiagnosticServerControlReplyStartupResume(): DiagnosticServerControlReplyStartupResume {
    return {
        type: "diagnostic_server",
        cmd: "startup_resume",
    };
}
