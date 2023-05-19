// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_external_root } from "../roots";
import { MonoString } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { js_string_to_mono_string_root } from "../strings";

export function pass_exception_details(ex: any, exceptionMessage: Int32Ptr){
    const exceptionJsString = ex.message + "\n" + ex.stack;
    const exceptionRoot = mono_wasm_new_external_root<MonoString>(<any>exceptionMessage);
    js_string_to_mono_string_root(exceptionJsString, exceptionRoot);
    exceptionRoot.release();
}
