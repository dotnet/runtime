// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { isMonoThreadMessage } from "../../pthreads";
import type { MonoThreadMessage } from "../../types/internal";

// Messages from the main thread to the diagnostic server thread
export interface DiagnosticMessage extends MonoThreadMessage {
    type: "diagnostic_server";
    cmd: string;
}

export function isDiagnosticMessage(x: unknown): x is DiagnosticMessage {
    return isMonoThreadMessage(x) && x.type === "diagnostic_server";
}

/// Commands from the diagnostic server controller on the main thread to the diagnostic server
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
