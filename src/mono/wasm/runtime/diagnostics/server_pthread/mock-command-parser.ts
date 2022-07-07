// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { ProtocolClientCommandBase, isDiagnosticCommandBase } from "./protocol-client-commands";

export default function parseCommand(x: string): ProtocolClientCommandBase | null {
    let command: object;
    try {
        command = JSON.parse(x);
    } catch (err) {
        console.warn("error while parsing JSON diagnostic server protocol command", err);
        return null;
    }
    if (isDiagnosticCommandBase(command)) {
        return command;
    } else {
        console.warn("received a JSON diagnostic server protocol command without command_set or command", command);
        return null;
    }
}
