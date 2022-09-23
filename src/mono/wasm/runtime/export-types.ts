// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { createDotnetRuntime, CreateDotnetRuntimeType, DotnetModuleConfig, RuntimeAPI, MonoConfig, ModuleAPI } from "./types";
import { EmscriptenModule } from "./types/emscripten";

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

export {
    EmscriptenModule,
    RuntimeAPI, ModuleAPI, DotnetModuleConfig, CreateDotnetRuntimeType, MonoConfig,
    dotnet, exit
};
