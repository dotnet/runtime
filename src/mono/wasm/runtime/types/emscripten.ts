// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export declare interface ManagedPointer {
    __brandManagedPointer: "ManagedPointer"
}

export declare interface NativePointer {
    __brandNativePointer: "NativePointer"
}

export declare interface VoidPtr extends NativePointer {
    __brand: "VoidPtr"
}
export declare interface CharPtr extends NativePointer {
    __brand: "CharPtr"
}
export declare interface Int32Ptr extends NativePointer {
    __brand: "Int32Ptr"
}
export declare interface CharPtrPtr extends NativePointer {
    __brand: "CharPtrPtr"
}

export declare interface EmscriptenModule {
    /** @deprecated Please use growableHeapI8() instead.*/
    HEAP8: Int8Array,
    /** @deprecated Please use growableHeapI16() instead.*/
    HEAP16: Int16Array;
    /** @deprecated Please use growableHeapI32() instead. */
    HEAP32: Int32Array;
    /** @deprecated Please use growableHeapI64() instead. */
    HEAP64: BigInt64Array;
    /** @deprecated Please use growableHeapU8() instead. */
    HEAPU8: Uint8Array;
    /** @deprecated Please use growableHeapU16() instead. */
    HEAPU16: Uint16Array;
    /** @deprecated Please use growableHeapU32() instead */
    HEAPU32: Uint32Array;
    /** @deprecated Please use growableHeapF32() instead */
    HEAPF32: Float32Array;
    /** @deprecated Please use growableHeapF64() instead. */
    HEAPF64: Float64Array;

    // this should match emcc -s EXPORTED_FUNCTIONS
    _malloc(size: number): VoidPtr;
    _free(ptr: VoidPtr): void;

    // this should match emcc -s EXPORTED_RUNTIME_METHODS
    out(message: string): void;
    err(message: string): void;
    ccall<T>(ident: string, returnType?: string | null, argTypes?: string[], args?: any[], opts?: any): T;
    cwrap<T extends Function>(ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    cwrap<T extends Function>(ident: string, ...args: any[]): T;
    setValue(ptr: VoidPtr, value: number, type: string, noSafe?: number | boolean): void;
    setValue(ptr: Int32Ptr, value: number, type: string, noSafe?: number | boolean): void;
    getValue(ptr: number, type: string, noSafe?: number | boolean): number;
    UTF8ToString(ptr: CharPtr, maxBytesToRead?: number): string;
    UTF8ArrayToString(u8Array: Uint8Array, idx?: number, maxBytesToRead?: number): string;
    stringToUTF8Array(str: string, heap: Uint8Array, outIdx: number, maxBytesToWrite: number): void;
    FS_createPath(parent: string, path: string, canRead?: boolean, canWrite?: boolean): string;
    FS_createDataFile(parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    addFunction(fn: Function, signature: string): number;
    stackSave(): VoidPtr;
    stackRestore(stack: VoidPtr): void;
    stackAlloc(size: number): VoidPtr;


    instantiateWasm?: InstantiateWasmCallBack;
    preInit?: (() => any)[] | (() => any);
    preRun?: (() => any)[] | (() => any);
    onRuntimeInitialized?: () => any;
    postRun?: (() => any)[] | (() => any);
    onAbort?: { (error: any): void };
}

export type InstantiateWasmSuccessCallback = (instance: WebAssembly.Instance, module: WebAssembly.Module | undefined) => void;
export type InstantiateWasmCallBack = (imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback) => any;

export declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;
