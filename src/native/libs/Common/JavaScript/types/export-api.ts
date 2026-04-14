// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CreateDotnetRuntimeType, DotnetHostBuilder, DotnetModuleConfig, ModuleAPI, LoaderConfig, IMemoryView, AssetEntry, GlobalizationMode, AssetBehaviors, RuntimeAPI, dotnet, exit } from "./public-api";
import type { EmscriptenModule } from "./emscripten";

declare const createDotnetRuntime: CreateDotnetRuntimeType;

declare global {
    function getDotnetRuntime(runtimeId: number): RuntimeAPI | undefined;
}

export default createDotnetRuntime;

export {
    EmscriptenModule,
    RuntimeAPI, ModuleAPI, DotnetHostBuilder, DotnetModuleConfig, CreateDotnetRuntimeType, LoaderConfig, IMemoryView, AssetEntry, GlobalizationMode, AssetBehaviors,
    dotnet, exit
};
