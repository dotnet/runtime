// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { PromiseController } from "../types/internal";

import { MonoConfig } from "../types";
import { mono_exit } from "./exit";
import { HostBuilder } from "./run";
import { mono_log_debug, mono_log_warn } from "./logging";
import { dotnet } from ".";
import { createPromiseController } from "./promise-controller";
import { mono_assert } from "./globals";

type TSideCarProxy = {
    worker?: Worker;
    promises: Map<number, PromiseController<any>>;
    nextPromiseId: number;
}
const sidecarProxy: TSideCarProxy = {
    promises: new Map(),
    nextPromiseId: 1
};

export async function dispatchToSideCar<TResponse>(message: SideCarMessage): Promise<TResponse> {
    const { promise, promise_control } = createPromiseController<TResponse>();
    message.promiseId = sidecarProxy.nextPromiseId++;
    sidecarProxy.promises.set(message.promiseId, promise_control);
    if (!sidecarProxy.worker) {
        // start and wait for "ready" message
        sidecarProxy.worker = new Worker(import.meta.url, { type: "module", name: "dotnet-side-car" });
        sidecarProxy.worker.onmessage = domSideHandler;
        const { promise: ready, promise_control: readyControl } = createPromiseController<any>();
        sidecarProxy.promises.set(-1, readyControl);
        await ready;
    }
    sidecarProxy.worker.postMessage(message);
    return promise;
}

function domSideHandler(e: MessageEvent) {
    const message = e.data as SideCarMessage;
    mono_log_debug(`Message from side car: ${e.data.type}`);
    const promise_control = sidecarProxy.promises.get(message.promiseId!);
    mono_assert(message.promiseId && promise_control, `Promise controller not found for: ${message}`);
    switch (message.type) {
        case SideCarMessageType.Ready:
            promise_control.resolve(undefined);
            break;
        case SideCarMessageType.Response:
            promise_control.resolve(message.response);
            break;
        case SideCarMessageType.Exit:
            sidecarProxy.worker?.terminate();
            mono_exit(message.exitCode);
            promise_control.resolve(message.exitCode);
            for (const promise of sidecarProxy.promises.values()) {
                promise.reject("Side car exited with " + message.exitCode);
            }
            break;
        case SideCarMessageType.Exception:
            sidecarProxy.worker?.terminate();
            mono_exit(-1, e.data.exception);
            promise_control.reject(e.data.exitCode);
            for (const promise of sidecarProxy.promises.values()) {
                promise.reject(e.data.exception);
            }
            break;
        default: {
            const message = `Unexpected message from side car: ${e.data}`;
            mono_log_warn(message);
            promise_control.reject(new Error(message));
        }
    }
}

export async function sidecarHandler(e: MessageEvent) {
    mono_log_debug(`Message from UI thread: ${e.data.type}`);
    const message = e.data as SideCarMessage;
    try {
        switch (message.type) {
            case SideCarMessageType.Create: {
                await createSideCar(message);
                break;
            }
            case SideCarMessageType.Run: {
                await runSideCar(message);
                break;
            }
            default:
                mono_exit(-1, `Unexpected message from main thread: ${e.data}`);
        }
    } catch (exception: any) {
        mono_log_warn("Exception while running side car", exception);
        const exceptionMessage: SideCarMessageException = {
            type: SideCarMessageType.Exception,
            promiseId: message.promiseId,
            exception: "" + exception
        };
        self.postMessage(exceptionMessage);
        mono_exit(-1, exception);
    }
}

const forwarderFunctions = [];
function forwardApi(fun: Function): number {
    const id = forwarderFunctions.length;
    forwarderFunctions.push(fun);
    return id;
}

type TSideCarApi = {
    runMain: number;
    runMainAndExit: number;
}

async function createSideCar(createMessage: SideCarMessageCreate) {
    mono_log_debug("Creating side car");
    try {
        const dotnet = new HostBuilder();
        const sidecarApi = await dotnet
            .withConfig(createMessage.config)
            .create();
        const api = {
            runtimeId: sidecarApi.runtimeId,
            runtimeBuildInfo: sidecarApi.runtimeBuildInfo,
            runMain: forwardApi(sidecarApi.runMain),
            runMainAndExit: forwardApi(sidecarApi.runMainAndExit),
        };
        forwarderFunctions.push(sidecarApi.runMain);

        const apiMessage: SideCarMessageResponse<TSideCarApi> = {
            type: SideCarMessageType.Response,
            promiseId: createMessage.promiseId,
            response: api
        };
        self.postMessage(apiMessage);
    } catch (exception: any) {
        mono_log_warn("Exception while creating side car", exception);
        const message: SideCarMessageException = {
            type: SideCarMessageType.Exception,
            promiseId: createMessage.promiseId,
            exception: "" + exception
        };
        self.postMessage(message);
        mono_exit(-1, exception);
    }
}

async function runSideCar(runMessage: SideCarMessageRun) {
    mono_log_debug("Creating side car");
    const exitCode = await dotnet.run();
    const exitMessage: SideCarMessageExit = {
        type: SideCarMessageType.Exit,
        promiseId: runMessage.promiseId,
        exitCode
    };
    self.postMessage(exitMessage);
}

export type SideCarMessage =
    | SideCarMessageReady
    | SideCarMessageCreate
    | SideCarMessageResponse<any>
    | SideCarMessageRun
    | SideCarMessageExit
    | SideCarMessageException;

export type SideCarMessageBase = {
    promiseId?: number;
}
export type SideCarMessageReady = SideCarMessageBase & {
    type: SideCarMessageType.Ready,
}

export type SideCarMessageCreate = SideCarMessageBase & {
    type: SideCarMessageType.Create,
    config: MonoConfig;
}

export type SideCarMessageResponse<TResponse> = SideCarMessageBase & {
    type: SideCarMessageType.Response,
    response: TResponse;
}

export type SideCarMessageRun = SideCarMessageBase & {
    type: SideCarMessageType.Run,
}

export type SideCarMessageException = SideCarMessageBase & {
    type: SideCarMessageType.Exception,
    exception: string;
}

export type SideCarMessageExit = SideCarMessageBase & {
    type: SideCarMessageType.Exit,
    exitCode: number;
}

export const enum SideCarMessageType {
    Ready = "dotnet-side-car-ready",
    Create = "dotnet-side-car-create",
    Response = "dotnet-side-car-response",
    Run = "dotnet-side-car-run",
    Exit = "dotnet-side-car-exit",
    Exception = "dotnet-side-car-exception",
}