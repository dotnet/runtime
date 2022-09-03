// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IDisposable, IMemoryView, MemoryViewType } from "./marshal";
import { AssetBehaviours, AssetEntry, createDotnetRuntime, CreateDotnetRuntimeType, DotnetModuleConfig, RuntimeAPI, LoadingResource, MonoConfig, ResourceRequest, ModuleAPI } from "./types";
import { EmscriptenModule, NativePointer, TypedArray } from "./types/emscripten";

// -----------------------------------------------------------
// this files has all public exports from the dotnet.js module
// -----------------------------------------------------------


// Here, declare things that go in the global namespace, or augment existing declarations in the global namespace
declare global {
    function getDotnetRuntime(runtimeId: number): RuntimeAPI | undefined;
}

export default createDotnetRuntime;

declare const dotnet: ModuleAPI["dotnet"];
declare const exit: ModuleAPI["exit"];

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
 * User could dispose it manually.
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

/**
 * Represents proxy to the System.Exception
 */
declare class ManagedError extends Error implements IDisposable {
    get stack(): string | undefined;
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
}

/**
 * Represents proxy to the System.Object
 */
declare class ManagedObject implements IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
}

export {
    EmscriptenModule, NativePointer,
    RuntimeAPI, ModuleAPI, DotnetModuleConfig, CreateDotnetRuntimeType, MonoConfig,
    AssetEntry, ResourceRequest, LoadingResource, AssetBehaviours,
    IMemoryView, MemoryViewType, ManagedObject, ManagedError, Span, ArraySegment,
    dotnet, exit
};

