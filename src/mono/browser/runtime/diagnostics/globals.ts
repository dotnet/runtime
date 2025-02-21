
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagHelpers, GlobalObjects, LoaderHelpers, RuntimeHelpers, DotnetModuleInternal } from "../types/internal";

export let _diagModuleLoaded = false; // please keep it in place also as rollup guard

export let diagHelpers: DiagHelpers = null as any;
export let runtimeHelpers: RuntimeHelpers = null as any;
export let loaderHelpers: LoaderHelpers = null as any;
export let Module: DotnetModuleInternal = null as any;

export function setRuntimeGlobalsImpl (globalObjects: GlobalObjects): void {
    if (_diagModuleLoaded) {
        throw new Error("Diag module already loaded");
    }
    _diagModuleLoaded = true;
    diagHelpers = globalObjects.diagHelpers;
    runtimeHelpers = globalObjects.runtimeHelpers;
    loaderHelpers = globalObjects.loaderHelpers;
    Module = globalObjects.module;
}
