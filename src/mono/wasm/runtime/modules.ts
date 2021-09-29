// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference path="./types/emscripten.d.ts" />
/// <reference path="./types/v8.d.ts" />

export var Module: t_Module;
export var MONO: any;
export var BINDING: any;

export function setMONO(mono: any, module: t_Module & any) {
    Module = module;
    MONO = mono;
}

export function setBINDING(binding: any, module: t_Module & any) {
    Module = module;
    BINDING = binding;
}