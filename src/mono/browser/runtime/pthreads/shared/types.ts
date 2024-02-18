// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { PThreadInfo, WorkerToMainMessageType } from "../../types/internal";

/// Messages sent from the main thread using Worker.postMessage or from the worker using DedicatedWorkerGlobalScope.postMessage
/// should use this interface.  The message event is also used by emscripten internals (and possibly by 3rd party libraries targeting Emscripten).
/// We should just use this to establish a dedicated MessagePort for Mono's uses.
export interface MonoWorkerToMainMessage {
    monoCmd: WorkerToMainMessageType;
    info: PThreadInfo;
    port?: MessagePort;
}
