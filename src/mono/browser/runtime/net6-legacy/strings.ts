// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert } from "../globals";
import { mono_wasm_new_root } from "../roots";
import { interned_string_table, mono_wasm_empty_string, stringToInternedMonoStringRoot, stringToMonoStringRoot } from "../strings";
import { MonoString, is_nullish } from "../types/internal";

import { assert_legacy_interop } from "./method-binding";

/**
 * @deprecated Not GC or thread safe
 */
export function stringToMonoStringUnsafe(string: string): MonoString {
    assert_legacy_interop();
    const temp = mono_wasm_new_root<MonoString>();
    try {
        stringToMonoStringRoot(string, temp);
        return temp.value;
    } finally {
        temp.release();
    }
}

// this is only used in legacy unit tests
export function stringToMonoStringIntern(string: string): string {
    if (string.length === 0)
        return mono_wasm_empty_string;

    const root = mono_wasm_new_root<MonoString>();
    try {
        stringToInternedMonoStringRoot(string, root);
        const result = interned_string_table.get(root.value);
        mono_assert(!is_nullish(result), "internal error: interned_string_table did not contain string after stringToMonoStringIntern");
        return result;
    }
    finally {
        root.release();
    }
}
