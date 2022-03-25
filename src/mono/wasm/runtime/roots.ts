// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { Module } from "./imports";
import { VoidPtr, ManagedPointer, NativePointer } from "./types/emscripten";
import { MonoObjectRef, MonoObjectRefNull, MonoObject, is_nullish, WasmRoot, WasmRootBuffer } from "./types";
import { _zero_region, getU32 } from "./memory";

const maxScratchRoots = 8192;
let _scratch_root_buffer: WasmRootBuffer | null = null;
let _scratch_root_free_indices: Int32Array | null = null;
let _scratch_root_free_indices_count = 0;
const _scratch_root_free_instances: WasmRoot<any>[] = [];
const _external_root_free_instances: WasmExternalRoot<any>[] = [];

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

    return new WasmRootBufferImpl(<any>offset, capacity, WasmRootBufferType.Owned, name);
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

    return new WasmRootBufferImpl(<any>offset, capacity, WasmRootBufferType.External, name);
}

/**
 * Allocates a WasmRoot pointing to a root provided and controlled by external code. Typicaly on managed stack.
 * Releasing this root will not de-allocate the root space. You still need to call .release().
 */
export function mono_wasm_new_external_root<T extends MonoObject>(address: VoidPtr | MonoObjectRef): WasmRoot<T> {
    let result: WasmExternalRoot<T>;

    if (!address)
        throw new Error("address must be a location in the native heap");

    if (_external_root_free_instances.length > 0) {
        result = _external_root_free_instances.pop()!;
        result._set_address(address);
    } else {
        result = new WasmExternalRoot<T>(address);
    }

    return result;
}

/**
 * Allocates temporary storage for a pointer into the managed heap.
 * Pointers stored here will be visible to the GC, ensuring that the object they point to aren't moved or collected.
 * If you already have a managed pointer you can pass it as an argument to initialize the temporary storage.
 * The result object has get() and set(value) methods, along with a .value property.
 * When you are done using the root you must call its .release() method.
 */
