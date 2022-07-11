// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { BINDINGType, DotnetPublicAPI, MONOType } from "./exports";
import { IDisposable, IMemoryView, ManagedError, ManagedObject, MemoryViewType } from "./marshal";
import { DotnetModuleConfig, MonoArray, MonoObject, MonoString } from "./types";
import { EmscriptenModule, TypedArray, VoidPtr } from "./types/emscripten";

// -----------------------------------------------------------
// this files has all public exports from the dotnet.js module
// -----------------------------------------------------------

declare function createDotnetRuntime(moduleFactory: DotnetModuleConfig | ((api: DotnetPublicAPI) => DotnetModuleConfig)): Promise<DotnetPublicAPI>;
declare type CreateDotnetRuntimeType = typeof createDotnetRuntime;

// Here, declare things that go in the global namespace, or augment existing declarations in the global namespace
declare global {
    function getDotnetRuntime(runtimeId: number): DotnetPublicAPI | undefined;
}

export default createDotnetRuntime;


/**
 * Span class is JS wrapper for System.Span<T>. This view doesn't own the memory, nor pin the underlying array.
 * It's ideal to be used on call from C# with the buffer pinned there or with unmanaged memory.
 * It is disposed at the end of the call to JS.
 */
declare class Span implements IMemoryView, IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    set(source: TypedArray, targetOffset?: number | undefined): void;
    copyTo(target: TypedArray, sourceOffset?: number | undefined): void;
    slice(start?: number | undefined, end?: number | undefined): TypedArray;
    get length(): number;
    get byteLength(): number;
}

/**
 * ArraySegment class is JS wrapper for System.ArraySegment<T>. 
 * This wrapper would also pin the underlying array and hold GCHandleType.Pinned until this JS instance is collected.
 * User could dispose it manualy.
 */
declare class ArraySegment implements IMemoryView, IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    set(source: TypedArray, targetOffset?: number | undefined): void;
    copyTo(target: TypedArray, sourceOffset?: number | undefined): void;
    slice(start?: number | undefined, end?: number | undefined): TypedArray;
    get length(): number;
    get byteLength(): number;
}

export {
    VoidPtr,
    MonoObject, MonoString, MonoArray,
    BINDINGType, MONOType, EmscriptenModule,
    DotnetPublicAPI, DotnetModuleConfig, CreateDotnetRuntimeType,
    IMemoryView, MemoryViewType, ManagedObject, ManagedError, Span, ArraySegment
};

