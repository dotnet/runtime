// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { IMemoryView } from "../marshal";
import type { CreateDotnetRuntimeType, DotnetHostBuilder, DotnetModuleConfig, RuntimeAPI, MonoConfig, ModuleAPI, AssetEntry, GlobalizationMode, AssetBehaviors } from ".";
import type { EmscriptenModule } from "./emscripten";
import type { dotnet, exit } from "../loader/index";

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
    RuntimeAPI, ModuleAPI, DotnetHostBuilder, DotnetModuleConfig, CreateDotnetRuntimeType, MonoConfig, IMemoryView, AssetEntry, GlobalizationMode, AssetBehaviors,
    dotnet, exit
};
