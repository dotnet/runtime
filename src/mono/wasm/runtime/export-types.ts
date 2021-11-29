// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNetPublicAPI } from "./exports";
import { EmscriptenModuleConfig } from "./types";

// -----------------------------------------------------------
// this files has all public exports from the dotnet.js module
// -----------------------------------------------------------

declare function createDotnetRuntime(moduleFactory: (api: DotNetPublicAPI) => EmscriptenModuleConfig): Promise<DotNetPublicAPI>;

// Here, declare things that go in the global namespace, or augment existing declarations in the global namespace
declare global {
    function getDotnetRuntime(runtimeId: number): DotNetPublicAPI | undefined;
}

export default createDotnetRuntime;