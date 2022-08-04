//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//!
//! This is generated file, see src/mono/wasm/runtime/rollup.config.js

//! This is not considered public API with backward compatibility guarantees. 

declare interface ManagedPointer {
    __brandManagedPointer: "ManagedPointer";
}
declare interface NativePointer {
    __brandNativePointer: "NativePointer";
}
declare interface VoidPtr extends NativePointer {
    __brand: "VoidPtr";
}

declare function mono_wasm_runtime_ready(): void;

declare function mono_wasm_load_icu_data(offset: VoidPtr): boolean;

interface MonoObject extends ManagedPointer {
    __brandMonoObject: "MonoObject";
}
interface MonoString extends MonoObject {
    __brand: "MonoString";
}
interface MonoArray extends MonoObject {
    __brand: "MonoArray";
}
interface MonoObjectRef extends ManagedPointer {
    __brandMonoObjectRef: "MonoObjectRef";
}

declare function mono_wasm_get_assembly_exports(assembly: string): Promise<any>;

declare type _MemOffset = number | VoidPtr | NativePointer | ManagedPointer;
declare type _NumberOrPointer = number | VoidPtr | NativePointer | ManagedPointer;
declare function setB32(offset: _MemOffset, value: number | boolean): void;
declare function setU8(offset: _MemOffset, value: number): void;
declare function setU16(offset: _MemOffset, value: number): void;
declare function setU32(offset: _MemOffset, value: _NumberOrPointer): void;
declare function setI8(offset: _MemOffset, value: number): void;
declare function setI16(offset: _MemOffset, value: number): void;
declare function setI32(offset: _MemOffset, value: number): void;
/**
 * Throws for values which are not 52 bit integer. See Number.isSafeInteger()
 */
declare function setI52(offset: _MemOffset, value: number): void;
/**
 * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
 */
declare function setU52(offset: _MemOffset, value: number): void;
declare function setI64Big(offset: _MemOffset, value: bigint): void;
declare function setF32(offset: _MemOffset, value: number): void;
declare function setF64(offset: _MemOffset, value: number): void;
declare function getB32(offset: _MemOffset): boolean;
declare function getU8(offset: _MemOffset): number;
declare function getU16(offset: _MemOffset): number;
declare function getU32(offset: _MemOffset): number;
declare function getI8(offset: _MemOffset): number;
declare function getI16(offset: _MemOffset): number;
declare function getI32(offset: _MemOffset): number;
/**
 * Throws for Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
 */
declare function getI52(offset: _MemOffset): number;
/**
 * Throws for 0 > value > Number.MAX_SAFE_INTEGER
 */
declare function getU52(offset: _MemOffset): number;
declare function getI64Big(offset: _MemOffset): bigint;
declare function getF32(offset: _MemOffset): number;
declare function getF64(offset: _MemOffset): number;
declare function mono_wasm_load_bytes_into_heap(bytes: Uint8Array): VoidPtr;

/**
 * Allocates a block of memory that can safely contain pointers into the managed heap.
 * The result object has get(index) and set(index, value) methods that can be used to retrieve and store managed pointers.
 * Once you are done using the root buffer, you must call its release() method.
 * For small numbers of roots, it is preferable to use the mono_wasm_new_root and mono_wasm_new_roots APIs instead.
 */
declare function mono_wasm_new_root_buffer(capacity: number, name?: string): WasmRootBuffer;
/**
 * Allocates a WasmRoot pointing to a root provided and controlled by external code. Typicaly on managed stack.
 * Releasing this root will not de-allocate the root space. You still need to call .release().
 */
declare function mono_wasm_new_external_root<T extends MonoObject>(address: VoidPtr | MonoObjectRef): WasmRoot<T>;
/**
 * Allocates temporary storage for a pointer into the managed heap.
 * Pointers stored here will be visible to the GC, ensuring that the object they point to aren't moved or collected.
 * If you already have a managed pointer you can pass it as an argument to initialize the temporary storage.
 * The result object has get() and set(value) methods, along with a .value property.
 * When you are done using the root you must call its .release() method.
 */
declare function mono_wasm_new_root<T extends MonoObject>(value?: T | undefined): WasmRoot<T>;
/**
 * Releases 1 or more root or root buffer objects.
 * Multiple objects may be passed on the argument list.
 * 'undefined' may be passed as an argument so it is safe to call this method from finally blocks
 *  even if you are not sure all of your roots have been created yet.
 * @param {... WasmRoot} roots
 */
