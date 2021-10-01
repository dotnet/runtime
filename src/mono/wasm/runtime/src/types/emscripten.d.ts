// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

declare interface ManagedPointer { }
declare interface NativePointer { }

declare interface MonoString extends ManagedPointer { }
declare interface VoidPtr extends NativePointer { }
declare interface CharPtr extends NativePointer { }
declare interface Int32Ptr extends NativePointer { }
declare interface CharPtrPtr extends NativePointer { }

declare var ENVIRONMENT_IS_WEB: boolean;
declare var ENVIRONMENT_IS_SHELL: boolean;
declare var ENVIRONMENT_IS_NODE: boolean;
declare var ENVIRONMENT_IS_WORKER: boolean;
declare var LibraryManager: any;

declare function autoAddDeps(a: object, b: string): void;
declare function mergeInto(a: object, b: object): void;

// TODO, what's wrong with EXPORTED_RUNTIME_METHODS ?
declare function locateFile(path: string, prefix?: string): string;

declare var Module: t_Module;

declare interface t_Module {
    HEAP8: Int8Array,
    HEAP16: Int16Array;
    HEAP32: Int32Array;
    HEAPU8: Uint8Array;
    HEAPU16: Uint16Array;
    HEAPU32: Uint32Array;
    HEAPF32: Float32Array;
    HEAPF64: Float64Array;

    // this should match emcc -s EXPORTED_FUNCTIONS
    _malloc(amnt: number): number;
    _free(amn: number): void;

    // this should match emcc -s EXPORTED_RUNTIME_METHODS
    print(message: string): void;
    ccall<T>(ident: string, returnType?: string | null, argTypes?: string[], args?: any[], opts?: any): T;
    cwrap<T extends Function>(ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    cwrap<T extends Function>(ident: string, ...args: any[]): T;
    setValue(ptr: number, value: number, type: string, noSafe?: number | boolean): void;
    getValue(ptr: number, type: string, noSafe?: number | boolean): number;
    UTF8ToString(arg: CharPtr): string;
    UTF8ArrayToString(str: TypedArray, heap: number[] | number, outIdx: number, maxBytesToWrite?: number): string;
    FS_createPath(parent: string, path: string, canRead?: boolean, canWrite?: boolean): string;
    FS_createDataFile(parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    removeRunDependency(id: string): void;
    addRunDependency(id: string): void;
}

declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;