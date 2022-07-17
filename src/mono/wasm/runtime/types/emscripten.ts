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
    HEAP8: Int8Array,
    HEAP16: Int16Array;
    HEAP32: Int32Array;
    HEAPU8: Uint8Array;
    HEAPU16: Uint16Array;
    HEAPU32: Uint32Array;
    HEAPF32: Float32Array;
    HEAPF64: Float64Array;

    // this should match emcc -s EXPORTED_FUNCTIONS
    _malloc(size: number): VoidPtr;
    _free(ptr: VoidPtr): void;

    // this should match emcc -s EXPORTED_RUNTIME_METHODS
    print(message: string): void;
    printErr(message: string): void;
    ccall<T>(ident: string, returnType?: string | null, argTypes?: string[], args?: any[], opts?: any): T;
    cwrap<T extends Function>(ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    cwrap<T extends Function>(ident: string, ...args: any[]): T;
    setValue(ptr: VoidPtr, value: number, type: string, noSafe?: number | boolean): void;
    setValue(ptr: Int32Ptr, value: number, type: string, noSafe?: number | boolean): void;
    getValue(ptr: number, type: string, noSafe?: number | boolean): number;
    UTF8ToString(ptr: CharPtr, maxBytesToRead?: number): string;
    UTF8ArrayToString(u8Array: Uint8Array, idx?: number, maxBytesToRead?: number): string;
    FS_createPath(parent: string, path: string, canRead?: boolean, canWrite?: boolean): string;
    FS_createDataFile(parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    FS_readFile(filename: string, opts: any): any;
    removeRunDependency(id: string): void;
    addRunDependency(id: string): void;
    stackSave(): VoidPtr;
    stackRestore(stack: VoidPtr): void;
    stackAlloc(size: number): VoidPtr;


    ready: Promise<unknown>;
    preInit?: (() => any)[];
    preRun?: (() => any)[];
    postRun?: (() => any)[];
    onAbort?: { (error: any): void };
    onRuntimeInitialized?: () => any;
    instantiateWasm: (imports: any, successCallback: Function) => any;
}

export declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;
