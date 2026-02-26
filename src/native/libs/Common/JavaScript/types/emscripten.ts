// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export interface ManagedPointer {
    __brandManagedPointer: "ManagedPointer"
}

export interface NativePointer {
    __brandNativePointer: "NativePointer"
}

export interface VoidPtr extends NativePointer {
    __brand: "VoidPtr"
}
export interface VoidPtrPtr extends NativePointer {
    __brand: "VoidPtrPtr"
}
export interface CharPtr extends NativePointer {
    __brand: "CharPtr"
}
export interface Int32Ptr extends NativePointer {
    __brand: "Int32Ptr"
}
export interface CharPtrPtr extends NativePointer {
    __brand: "CharPtrPtr"
}

export interface EmscriptenModule {
    // this should match emcc -s EXPORTED_FUNCTIONS
    _malloc(size: number): VoidPtr;
    _free(ptr: VoidPtr): void;
    _sbrk(size: number): VoidPtr;
    _posix_memalign(res: VoidPtrPtr, alignment: number, size: number): number;

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
    lengthBytesUTF8(str: string): number;
    stackSave(): VoidPtr;
    stackRestore(stack: VoidPtr): void;
    stackAlloc(size: number): VoidPtr;
    safeSetTimeout(func: Function, delay: number): number;
}

export type InstantiateWasmSuccessCallback = (instance: WebAssembly.Instance, module: WebAssembly.Module | undefined) => void;
export type InstantiateWasmCallBack = (imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback) => any;

export type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;