declare function mono_wasm_release_roots(...args: WasmRoot<any>[]): void;
declare class WasmRootBuffer {
    private __count;
    private length;
    private __offset;
    private __offset32;
    private __handle;
    private __ownsAllocation;
    constructor(offset: VoidPtr, capacity: number, ownsAllocation: boolean, name?: string);
    _throw_index_out_of_range(): void;
    _check_in_range(index: number): void;
    get_address(index: number): MonoObjectRef;
    get_address_32(index: number): number;
    get(index: number): ManagedPointer;
    set(index: number, value: ManagedPointer): ManagedPointer;
    copy_value_from_address(index: number, sourceAddress: MonoObjectRef): void;
    _unsafe_get(index: number): number;
    _unsafe_set(index: number, value: ManagedPointer | NativePointer): void;
    clear(): void;
    release(): void;
    toString(): string;
}
interface WasmRoot<T extends MonoObject> {
    get_address(): MonoObjectRef;
    get_address_32(): number;
    get address(): MonoObjectRef;
    get(): T;
    set(value: T): T;
    get value(): T;
    set value(value: T);
    copy_from_address(source: MonoObjectRef): void;
    copy_to_address(destination: MonoObjectRef): void;
    copy_from(source: WasmRoot<T>): void;
    copy_to(destination: WasmRoot<T>): void;
    valueOf(): T;
    clear(): void;
    release(): void;
    toString(): string;
}

declare function mono_run_main_and_exit(main_assembly_name: string, args: string[]): Promise<void>;
declare function mono_run_main(main_assembly_name: string, args: string[]): Promise<number>;

declare function mono_wasm_setenv(name: string, value: string): void;
declare function mono_wasm_load_data_archive(data: Uint8Array, prefix: string): boolean;
/**
 * Loads the mono config file (typically called mono-config.json) asynchroniously
 * Note: the run dependencies are so emsdk actually awaits it in order.
 *
 * @param {string} configFilePath - relative path to the config file
 * @throws Will throw an error if the config file loading fails
 */
declare function mono_wasm_load_config(configFilePath: string): Promise<void>;

/**
 * @deprecated Not GC or thread safe
 */
declare function conv_string(mono_obj: MonoString): string | null;
declare function conv_string_root(root: WasmRoot<MonoString>): string | null;
declare function js_string_to_mono_string_root(string: string, result: WasmRoot<MonoString>): void;
/**
 * @deprecated Not GC or thread safe
 */
declare function js_string_to_mono_string(string: string): MonoString;

declare function unbox_mono_obj(mono_obj: MonoObject): any;
declare function unbox_mono_obj_root(root: WasmRoot<any>): any;
declare function mono_array_to_js_array(mono_array: MonoArray): any[] | null;
declare function mono_array_root_to_js_array(arrayRoot: WasmRoot<MonoArray>): any[] | null;

/**
 * @deprecated Not GC or thread safe. For blazor use only
 */
declare function js_to_mono_obj(js_obj: any): MonoObject;
declare function js_to_mono_obj_root(js_obj: any, result: WasmRoot<MonoObject>, should_add_in_flight: boolean): void;
declare function js_typed_array_to_array_root(js_obj: any, result: WasmRoot<MonoArray>): void;
/**
 * @deprecated Not GC or thread safe
 */
declare function js_typed_array_to_array(js_obj: any): MonoArray;

declare function mono_bind_static_method(fqn: string, signature?: string): Function;
declare function mono_call_assembly_entry_point(assembly: string, args?: any[], signature?: string): number;

/**
 * @deprecated Please use methods in top level API object instead
 */
declare type BINDINGType = {
    /**
     * @deprecated Please use [JSExportAttribute] instead
     */
    bind_static_method: typeof mono_bind_static_method;
    /**
     * @deprecated Please use runMain() instead
     */
    call_assembly_entry_point: typeof mono_call_assembly_entry_point;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_new: (size: number) => MonoArray;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_set: (array: MonoArray, idx: number, obj: MonoObject) => void;
    /**
     * @deprecated Not GC or thread safe
     */
    js_string_to_mono_string: typeof js_string_to_mono_string;
    /**
     * @deprecated Not GC or thread safe
     */
    js_typed_array_to_array: typeof js_typed_array_to_array;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_array_to_js_array: typeof mono_array_to_js_array;
    /**
     * @deprecated Not GC or thread safe
     */
    js_to_mono_obj: typeof js_to_mono_obj;
    /**
     * @deprecated Not GC or thread safe
     */
    conv_string: typeof conv_string;
    /**
     * @deprecated Not GC or thread safe
     */
    unbox_mono_obj: typeof unbox_mono_obj;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_obj_array_new_ref: (size: number, result: MonoObjectRef) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_obj_array_set_ref: (array: MonoObjectRef, idx: number, obj: MonoObjectRef) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_string_to_mono_string_root: typeof js_string_to_mono_string_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_typed_array_to_array_root: typeof js_typed_array_to_array_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_to_mono_obj_root: typeof js_to_mono_obj_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    conv_string_root: typeof conv_string_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    unbox_mono_obj_root: typeof unbox_mono_obj_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_array_root_to_js_array: typeof mono_array_root_to_js_array;
};
/**
 * @deprecated Please use methods in top level API object instead
 */
