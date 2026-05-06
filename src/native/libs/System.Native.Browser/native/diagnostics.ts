// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CharPtr, VoidPtr } from "../types";
import { dotnetDiagnosticsExports } from "../utils/cross-module";

export function ds_rt_websocket_create(urlPtr: CharPtr): number {
    return dotnetDiagnosticsExports.ds_rt_websocket_create(urlPtr);
}

export function ds_rt_websocket_send(clientSocket: number, buffer: VoidPtr, bytesToWrite: number): number {
    return dotnetDiagnosticsExports.ds_rt_websocket_send(clientSocket, buffer, bytesToWrite);
}

export function ds_rt_websocket_poll(clientSocket: number): number {
    return dotnetDiagnosticsExports.ds_rt_websocket_poll(clientSocket);
}

export function ds_rt_websocket_recv(clientSocket: number, buffer: VoidPtr, bytesToRead: number): number {
    return dotnetDiagnosticsExports.ds_rt_websocket_recv(clientSocket, buffer, bytesToRead);
}

export function ds_rt_websocket_close(clientSocket: number): number {
    return dotnetDiagnosticsExports.ds_rt_websocket_close(clientSocket);
}
