// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { VoidPtrNull } from "../types/internal";
import { runtimeHelpers } from "./module-exports";
import { VoidPtr } from "../types/emscripten";
import { isSurrogate } from "./helpers";

export function mono_wasm_change_case (culture: number, cultureLength: number, src: number, srcLength: number, dst: number, dstLength: number, toUpper: number): VoidPtr {
    try {
        const cultureName = runtimeHelpers.utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        if (!cultureName)
            throw new Error("Cannot change case, the culture name is null.");
        const input = runtimeHelpers.utf16ToStringLoop(src, src + 2 * srcLength);
        const result = toUpper ? input.toLocaleUpperCase(cultureName) : input.toLocaleLowerCase(cultureName);

        if (result.length <= input.length) {
            runtimeHelpers.stringToUTF16(dst, dst + 2 * dstLength, result);
            return VoidPtrNull;
        }
        // workaround to maintain the ICU-like behavior
        const heapI16 = runtimeHelpers.localHeapViewU16();
        let jump = 1;
        if (toUpper) {
            for (let i = 0; i < input.length; i += jump) {
                // surrogate parts have to enter ToUpper/ToLower together to give correct output
                if (isSurrogate(input, i)) {
                    jump = 2;
                    const surrogate = input.substring(i, i + 2);
                    const upperSurrogate = surrogate.toLocaleUpperCase(cultureName);
                    const appendedSurrogate = upperSurrogate.length > 2 ? surrogate : upperSurrogate;
                    appendSurrogateToMemory(heapI16, dst, appendedSurrogate, i);

                } else {
                    jump = 1;
                    const upperChar = input[i].toLocaleUpperCase(cultureName);
                    const appendedChar = upperChar.length > 1 ? input[i] : upperChar;
                    runtimeHelpers.setU16_local(heapI16, dst + i * 2, appendedChar.charCodeAt(0));
                }
            }
        } else {
            for (let i = 0; i < input.length; i += jump) {
                // surrogate parts have to enter ToUpper/ToLower together to give correct output
                if (isSurrogate(input, i)) {
                    jump = 2;
                    const surrogate = input.substring(i, i + 2);
                    const upperSurrogate = surrogate.toLocaleLowerCase(cultureName);
                    const appendedSurrogate = upperSurrogate.length > 2 ? surrogate : upperSurrogate;
                    appendSurrogateToMemory(heapI16, dst, appendedSurrogate, i);
                } else {
                    jump = 1;
                    const lowerChar = input[i].toLocaleLowerCase(cultureName);
                    const appendedChar = lowerChar.length > 1 ? input[i] : lowerChar;
                    runtimeHelpers.setU16_local(heapI16, dst + i * 2, appendedChar.charCodeAt(0));
                }
            }
        }
        return VoidPtrNull;
    } catch (ex: any) {
        return runtimeHelpers.stringToUTF16Ptr(ex.toString());
    }
}

function appendSurrogateToMemory (heapI16: Uint16Array, dst: number, surrogate: string, idx: number) {
    runtimeHelpers.setU16_local(heapI16, dst + idx * 2, surrogate.charCodeAt(0));
    runtimeHelpers.setU16_local(heapI16, dst + (idx + 1) * 2, surrogate.charCodeAt(1));
}
