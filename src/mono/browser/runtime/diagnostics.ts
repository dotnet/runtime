// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VoidPtr } from "./types/emscripten";

import { diagnosticHelpers } from "./globals";

export function ds_rt_websocket_create (urlPtr :CharPtr):number {
    return diagnosticHelpers.ds_rt_websocket_create(urlPtr);
}

export function ds_rt_websocket_send (client_socket :number, buffer:VoidPtr, bytes_to_write:number):number {
    return diagnosticHelpers.ds_rt_websocket_send(client_socket, buffer, bytes_to_write);
}

export function ds_rt_websocket_poll (client_socket :number):number {
    return diagnosticHelpers.ds_rt_websocket_poll(client_socket);
}

export function ds_rt_websocket_recv (client_socket :number, buffer:VoidPtr, bytes_to_read:number):number {
    return diagnosticHelpers.ds_rt_websocket_recv(client_socket, buffer, bytes_to_read);
}

export function ds_rt_websocket_close (client_socket :number):number {
    return diagnosticHelpers.ds_rt_websocket_close(client_socket);
}
