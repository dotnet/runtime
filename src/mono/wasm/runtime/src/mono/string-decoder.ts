// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root } from './roots'
import cwraps from './cwraps'
import { Module } from '../runtime'

const mono_wasm_empty_string = "";

export class StringDecoder {

    private mono_wasm_string_root: any;
    private mono_text_decoder: TextDecoder | undefined | null;
    private mono_wasm_string_decoder_buffer: number | undefined;
    private interned_string_table: Map<number, string> | undefined;

    copy(mono_string: MonoString) {
        if (!this.interned_string_table || !this.mono_wasm_string_decoder_buffer) {
            this.mono_text_decoder = typeof TextDecoder !== 'undefined' ? new TextDecoder('utf-16le') : null;
            this.mono_wasm_string_root = mono_wasm_new_root();
            this.mono_wasm_string_decoder_buffer = Module._malloc(12);
            this.interned_string_table = new Map();
        }
        if (mono_string === 0)
            return null;

        this.mono_wasm_string_root.value = mono_string;

        let ppChars = this.mono_wasm_string_decoder_buffer + 0,
            pLengthBytes = this.mono_wasm_string_decoder_buffer + 4,
            pIsInterned = this.mono_wasm_string_decoder_buffer + 8;

        cwraps.mono_wasm_string_get_data(mono_string, ppChars, pLengthBytes, pIsInterned);

        let result = mono_wasm_empty_string;
        let lengthBytes = Module.HEAP32[pLengthBytes / 4],
            pChars = Module.HEAP32[ppChars / 4],
            isInterned = Module.HEAP32[pIsInterned / 4];

        if (pLengthBytes && pChars) {
            if (
                isInterned &&
                this.interned_string_table &&
                this.interned_string_table.has(<number>mono_string) //TODO remove 2x lookup
            ) {
                result = this.interned_string_table.get(<number>mono_string)!;
                // console.log("intern table cache hit", mono_string, result.length);
            } else {
                result = this.decode(pChars, pChars + lengthBytes);
                if (isInterned) {
                    // console.log("interned", mono_string, result.length);
                    this.interned_string_table.set(<number>mono_string, result);
                }
            }
        }

        this.mono_wasm_string_root.value = 0;
        return result;
    }

    private decode(start: number, end: number) {
        var str = "";
        if (this.mono_text_decoder) {
            // When threading is enabled, TextDecoder does not accept a view of a
            // SharedArrayBuffer, we must make a copy of the array first.
            // See https://github.com/whatwg/encoding/issues/172
            var subArray = typeof SharedArrayBuffer !== 'undefined' && Module.HEAPU8.buffer instanceof SharedArrayBuffer
                ? Module.HEAPU8.slice(start, end)
                : Module.HEAPU8.subarray(start, end);

            str = this.mono_text_decoder.decode(subArray);
        } else {
            for (var i = 0; i < end - start; i += 2) {
                var char = Module.getValue(start + i, 'i16');
                str += String.fromCharCode(char);
            }
        }

        return str;
    }
}
