// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CreateDotnetRuntimeType, DotnetHostBuilder, DotnetModuleConfig, ModuleAPI, LoaderConfig, IMemoryView, Assets, Asset, AssetEntry, AssemblyAsset, AssetBehaviors, BootModule, GlobalizationMode, IcuAsset, JsAsset, LoadBootResourceCallback, LoadingResource, PdbAsset, ResourceExtensions, ResourceList, RuntimeAPI, SymbolsAsset, VfsAsset, WasmAsset, WebAssemblyBootResourceType, dotnet, exit } from "./public-api";
import type { EmscriptenModule } from "./emscripten";

declare const createDotnetRuntime: CreateDotnetRuntimeType;

declare global {
    function getDotnetRuntime(runtimeId: number): RuntimeAPI | undefined;
}

export default createDotnetRuntime;

export {
    EmscriptenModule,
    RuntimeAPI, ModuleAPI, DotnetHostBuilder, DotnetModuleConfig, CreateDotnetRuntimeType, LoaderConfig, IMemoryView, Assets, Asset, AssetEntry, AssemblyAsset, AssetBehaviors, BootModule, GlobalizationMode, IcuAsset, JsAsset, LoadBootResourceCallback, LoadingResource, PdbAsset, ResourceExtensions, ResourceList, SymbolsAsset, VfsAsset, WasmAsset, WebAssemblyBootResourceType,
    dotnet, exit
};
