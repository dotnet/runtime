// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { VoidPtr } from "../../types/emscripten";
import * as Memory from "../../memory";


/// One-reader, one-writer, size 1 queue for messages from an EventPipe streaming thread to
// the diagnostic server thread that owns the WebSocket.

// EventPipeStreamQueue has 3 memory words that are used to communicate with the streaming thread:
// struct MonoWasmEventPipeStreamQueue {
//    void* buf;
//    int32_t count;
//    volatile int32_t write_done;
// }
//
// To write, the streaming thread does:
//
//  int32_t queue_write (MonoWasmEventPipeSteamQueue *queue, uint8_t *buf, int32_t len, int32_t *bytes_written)
//  {
//   queue->buf = buf;
//   queue->count = len;
//   //WISH: mono_wasm_memory_atomic_notify (&queue->wakeup_write, 1);  //   __builtin_wasm_memory_atomic_notify((int*)addr, count);
//   emscripten_dispatch_to_thread (diagnostic_thread_id, wakeup_stream_queue, queue);
//   int r = mono_wasm_memory_atomic_wait (&queue->wakeup_write_done, 0, -1);  // __builtin_wasm_memory_atomic_wait((int*)addr, expected, timeout); // returns 0 ok, 1 not_equal, 2 timed out
//   if (G_UNLIKELY (r != 0) {
//     return -1;
//   }
//   result = Atomics.load (wakeup_write_done); // 0 or errno
//   if (bytes_writen) *bytes_written = len;
//   mono_atomic_store_int32 (&queue->wakeup_write_done, 0);
//
//  This would be a lot less hacky if more browsers implemented Atomics.waitAsync.
//  Then we wouldn't have to use emscripten_dispatch_to_thread, and instead the diagnostic server could
//  just call Atomics.waitAsync to wait for the streaming thread to write.

const BUF_OFFSET = 0;
const COUNT_OFFSET = 4;
const WRITE_DONE_OFFSET = 8;

type SyncSendBuffer = (buf: VoidPtr, len: number) => void;

export class StreamQueue {
    readonly workAvailable: EventTarget = new EventTarget();
    readonly signalWorkAvailable = this.signalWorkAvailableImpl.bind(this);

    constructor(readonly queue_addr: VoidPtr, readonly syncSendBuffer: SyncSendBuffer) {
        this.workAvailable.addEventListener("workAvailable", this.onWorkAvailable.bind(this));
    }

    private get wakeup_write_addr(): VoidPtr {
        return <any>this.queue_addr + BUF_OFFSET;
    }
    private get count_addr(): VoidPtr {
        return <any>this.queue_addr + COUNT_OFFSET;
    }
    private get wakeup_write_done_addr(): VoidPtr {
        return <any>this.queue_addr + WRITE_DONE_OFFSET;
    }

    /// called from native code on the diagnostic thread when the streaming thread queues a call to notify the
    /// diagnostic thread that it can send the buffer.
    wakeup(): void {
        queueMicrotask(this.signalWorkAvailable);
    }

    private signalWorkAvailableImpl(this: StreamQueue): void {
        this.workAvailable.dispatchEvent(new Event("workAvailable"));
    }

    private onWorkAvailable(this: StreamQueue /*,event: Event */): void {
        const buf = Memory.getI32(this.wakeup_write_addr) as unknown as VoidPtr;
        const count = Memory.getI32(this.count_addr);
        Memory.setI32(this.wakeup_write_addr, 0);
        if (count > 0) {
            this.syncSendBuffer(buf, count);
        }
        Memory.Atomics.storeI32(this.wakeup_write_done_addr, 0);
        Memory.Atomics.notifyI32(this.wakeup_write_done_addr, 1);
    }
}

// maps stream queue addresses to StreamQueue instances
const streamQueueMap = new Map<VoidPtr, StreamQueue>();

export function allocateQueue(nativeQueueAddr: VoidPtr, syncSendBuffer: SyncSendBuffer): StreamQueue {
    const queue = new StreamQueue(nativeQueueAddr, syncSendBuffer);
    streamQueueMap.set(nativeQueueAddr, queue);
    return queue;
}

export function closeQueue(nativeQueueAddr: VoidPtr): void {
    streamQueueMap.delete(nativeQueueAddr);
    // TODO: remove the event listener?
}

// called from native code on the diagnostic thread by queueing a call from the streaming thread.
export function mono_wasm_diagnostic_server_stream_signal_work_available(nativeQueueAddr: VoidPtr): void {
    const queue = streamQueueMap.get(nativeQueueAddr);
    if (queue) {
        queue.wakeup();
    }
}
