// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference path="./types/emscripten.d.ts" />
/// <reference path="./types/v8.d.ts" />
import { t_MONO, t_ModuleExtension } from './mono/types'

export var Module: t_Module & t_ModuleExtension;
export var MONO: t_MONO;

export function setMONO(mono: t_MONO, module: t_Module & any) {
    Module = module;
    MONO = mono;
}