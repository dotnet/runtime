// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { VoidPtr } from "../../types/emscripten";
import * as Memory from "../../memory";


/// One-reader, one-writer, size 1 queue for messages from an EventPipe streaming thread to
// the diagnostic server thread that owns the WebSocket.

// EventPipeStreamQueue has 3 memory words that are used to communicate with the streaming thread:
// struct MonoWasmEventPipeStreamQueue {
//    union { void* buf; intptr_t close_msg; /* -1 */ };
//    int32_t count;
//    volatile int32_t buf_full;
// }
//
// To write, the streaming thread:
//  1. sets buf (or close_msg) and count, and then atomically sets buf_full.
//  2. queues mono_wasm_diagnostic_server_stream_signal_work_available to run on the diagnostic server thread
//  3. waits for buf_full to be 0.
//
// Note this is a little bit fragile if there are multiple writers.
// There _are_ multiple writers - when the streaming session first starts, either the diagnostic server thread
// or the main thread write to the queue before the streaming thread starts.  But those actions are
// implicitly serialized because the streaming thread isn't started until the writes are done.

const BUF_OFFSET = 0;
const COUNT_OFFSET = 4;
const WRITE_DONE_OFFSET = 8;

type SyncSendBuffer = (buf: VoidPtr, len: number) => void;
type SyncSendClose = () => void;

const STREAM_CLOSE_SENTINEL = -1;

export class StreamQueue {
    readonly workAvailable: EventTarget = new EventTarget();
    readonly signalWorkAvailable = this.signalWorkAvailableImpl.bind(this);

    constructor(readonly queue_addr: VoidPtr, readonly syncSendBuffer: SyncSendBuffer, readonly syncSendClose: SyncSendClose) {
        this.workAvailable.addEventListener("workAvailable", this.onWorkAvailable.bind(this));
    }

    private get buf_addr(): VoidPtr {
        return <any>this.queue_addr + BUF_OFFSET;
    }
    private get count_addr(): VoidPtr {
        return <any>this.queue_addr + COUNT_OFFSET;
    }
    private get buf_full_addr(): VoidPtr {
        return <any>this.queue_addr + WRITE_DONE_OFFSET;
    }

    /// called from native code on the diagnostic thread when the streaming thread queues a call to notify the
    /// diagnostic thread that it can send the buffer.
    wakeup(): void {
        queueMicrotask(this.signalWorkAvailable);
    }

    workAvailableNow(): void {
        // process the queue immediately, rather than waiting for the next event loop tick.
        this.onWorkAvailable();
    }

    private signalWorkAvailableImpl(this: StreamQueue): void {
        this.workAvailable.dispatchEvent(new Event("workAvailable"));
    }

    private onWorkAvailable(this: StreamQueue /*,event: Event */): void {
        const buf = Memory.getI32(this.buf_addr) as unknown as VoidPtr;
        const intptr_buf = buf as unknown as number;
        if (intptr_buf === STREAM_CLOSE_SENTINEL) {
            // special value signaling that the streaming thread closed the queue.
            this.syncSendClose();
        } else {
            const count = Memory.getI32(this.count_addr);
            Memory.setI32(this.buf_addr, 0);
            if (count > 0) {
                this.syncSendBuffer(buf, count);
            }
        }
        /* buffer is now not full */
        Memory.Atomics.storeI32(this.buf_full_addr, 0);
        /* wake up the writer thread */
        Memory.Atomics.notifyI32(this.buf_full_addr, 1);
    }
}

// maps stream queue addresses to StreamQueue instances
const streamQueueMap = new Map<VoidPtr, StreamQueue>();

export function allocateQueue(nativeQueueAddr: VoidPtr, syncSendBuffer: SyncSendBuffer, syncSendClose: SyncSendClose): StreamQueue {
    const queue = new StreamQueue(nativeQueueAddr, syncSendBuffer, syncSendClose);
    streamQueueMap.set(nativeQueueAddr, queue);
    return queue;
}

export function closeQueue(nativeQueueAddr: VoidPtr): void {
    streamQueueMap.delete(nativeQueueAddr);
    // TODO: remove the event listener?
}

// called from native code on the diagnostic thread by queueing a call from the streaming thread.
export function mono_wasm_diagnostic_server_stream_signal_work_available(nativeQueueAddr: VoidPtr, current_thread: number): void {
    const queue = streamQueueMap.get(nativeQueueAddr);
    if (queue) {
        if (current_thread === 0) {
            queue.wakeup();
        } else {
            queue.workAvailableNow();
        }
    }
}
