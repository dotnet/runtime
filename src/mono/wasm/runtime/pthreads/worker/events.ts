
import type { pthread_ptr } from "../shared";

export const dotnetPthreadCreated = "dotnet:pthread:created" as const;
export const dotnetPthreadAttached = "dotnet:pthread:attached" as const;

/// Events emitted on the current worker when Emscripten uses it to run a pthread.
export interface WorkerThreadEventMap {
    /// Emitted on the current worker when Emscripten first creates a pthread on the current worker.
    /// May be fired multiple times because Emscripten reuses workers to run a new pthread after the current one is finished.
    [dotnetPthreadCreated]: WorkerThreadEvent;
    // Emitted on the current worker when a pthread attaches to Mono.
    // Threads may attach and detach to Mono multiple times during their lifetime.
    [dotnetPthreadAttached]: WorkerThreadEvent;
}

interface WorkerThreadEventDetail {
    pthread_ptr: pthread_ptr;
    portToMain: MessagePort;
}

export class WorkerThreadEvent extends CustomEvent<WorkerThreadEventDetail> {
    constructor(type: keyof WorkerThreadEventMap, pthread_ptr: pthread_ptr, portToMain: MessagePort) {
        super(type, { detail: { pthread_ptr, portToMain } });
    }
    get pthread_ptr(): pthread_ptr {
        return this.detail.pthread_ptr;
    }
    get portToMain(): MessagePort {
        return this.detail.portToMain;
    }
}


export interface WorkerThreadEventTarget extends EventTarget {
    dispatchEvent(event: WorkerThreadEvent): boolean;

    addEventListener<K extends keyof WorkerThreadEventMap>(type: K, listener: ((this: WorkerThreadEventTarget, ev: WorkerThreadEventMap[K]) => any) | null, options?: boolean | AddEventListenerOptions): void;
    addEventListener(type: string, callback: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions): void;
}

// export class WorkerThreadEventTargetImpl extends EventTarget implements WorkerThreadEventTarget {
//     constructor() {
//         super();
//     }
//     dispatchEvent(event: Event): boolean {
//         return super.dispatchEvent(event);
//     }

//     addEventListener(type: string, callback: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions): void {
//         super.addEventListener(type, callback, options);
//     }
// }

export function makeWorkerThreadEvent(type: keyof WorkerThreadEventMap, pthread_ptr: pthread_ptr, port: MessagePort): WorkerThreadEvent {
    return new WorkerThreadEvent(type, pthread_ptr, port);
}
