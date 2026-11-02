// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange } from "./types";
import { InternalExchangeIndex } from "../types";
import { dotnetApi } from "./cross-module";
import { createMemoryViews } from "../../Common/JavaScript/loader/shared-signaling";
import { NativeOperation } from "../../Common/JavaScript/loader/worker-native";
import {
    installMemoryAPI, installSharedMemoryViews, zeroRegion, arrayBufferNeedsCopy, viewOrCopy,
} from "../../System.Native.Browser/utils/memory";
import {
    stringsInit, utf16ToString, stringToUTF16, stringToUTF16Ptr, stringToUTF8Ptr, stringToUTF8, utf8ToStringRelaxed,
} from "../../System.Native.Browser/utils/strings";
import { registerRuntime } from "../../System.Native.Browser/utils/runtime-list";

export function initializeWorkerUI(internals: InternalExchange): void {
    const context = internals[InternalExchangeIndex.WorkerRuntimeContext];
    if (!context?.memory) throw new Error("withWorker(): UI shared memory is missing");
    const memory = context.memory;
    let views = createMemoryViews(memory.buffer);
    installSharedMemoryViews(() => {
        if (views.u8.buffer.byteLength !== memory.buffer.byteLength) views = createMemoryViews(memory.buffer);
        return views;
    });
    installMemoryAPI(dotnetApi);
    stringsInit(true);
    registerRuntime(dotnetApi);
    class ExitStatus extends Error {
        constructor(public status: number) {
            super(`Process exited with code ${status}`);
        }
    }
    internals[InternalExchangeIndex.BrowserUtilsExportsTable] = [
        utf16ToString, stringToUTF16, stringToUTF16Ptr, stringToUTF8Ptr, stringToUTF8,
        utf8ToStringRelaxed, zeroRegion, arrayBufferNeedsCopy, viewOrCopy,
        async () => {}, // VM timers are owned by the native worker.
        (code: number) => dotnetApi.exit(code),
        () => ExitStatus,
        async () => {
            context.invoke(NativeOperation.RunBackgroundTimers);
        },
    ];
}
