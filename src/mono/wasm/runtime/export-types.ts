// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { BINDINGType, DotnetPublicAPI, MONOType } from "./exports";
import { DotnetModuleConfig, MonoArray, MonoObject, MonoString } from "./types";
import { EmscriptenModule, VoidPtr } from "./types/emscripten";

// -----------------------------------------------------------
// this files has all public exports from the dotnet.js module
// -----------------------------------------------------------

export type createDotnetRuntimeType = (moduleFactory: (api: DotnetPublicAPI) => DotnetModuleConfig) => Promise<DotnetPublicAPI>;

// Here, declare things that go in the global namespace, or augment existing declarations in the global namespace
declare global {
    function getDotnetRuntime(runtimeId: number): DotnetPublicAPI | undefined;
}

declare const createDotnetRuntime: createDotnetRuntimeType;
export default createDotnetRuntime;


export {
    VoidPtr,
    MonoObject, MonoString, MonoArray,
    BINDINGType, MONOType, EmscriptenModule,
    DotnetPublicAPI, DotnetModuleConfig,
};

