// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { BootJsonData, ICUDataMode } from "./blazor/BootConfig";
import type { IMemoryView } from "./marshal";
import type { CreateDotnetRuntimeType, DotnetModuleConfig, RuntimeAPI, MonoConfig, ModuleAPI, AssetEntry, ResourceRequest } from "./types-api";
import type { EmscriptenModule } from "./types/emscripten";
import type { dotnet, exit } from "./exports";

// -----------------------------------------------------------
// this files has all public exports from the dotnet.js module
// -----------------------------------------------------------

// Here, declare things that go in the global namespace, or augment existing declarations in the global namespace
declare global {
    function getDotnetRuntime(runtimeId: number): RuntimeAPI | undefined;
}

declare const createDotnetRuntime: CreateDotnetRuntimeType;

export default createDotnetRuntime;

export {
    EmscriptenModule,
    RuntimeAPI, ModuleAPI, DotnetModuleConfig, CreateDotnetRuntimeType, MonoConfig, IMemoryView, AssetEntry, ResourceRequest, BootJsonData, ICUDataMode,
    dotnet, exit
};
