// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticServerControlCommand } from "../types";

function messageReceived(event: MessageEvent<DiagnosticServerControlCommand>): void {
    console.debug("get in loser, we're going to vegas", event.data);
}
addEventListener("message", messageReceived);
