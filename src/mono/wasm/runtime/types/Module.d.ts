// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces and types that we use. Specifically, this file is for the EMSDK API

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/

// TODO PROPERLY SET THESE ANYS

declare namespace Module {
    let config: MonoConfig;
    let HEAP8: Int8Array;
    let HEAP16: Int16Array;
    let HEAP32: Int32Array;
    let HEAPU8: Uint8Array;
    let HEAPU16: Uint16Array;
    let HEAPU32: Uint32Array;
    let HEAPF32: Float32Array;
    let HEAPF64: Float64Array;

    let arguments: string[];
    let loadReadFiles: boolean;
    let printWithColors: boolean;
    let noExitRuntime: boolean;
    let noInitialRun: boolean;

    type preInit = Function | Function[];
    type preRun = Function | Function[];

    function locateFile (path: string, prefix: string): string;
    function onAbort (): void;
    function onRuntimeInitialized (): void;
    function destroy (obj: any): void;
    function getPreloadedPackage (remotePackageName: string, remotePackageSize: number): void;
    function _malloc (amnt: number): number;
    function _free (amn: number): void;
    function addRunDependency(id: string): void;
    function removeRunDependency(id: string): void;
    function mono_method_get_call_signature (method: any, mono_obj?: any): string;
    function print (message: string): void;

    function ccall <T extends Function> (ident: string, returnType?: string, argTypes?: string[], args?: any[] , opts?: any): T;
    function cwrap <T extends Function> (ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    function cwrap <T extends Function> (ident: string, ...args: any[]): T;

    function FS_createPath (parent: string | any, path: string, canRead?: boolean /* unused */, canWrite?: boolean /* unused */): string;
    function FS_createDataFile (parent: string | any, name: string, data: string, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    function setValue (ptr: number, value: number, type: string, noSafe?: number | boolean): void;
    function getValue (ptr: number, type: string, noSafe?: number | boolean ): number;
    function UTF8ToString (ptr: number, maxBytesToRead?: number): string;
    function UTF8ToString (arg: string): string;
    function UTF8ArrayToString (str: string, heap: number[] | number, outIdx: number, maxBytesToWrite?: number): string;
    function addFunction (func: Function, sig: string): void;
}

type MonoConfig = {
    assembly_root: string,
    debug_level: number,
    assets: (AssetEntry | AssetEntry | SatelliteAssemblyEntry | VfsEntry | IcuData)[],
    remote_sources: string[],
    [index: string]: any, // overflow for the "Extra" sections
} | {message: string, error: any};

// Types of assets that can be in the mono-config.js/mono-config.json file (taken from /src/tasks/WasmAppBuilder/WasmAppBuilder.cs)
type AssetEntry = {
    behavior: AssetBehaviours,
    name: string
}

interface AssemblyEntry extends AssetEntry {
    name: "assembly"
}

interface SatelliteAssemblyEntry extends AssetEntry {
    name: "resource",
    culture: string
}

interface VfsEntry extends AssetEntry {
    name: "vfs",
    virtual_path: string
}

interface IcuData extends AssetEntry {
    name: "icu",
    load_remote: boolean
}

// Note that since these are annoated as `declare const enum` they are replaces by tsc with their raw value during compilation
declare const enum AssetBehaviours {
    Resource = "resource",
    Assembly = "assembly",
    Heap = "heap",
    ICU = "icu",
    VFS = "vfs",
}
