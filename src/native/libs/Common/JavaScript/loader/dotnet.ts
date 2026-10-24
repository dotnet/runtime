// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **JavaScript module** that would become of `dotnet.js`.
 * It implements host for the browser together with `src/native/corehost/browserhost`.
 * It exposes the public JS runtime APIs that is implemented in `dotnet.runtime.ts`.
 * It's good to keep this file small.
 */

import type { DotnetHostBuilder } from "./types";

import { HostBuilder } from "./host-builder";
import { initPolyfillsEarly } from "./polyfills";
import { exit as loaderExit } from "./exit";
import { dotnetInitializeModule } from ".";
import { ENVIRONMENT_IS_WEB_WORKER, ENVIRONMENT_IS_SIDECAR } from "./per-module";
import { waitForInitMessage, bootstrapWorkerRuntime } from "./worker-runtime";
import { dotnetApi } from "../cross-module";

dotnetInitializeModule();
await initPolyfillsEarly();

const exit = (exitCode: number, reason: any): void => {
    if (dotnetApi && dotnetApi.exit) {
        dotnetApi.exit(exitCode, reason);
    } else {
        loaderExit(exitCode, reason);
    }
};

let _dotnet: DotnetHostBuilder | undefined;
if (ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_SIDECAR) {
    // Dedicated worker spawned by withWorker(): wait for init message
    // from main thread, then bootstrap the runtime with shared WebAssembly.Memory.
    // ENVIRONMENT_IS_SIDECAR workers (Emscripten pthreads) continue normal startup.
    const initMsg = await waitForInitMessage();
    await bootstrapWorkerRuntime(initMsg);
    // dotnet export is undefined in worker context
} else {
    _dotnet = new HostBuilder() as DotnetHostBuilder;
    _dotnet.withConfig(/*! dotnetBootConfig */{});
}

export const dotnet = _dotnet;
export { exit };
