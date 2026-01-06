// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


import { } from "../../Common/JavaScript/cross-linked";
declare global {
    export const DOTNET: any;
    export function _emscripten_force_exit(exitCode: number): void;
    export function _exit(exitCode: number, implicit?: boolean): void;
    export function _GetDotNetRuntimeContractDescriptor(): void;
    export function _SystemJS_ExecuteTimerCallback(): void;
    export function _SystemJS_ExecuteBackgroundJobCallback(): void;
}
