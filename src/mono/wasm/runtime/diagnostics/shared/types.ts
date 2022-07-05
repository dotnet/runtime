// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { MonoThreadMessage } from "../../pthreads/shared";
import { isMonoThreadMessage } from "../../pthreads/shared";

export type EventPipeSessionIDImpl = number;

export interface DiagnosticMessage extends MonoThreadMessage {
    type: "diagnostic_server";
    cmd: string;
}

export function isDiagnosticMessage(x: unknown): x is DiagnosticMessage {
    return isMonoThreadMessage(x) && x.type === "diagnostic_server";
}


