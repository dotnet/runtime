// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VoidPtr } from "../../Common/JavaScript/types/emscripten";

import { createDiagConnectionJs, initializeJsClient, serverSession } from "./diagnostic-server-js";
import { createDiagConnectionWs } from "./diagnostic-server-ws";
import { advertise } from "./client-commands";
import { IDiagnosticConnection } from "./types";
import { dotnetApi, Module } from "./cross-module";

let socketHandles: Map<number, IDiagnosticConnection> = undefined as any;
let nextSocketHandle = 1;
let urlOverride: string | undefined = undefined;

export function ds_rt_websocket_create(urlPtr: CharPtr): number {
    if (!socketHandles) {
        socketHandles = new Map<number, IDiagnosticConnection>();
    }
    let url;
    if (!urlOverride) {
        url = Module.UTF8ToString(urlPtr);
    } else {
        url = urlOverride;
    }

    const socketHandle = nextSocketHandle++;
    const isWebSocket = url.startsWith("ws://") || url.startsWith("wss://");
    const wrapper = isWebSocket
        ? createDiagConnectionWs(socketHandle, url)
        : createDiagConnectionJs(socketHandle, url);
    socketHandles.set(socketHandle, wrapper);
    return socketHandle;
}

export function ds_rt_websocket_send(clientSocket: number, buffer: VoidPtr, bytesToWrite: number): number {
    const wrapper = socketHandles.get(clientSocket);
    if (!wrapper) {
        return -1;
    }
    const view = dotnetApi.localHeapViewU8();
    const bufferPtr = buffer as any >>> 0;
    const message = view.slice(bufferPtr, bufferPtr + bytesToWrite);
    return wrapper.send(message);
}

export function ds_rt_websocket_poll(clientSocket: number): number {
    const wrapper = socketHandles.get(clientSocket);
    if (!wrapper) {
        return 0;
    }
    return wrapper.poll();
}

export function ds_rt_websocket_recv(clientSocket: number, buffer: VoidPtr, bytesToRead: number): number {
    const wrapper = socketHandles.get(clientSocket);
    if (!wrapper) {
        return -1;
    }
    const bufferPtr: VoidPtr = buffer as any >>> 0 as any;
    return wrapper.recv(bufferPtr, bytesToRead);
}

export function ds_rt_websocket_close(clientSocket: number): number {
    const wrapper = socketHandles.get(clientSocket);
    if (!wrapper) {
        return -1;
    }
    socketHandles.delete(clientSocket);
    return wrapper.close();
}

// this will take over the existing connection to JS and send new advert message to WS client
// use dotnet-dsrouter server-websocket -v trace
export function connectDSRouter(url: string): void {
    if (!serverSession) {
        throw new Error("No active session to reconnect");
    }

    // make sure new sessions hit the new URL
    urlOverride = url;

    const oldWrapper = socketHandles.get(serverSession.clientSocket);
    if (oldWrapper) {
        oldWrapper.close();
    }
    const wrapper = createDiagConnectionWs(serverSession.clientSocket, url);
    socketHandles.set(serverSession.clientSocket, wrapper);
    wrapper.send(advertise());
}

export function initializeDS() {
    /* WASM-TODO, do this only when <EnableDiagnostics>true</EnableDiagnostics>
    const loaderConfig = dotnetApi.getConfig();
    const diagnosticPorts = "DOTNET_DiagnosticPorts";
    if (!loaderConfig.environmentVariables![diagnosticPorts]) {
        loaderConfig.environmentVariables![diagnosticPorts] = "js://ready";
    }
    */
    initializeJsClient();
}
