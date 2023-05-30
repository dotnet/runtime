// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { assert_legacy_interop } from "../pthreads/shared";
import { mono_wasm_new_root } from "../roots";
import { stringToMonoStringRoot } from "../strings";
import { MonoString } from "../types/internal";

/**
 * @deprecated Not GC or thread safe
 */
export function js_string_to_mono_string(string: string): MonoString {
    assert_legacy_interop();
    const temp = mono_wasm_new_root<MonoString>();
    try {
        stringToMonoStringRoot(string, temp);
        return temp.value;
    } finally {
        temp.release();
    }
}
