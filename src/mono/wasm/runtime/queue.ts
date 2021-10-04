// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export class Queue<T> {
    // amortized time, By Kate Morley http://code.iamkate.com/ under CC0 1.0
    private queue: T[];
    private offset: number;

    constructor() {
        this.queue = [];
        this.offset = 0;
    }
    // initialise the queue and offset

    // Returns the length of the queue.
    getLength() {
        return (this.queue.length - this.offset);
    }

    // Returns true if the queue is empty, and false otherwise.
    isEmpty() {
        return (this.queue.length == 0);
    }

    /* Enqueues the specified item. The parameter is:
    *
    * item - the item to enqueue
    */
    enqueue(item: T) {
        this.queue.push(item);
    }

    /* Dequeues an item and returns it. If the queue is empty, the value
    * 'undefined' is returned.
    */
    dequeue(): T | undefined {

        // if the queue is empty, return immediately
        if (this.queue.length == 0) return undefined;

        // store the item at the front of the queue
        var item = this.queue[this.offset];

        // for GC's sake
        this.queue[this.offset] = <any>null;

        // increment the offset and remove the free space if necessary
        if (++this.offset * 2 >= this.queue.length) {
            this.queue = this.queue.slice(this.offset);
            this.offset = 0;
        }

        // return the dequeued item
        return item;
    }

    /* Returns the item at the front of the queue (without dequeuing it). If the
     * queue is empty then undefined is returned.
     */
    peek() {
        return (this.queue.length > 0 ? this.queue[this.offset] : undefined);
    }

    drain(onEach: (item: T) => void) {
        while (this.getLength()) {
            var item = this.dequeue()!;
            onEach(item);
        }
    }
}