declare type MONOType = {
    /**
     * @deprecated Please use setEnvironmentVariable() instead
     */
    mono_wasm_setenv: typeof mono_wasm_setenv;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_runtime_ready: typeof mono_wasm_runtime_ready;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_data_archive: typeof mono_wasm_load_data_archive;
    /**
     * @deprecated Please use configSrc instead
     */
    mono_wasm_load_config: typeof mono_wasm_load_config;
    /**
     * @deprecated Please use runMain instead
     */
    mono_load_runtime_and_bcl_args: Function;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_root_buffer: typeof mono_wasm_new_root_buffer;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_root: typeof mono_wasm_new_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_external_root: typeof mono_wasm_new_external_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_release_roots: typeof mono_wasm_release_roots;
    /**
     * @deprecated Please use runMain instead
     */
    mono_run_main: typeof mono_run_main;
    /**
     * @deprecated Please use runMainAndExit instead
     */
    mono_run_main_and_exit: typeof mono_run_main_and_exit;
    /**
     * @deprecated Please use getAssemblyExports instead
     */
    mono_wasm_get_assembly_exports: typeof mono_wasm_get_assembly_exports;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_add_assembly: (name: string, data: VoidPtr, size: number) => number;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_runtime: (unused: string, debugLevel: number) => void;
    /**
     * @deprecated Please use getConfig() instead
     */
    config: any;
    /**
     * @deprecated Please use config.assets instead
     */
    loaded_files: string[];
    /**
     * @deprecated Please use setHeapB32
     */
    setB32: typeof setB32;
    /**
     * @deprecated Please use setHeapI8
     */
    setI8: typeof setI8;
    /**
     * @deprecated Please use setHeapI16
     */
    setI16: typeof setI16;
    /**
     * @deprecated Please use setHeapI32
     */
    setI32: typeof setI32;
    /**
     * @deprecated Please use setHeapI52
     */
    setI52: typeof setI52;
    /**
     * @deprecated Please use setHeapU52
     */
    setU52: typeof setU52;
    /**
     * @deprecated Please use setHeapI64Big
     */
    setI64Big: typeof setI64Big;
    /**
     * @deprecated Please use setHeapU8
     */
    setU8: typeof setU8;
    /**
     * @deprecated Please use setHeapU16
     */
    setU16: typeof setU16;
    /**
     * @deprecated Please use setHeapU32
     */
    setU32: typeof setU32;
    /**
     * @deprecated Please use setHeapF32
     */
    setF32: typeof setF32;
    /**
     * @deprecated Please use setHeapF64
     */
    setF64: typeof setF64;
    /**
     * @deprecated Please use getHeapB32
     */
    getB32: typeof getB32;
    /**
     * @deprecated Please use getHeapI8
     */
    getI8: typeof getI8;
    /**
     * @deprecated Please use getHeapI16
     */
    getI16: typeof getI16;
    /**
     * @deprecated Please use getHeapI32
     */
    getI32: typeof getI32;
    /**
     * @deprecated Please use getHeapI52
     */
    getI52: typeof getI52;
    /**
     * @deprecated Please use getHeapU52
     */
    getU52: typeof getU52;
    /**
     * @deprecated Please use getHeapI64Big
     */
    getI64Big: typeof getI64Big;
    /**
     * @deprecated Please use getHeapU8
     */
    getU8: typeof getU8;
    /**
     * @deprecated Please use getHeapU16
     */
    getU16: typeof getU16;
    /**
     * @deprecated Please use getHeapU32
     */
    getU32: typeof getU32;
    /**
     * @deprecated Please use getHeapF32
     */
    getF32: typeof getF32;
    /**
     * @deprecated Please use getHeapF64
     */
    getF64: typeof getF64;
};

export { BINDINGType, MONOType };
