
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticHelpers, GlobalObjects, LoaderHelpers, RuntimeHelpers, DotnetModuleInternal } from "../types/internal";

export let _diagnosticModuleLoaded = false; // please keep it in place also as rollup guard

export let diagnosticHelpers: DiagnosticHelpers = null as any;
export let runtimeHelpers: RuntimeHelpers = null as any;
export let loaderHelpers: LoaderHelpers = null as any;
export let Module: DotnetModuleInternal = null as any;

export function setRuntimeGlobalsImpl (globalObjects: GlobalObjects): void {
    if (_diagnosticModuleLoaded) {
        throw new Error("Diag module already loaded");
    }
    _diagnosticModuleLoaded = true;
    diagnosticHelpers = globalObjects.diagnosticHelpers;
    runtimeHelpers = globalObjects.runtimeHelpers;
    loaderHelpers = globalObjects.loaderHelpers;
    Module = globalObjects.module;
}