export function mono_wasm_new_root<T extends MonoObject>(value: T | undefined = undefined): WasmRoot<T> {
    let result: WasmRoot<T>;

    if (_scratch_root_free_instances.length > 0) {
        result = _scratch_root_free_instances.pop()!;
    } else {
        const index = _mono_wasm_claim_scratch_index();
        const buffer = _scratch_root_buffer;

        result = new WasmJsOwnedRoot(buffer!, index);
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
export function mono_wasm_new_roots<T extends MonoObject>(count_or_values: number | T[]): WasmRoot<T>[] {
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
        if (is_nullish(args[i]))
            continue;

        args[i].release();
    }
}

let _scratch_hazard_buffer_wrapper: WasmRootBuffer | null = null;

/**
 * Specific overloads for good tsc type info and errors
 * If we merely exported the implementation below, it would be possible to do:
 *  mono_wasm_with_hazard_buffer(n, (buf: ..., a: string, b: number) => (a + b), "hello") and
 *  despite the fact that we omitted the 2nd userdata (a number) tsc would happily roll along,
 *  and the userdata callback would receive an undefined where a number should have been.
 * Providing these specific overloads ensures that the number of userdata parameters must match
 *  the number of userdata parameters accepted by the callback.
 * At this point you may be wondering: Why not just use a single '...args' signature?
 * Because spread, args arrays, etc all have significant JS runtime overhead! :-)
 * The underlying implementation will always try and pass 3 userdata values, even if it only got
 *  one value or the callback only wants one. This is fine, because the JS runtime will discard
 *  any extra parameters that the caller doesn't want.
 */
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_with_hazard_buffer<TFunc extends (buffer: WasmRootBuffer) => any>(
    num_elements: number,
    func: TFunc
): ReturnType<TFunc>;
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_with_hazard_buffer<TFunc extends (buffer: WasmRootBuffer, userdata1: any) => any>(
    num_elements: number,
    func: TFunc,
    userdata1: Parameters<TFunc>[1]
): ReturnType<TFunc>;
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_with_hazard_buffer<TFunc extends (buffer: WasmRootBuffer, userdata1: any, userdata2: any) => any>(
    num_elements: number,
    func: TFunc,
    userdata1: Parameters<TFunc>[1],
    userdata2: Parameters<TFunc>[2]
): ReturnType<TFunc>;
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_with_hazard_buffer<TFunc extends (buffer: WasmRootBuffer, userdata1: any, userdata2: any, userdata3: any) => any>(
    num_elements: number,
    func: TFunc,
    userdata1: Parameters<TFunc>[1],
    userdata2: Parameters<TFunc>[2],
    userdata3: Parameters<TFunc>[3]
): ReturnType<TFunc>;

/**
 * Invokes func(hazard_buffer, [userdata1], [userdata2], [userdata3]), where:
 *  hazard_buffer is a WasmRootBuffer with space for at least num_elements managed pointers to be stored in it
 *  hazard_buffer is stored on the stack and cannot be used after func returns
 * Any managed pointers stored inside hazard_buffer will not be moved or destroyed by the GC until func returns.
 * func's return value (if any) will be returned by mono_wasm_with_hazard_buffer.
 */
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_with_hazard_buffer<TFunc extends (buffer: WasmRootBuffer, ...userdata: any) => any>(
    num_elements: number,
    func: TFunc,
    userdata1?: Parameters<TFunc>[1],
    userdata2?: Parameters<TFunc>[2],
    userdata3?: Parameters<TFunc>[3]
): ReturnType<TFunc> {
    const sizeBytes = num_elements * 4;
    const oldStackTop = Module.stackSave();
    let wrapper = _scratch_hazard_buffer_wrapper;
    _scratch_hazard_buffer_wrapper = null;
    try {
        const offset = Module.stackAlloc(sizeBytes);
        if (!wrapper)
            wrapper = new WasmRootBufferImpl(<any>offset, num_elements, WasmRootBufferType.Stack);
        else
            (<WasmRootBufferImpl>wrapper)._unsafe_change_address(<any>offset, num_elements);
        wrapper.clear();

        return func(wrapper, userdata1, userdata2, userdata3);
    } finally {
        if (wrapper) {
            wrapper.release();
            if (!_scratch_hazard_buffer_wrapper)
                _scratch_hazard_buffer_wrapper = wrapper;
        }
        Module.stackRestore(oldStackTop);
    }
}

/**
 * Pins the object pointed to by root and then invokes func(obj, userdata).
 * Until func returns, the object is guaranteed to not move or be collected by the GC.
 */
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_with_pinned_object<T extends MonoObject, TFunc extends (ptr: T, userdata?: any) => any>(
    root: WasmRoot<T>,
    func: TFunc,
    userdata?: Parameters<TFunc>[1]
): ReturnType<TFunc> {
    const oldStackTop = Module.stackSave();
    try {
        const offset = Module.stackAlloc(4);
        root.copy_to_address(<any>offset);
        // We read-back after the copy since the object may have been moved by the GC
        return func(<any>getU32(offset), userdata);
    } finally {
        Module.stackRestore(oldStackTop);
    }
}

function _mono_wasm_release_scratch_index(index: number) {
    if (index === undefined)
        return;

    _scratch_root_buffer!.set(index, <any>0);
    _scratch_root_free_indices![_scratch_root_free_indices_count] = index;
    _scratch_root_free_indices_count++;
}

function _mono_wasm_claim_scratch_index() {
    if (is_nullish(_scratch_root_buffer) || !_scratch_root_free_indices) {
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

const enum WasmRootBufferType {
    // A root buffer with its storage controlled by the WasmRootBuffer instance. The release method will deallocate it.
    Owned,
    // A root buffer with its storage controlled elsewhere. The release method will unregister the root but not deallocate it.
    External,
    // A root buffer present on the stack. The release method will do nothing.
    Stack
}

export class WasmRootBufferImpl implements WasmRootBuffer {
    private __count: number;
    private __offset: MonoObjectRef;
    private __offset32: number;
    private __type: WasmRootBufferType;

    constructor(offset: MonoObjectRef, capacity: number, type: WasmRootBufferType, name?: string) {
        // silence tsc warnings
        this.__offset = <any>(this.__count = this.__offset32 = 0);

        if (type !== WasmRootBufferType.Stack) {
            const capacityBytes = capacity * 4;
            cwraps.mono_wasm_register_root(offset as any, capacityBytes, name || "noname");
        }

        this._unsafe_change_address(offset, capacity);
        this.__type = type;
    }

    get length(): number {
        return this.__count;
    }

    _unsafe_change_address(offset: MonoObjectRef, capacity: number): void {
        this.__offset = offset;
        this.__offset32 = <number><any>offset >>> 2;
        this.__count = capacity;
    }

    _throw_index_out_of_range(): void {
        if (this.__offset)
            throw new Error("index out of range");
        else
            throw new Error("root buffer already released");
    }

    _check_in_range(index: number): void {
        if ((index >= this.__count) || (index < 0))
            this._throw_index_out_of_range();
    }

    get_address(index: number): MonoObjectRef {
        this._check_in_range(index);
        return <any>this.__offset + (index * 4);
    }

    get_address_32(index: number): number {
        this._check_in_range(index);
        return this.__offset32 + index;
    }

    // NOTE: These functions do not use the helpers from memory.ts because WasmRoot.get and WasmRoot.set
    //  are hot-spots when you profile any application that uses the bindings extensively.

    get(index: number): MonoObject {
        this._check_in_range(index);
        const offset = this.get_address_32(index);
        return <any>Module.HEAPU32[offset];
    }

    set(index: number, value: MonoObject): MonoObject {
        const address = this.get_address(index);
        cwraps.mono_wasm_write_managed_pointer_unsafe(address, value);
        return value;
    }

    copy_value_from_address(index: number, sourceAddress: MonoObjectRef): void {
        const destinationAddress = this.get_address(index);
        cwraps.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
    }

    _unsafe_get(index: number): number {
        return Module.HEAPU32[this.__offset32 + index];
    }

    _unsafe_set(index: number, value: ManagedPointer | NativePointer): void {
        const address = <any>this.__offset + index;
        cwraps.mono_wasm_write_managed_pointer_unsafe(<VoidPtr><any>address, <ManagedPointer>value);
    }

    clear(): void {
        if (this.__offset)
            _zero_region(<any>this.__offset, this.__count * 4);
    }

    release(): void {
        switch (this.__type) {
            case WasmRootBufferType.Owned:
                cwraps.mono_wasm_deregister_root(<any>this.__offset);
                _zero_region(<any>this.__offset, this.__count * 4);
                Module._free(<any>this.__offset);
                break;
            default:
                break;
        }

        this.__offset = <any>(this.__count = this.__offset32 = 0);
    }

    toString(): string {
        return `[root buffer @${this.get_address(0)}, size ${this.__count} ]`;
    }
}

class WasmJsOwnedRoot<T extends MonoObject> implements WasmRoot<T> {
    private __buffer: WasmRootBuffer;
    private __index: number;

    constructor(buffer: WasmRootBuffer, index: number) {
        this.__buffer = buffer;//TODO
        this.__index = index;
    }

    get_address(): MonoObjectRef {
        return this.__buffer.get_address(this.__index);
    }

    get_address_32(): number {
        return this.__buffer.get_address_32(this.__index);
    }

    get address(): MonoObjectRef {
        return this.__buffer.get_address(this.__index);
    }

    get(): T {
        const result = (<WasmRootBufferImpl>this.__buffer)._unsafe_get(this.__index);
        return <any>result;
    }

    set(value: T): T {
        const destinationAddress = this.__buffer.get_address(this.__index);
        cwraps.mono_wasm_write_managed_pointer_unsafe(destinationAddress, <ManagedPointer>value);
        return value;
    }

    copy_from(source: WasmRoot<T>): void {
        const sourceAddress = source.address;
        const destinationAddress = this.address;
        cwraps.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
    }

    copy_to(destination: WasmRoot<T>): void {
        const sourceAddress = this.address;
        const destinationAddress = destination.address;
        cwraps.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
    }

    copy_from_address(source: MonoObjectRef): void {
        const destinationAddress = this.address;
        cwraps.mono_wasm_copy_managed_pointer(destinationAddress, source);
    }

    copy_to_address(destination: MonoObjectRef): void {
        const sourceAddress = this.address;
        cwraps.mono_wasm_copy_managed_pointer(destination, sourceAddress);
    }

    get value(): T {
        return this.get();
    }

    set value(value: T) {
        this.set(value);
    }

    valueOf(): T {
        throw new Error("Implicit conversion of roots to pointers is no longer supported. Use .value or .address as appropriate");
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
        return `[root @${this.address}]`;
    }
}

class WasmExternalRoot<T extends MonoObject> implements WasmRoot<T> {
    private __external_address: MonoObjectRef = MonoObjectRefNull;
    private __external_address_32: number = <any>0;

    constructor(address: NativePointer | ManagedPointer) {
        this._set_address(address);
    }

    _set_address(address: NativePointer | ManagedPointer): void {
        this.__external_address = <MonoObjectRef><any>address;
        this.__external_address_32 = <number><any>address >>> 2;
    }

    get address(): MonoObjectRef {
        return <MonoObjectRef><any>this.__external_address;
    }

    get_address(): MonoObjectRef {
        return <MonoObjectRef><any>this.__external_address;
    }

    get_address_32(): number {
        return this.__external_address_32;
    }

    get(): T {
        const result = Module.HEAPU32[this.__external_address_32];
        return <any>result;
    }

    set(value: T): T {
        cwraps.mono_wasm_write_managed_pointer_unsafe(this.__external_address, <ManagedPointer>value);
        return value;
    }

    copy_from(source: WasmRoot<T>): void {
        const sourceAddress = source.address;
        const destinationAddress = this.__external_address;
        cwraps.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
    }

    copy_to(destination: WasmRoot<T>): void {
        const sourceAddress = this.__external_address;
        const destinationAddress = destination.address;
        cwraps.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
    }

    copy_from_address(source: MonoObjectRef): void {
        const destinationAddress = this.__external_address;
        cwraps.mono_wasm_copy_managed_pointer(destinationAddress, source);
    }

    copy_to_address(destination: MonoObjectRef): void {
        const sourceAddress = this.__external_address;
        cwraps.mono_wasm_copy_managed_pointer(destination, sourceAddress);
    }

    get value(): T {
        return this.get();
    }

    set value(value: T) {
        this.set(value);
    }

    valueOf(): T {
        throw new Error("Implicit conversion of roots to pointers is no longer supported. Use .value or .address as appropriate");
    }

    clear(): void {
        this.set(<any>0);
    }

    release(): void {
        const maxPooledInstances = 128;
        if (_external_root_free_instances.length < maxPooledInstances)
            _external_root_free_instances.push(this);
    }

    toString(): string {
        return `[external root @${this.address}]`;
    }
}