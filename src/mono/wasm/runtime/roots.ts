// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { Module } from "./imports";
import { VoidPtr, ManagedPointer, NativePointer } from "./types";

const maxScratchRoots = 8192;
let _scratch_root_buffer: WasmRootBuffer | null = null;
let _scratch_root_free_indices: Int32Array | null = null;
let _scratch_root_free_indices_count = 0;
const _scratch_root_free_instances: WasmRoot<any>[] = [];

/**
 * Allocates a block of memory that can safely contain pointers into the managed heap.
 * The result object has get(index) and set(index, value) methods that can be used to retrieve and store managed pointers.
 * Once you are done using the root buffer, you must call its release() method.
 * For small numbers of roots, it is preferable to use the mono_wasm_new_root and mono_wasm_new_roots APIs instead.
 */
export function mono_wasm_new_root_buffer(capacity: number, name?: string): WasmRootBuffer {
    if (capacity <= 0)
        throw new Error("capacity >= 1");

    capacity = capacity | 0;

    const capacityBytes = capacity * 4;
    const offset = Module._malloc(capacityBytes);
    if ((<any>offset % 4) !== 0)
        throw new Error("Malloc returned an unaligned offset");

    _zero_region(offset, capacityBytes);

    return new WasmRootBuffer(offset, capacity, true, name);
}

/**
 * Creates a root buffer object representing an existing allocation in the native heap and registers
 *  the allocation with the GC. The caller is responsible for managing the lifetime of the allocation.
 */
export function mono_wasm_new_root_buffer_from_pointer(offset: VoidPtr, capacity: number, name?: string): WasmRootBuffer {
    if (capacity <= 0)
        throw new Error("capacity >= 1");

    capacity = capacity | 0;

    const capacityBytes = capacity * 4;
    if ((<any>offset % 4) !== 0)
        throw new Error("Unaligned offset");

    _zero_region(offset, capacityBytes);

    return new WasmRootBuffer(offset, capacity, false, name);
}

/**
 * Allocates temporary storage for a pointer into the managed heap.
 * Pointers stored here will be visible to the GC, ensuring that the object they point to aren't moved or collected.
 * If you already have a managed pointer you can pass it as an argument to initialize the temporary storage.
 * The result object has get() and set(value) methods, along with a .value property.
 * When you are done using the root you must call its .release() method.
 */
export function mono_wasm_new_root<T extends ManagedPointer | NativePointer>(value: T | undefined = undefined): WasmRoot<T> {
    let result: WasmRoot<T>;

    if (_scratch_root_free_instances.length > 0) {
        result = _scratch_root_free_instances.pop()!;
    } else {
        const index = _mono_wasm_claim_scratch_index();
        const buffer = _scratch_root_buffer;

        result = new WasmRoot(buffer!, index);
    }

    if (value !== undefined) {
        if (typeof (value) !== "number")
            throw new Error("value must be an address in the managed heap");

        result.set(value);
    } else {
        result.set(<any>0);
    }

    return result;
}

/**
 * Allocates 1 or more temporary roots, accepting either a number of roots or an array of pointers.
 * mono_wasm_new_roots(n): returns an array of N zero-initialized roots.
 * mono_wasm_new_roots([a, b, ...]) returns an array of new roots initialized with each element.
 * Each root must be released with its release method, or using the mono_wasm_release_roots API.
 */
export function mono_wasm_new_roots<T extends ManagedPointer | NativePointer>(count_or_values: number | T[]): WasmRoot<T>[] {
    let result;

    if (Array.isArray(count_or_values)) {
        result = new Array(count_or_values.length);
        for (let i = 0; i < result.length; i++)
            result[i] = mono_wasm_new_root(count_or_values[i]);
    } else if ((count_or_values | 0) > 0) {
        result = new Array(count_or_values);
        for (let i = 0; i < result.length; i++)
            result[i] = mono_wasm_new_root();
    } else {
        throw new Error("count_or_values must be either an array or a number greater than 0");
    }

    return result;
}

/**
 * Releases 1 or more root or root buffer objects.
 * Multiple objects may be passed on the argument list.
 * 'undefined' may be passed as an argument so it is safe to call this method from finally blocks
 *  even if you are not sure all of your roots have been created yet.
 * @param {... WasmRoot} roots
 */
export function mono_wasm_release_roots(...args: WasmRoot<any>[]): void {
    for (let i = 0; i < args.length; i++) {
        if (!args[i])
            continue;

        args[i].release();
    }
}

function _zero_region(byteOffset: VoidPtr, sizeBytes: number) {
    if (((<any>byteOffset % 4) === 0) && ((sizeBytes % 4) === 0))
        Module.HEAP32.fill(0, <any>byteOffset >>> 2, sizeBytes >>> 2);
    else
        Module.HEAP8.fill(0, <any>byteOffset, sizeBytes);
}

function _mono_wasm_release_scratch_index(index: number) {
    if (index === undefined)
        return;

    _scratch_root_buffer!.set(index, <any>0);
    _scratch_root_free_indices![_scratch_root_free_indices_count] = index;
    _scratch_root_free_indices_count++;
}

function _mono_wasm_claim_scratch_index() {
    if (!_scratch_root_buffer || !_scratch_root_free_indices) {
        _scratch_root_buffer = mono_wasm_new_root_buffer(maxScratchRoots, "js roots");

        _scratch_root_free_indices = new Int32Array(maxScratchRoots);
        _scratch_root_free_indices_count = maxScratchRoots;
        for (let i = 0; i < maxScratchRoots; i++)
            _scratch_root_free_indices[i] = maxScratchRoots - i - 1;
    }

    if (_scratch_root_free_indices_count < 1)
        throw new Error("Out of scratch root space");

    const result = _scratch_root_free_indices[_scratch_root_free_indices_count - 1];
    _scratch_root_free_indices_count--;
    return result;
}


export class WasmRootBuffer {
    private __count: number;
    private length: number;
    private __offset: VoidPtr;
    private __offset32: number;
    private __handle: number;
    private __ownsAllocation: boolean;

    constructor(offset: VoidPtr, capacity: number, ownsAllocation: boolean, name?: string) {
        const capacityBytes = capacity * 4;

        this.__offset = offset;
        this.__offset32 = <number><any>offset >>> 2;
        this.__count = capacity;
        this.length = capacity;
        this.__handle = cwraps.mono_wasm_register_root(offset, capacityBytes, name || "noname");
        this.__ownsAllocation = ownsAllocation;
    }

    _throw_index_out_of_range(): void {
        throw new Error("index out of range");
    }

    _check_in_range(index: number): void {
        if ((index >= this.__count) || (index < 0))
            this._throw_index_out_of_range();
    }

    get_address(index: number): NativePointer {
        this._check_in_range(index);
        return <any>this.__offset + (index * 4);
    }

    get_address_32(index: number): number {
        this._check_in_range(index);
        return this.__offset32 + index;
    }

    // NOTE: These functions do not use the helpers from memory.ts because WasmRoot.get and WasmRoot.set
    //  are hot-spots when you profile any application that uses the bindings extensively.

    get(index: number): ManagedPointer {
        this._check_in_range(index);
        const offset = this.get_address_32(index);
        return <any>Module.HEAP32[offset];
    }

    set(index: number, value: ManagedPointer): ManagedPointer {
        const offset = this.get_address_32(index);
        Module.HEAP32[offset] = <any>value;
        return value;
    }

    _unsafe_get(index: number): number {
        return Module.HEAP32[this.__offset32 + index];
    }

    _unsafe_set(index: number, value: ManagedPointer | NativePointer): void {
        Module.HEAP32[this.__offset32 + index] = <any>value;
    }

    clear(): void {
        if (this.__offset)
            _zero_region(this.__offset, this.__count * 4);
    }

    release(): void {
        if (this.__offset && this.__ownsAllocation) {
            cwraps.mono_wasm_deregister_root(this.__offset);
            _zero_region(this.__offset, this.__count * 4);
            Module._free(this.__offset);
        }

        this.__handle = (<any>this.__offset) = this.__count = this.__offset32 = 0;
    }

    toString(): string {
        return `[root buffer @${this.get_address(0)}, size ${this.__count} ]`;
    }
}

export class WasmRoot<T extends ManagedPointer | NativePointer> {
    private __buffer: WasmRootBuffer;
    private __index: number;

    constructor(buffer: WasmRootBuffer, index: number) {
        this.__buffer = buffer;//TODO
        this.__index = index;
    }

    get_address(): NativePointer {
        return this.__buffer.get_address(this.__index);
    }

    get_address_32(): number {
        return this.__buffer.get_address_32(this.__index);
    }

    get(): T {
        const result = this.__buffer._unsafe_get(this.__index);
        return <any>result;
    }

    set(value: T): T {
        this.__buffer._unsafe_set(this.__index, value);
        return value;
    }

    get value(): T {
        return this.get();
    }

    set value(value: T) {
        this.set(value);
    }

    valueOf(): T {
        return this.get();
    }

    clear(): void {
        this.set(<any>0);
    }

    release(): void {
        if (!this.__buffer)
            throw new Error("No buffer");

        const maxPooledInstances = 128;
        if (_scratch_root_free_instances.length > maxPooledInstances) {
            _mono_wasm_release_scratch_index(this.__index);
            (<any>this).__buffer = null;
            this.__index = 0;
        } else {
            this.set(<any>0);
            _scratch_root_free_instances.push(this);
        }
    }

    toString(): string {
        return `[root @${this.get_address()}]`;
    }
}
