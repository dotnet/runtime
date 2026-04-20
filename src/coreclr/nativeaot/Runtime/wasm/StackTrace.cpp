// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"

#ifdef HOST_BROWSER
#include <stdint.h>
#include <emscripten.h>

// Recieve pointers as JS-native types to avoid BigInt overheads on 64 bit
// and the need to worry about normalizing large (> int.MaxValue) values.
using JSPointerType = double;

EM_JS_DEPS(RhpBrowserStackTraceDependencies, "$wasmTable");

// It is a shortcut that this method is an FCall (i. e. called in cooperative mode) - computing the stack trace
// is a heavy operation that should ideally be done in preemptive mode. However, doing it this way allows us to
// avoid the complexity of skipping the PI stub managed frame that can be part of a QCall sequence in Debug code.
//
EM_JS(int32_t, RhpGetCurrentBrowserThreadStackTrace, (void* pShadowStack, JSPointerType pOutputBuffer, int allFramesAsJS), {
#ifdef TARGET_64BIT
    const POINTER_SIZE = 8;
    const POINTER_LOG2 = 3;
#else
    const POINTER_SIZE = 4;
    const POINTER_LOG2 = 2;
#endif
    // Not ":wasm-function[" because of a WebKit Issue (WKI): https://bugs.webkit.org/show_bug.cgi?id=278991.
    const WASM_FUNCTION_TEXT = "wasm-function[";
    const WASM_OFFSET_TEXT = "]:0x";

    // Our callers must employ the following pattern:
    //   int size = RhpGetCurrentBrowserThreadStackTrace(null)
    //   IntPtr[] data = new IntPtr[size];
    //   RhpGetCurrentBrowserThreadStackTrace(ref data[0])
    // This way, we avoid fetching the same stack trace from the JS engine twice.
    //
    const isMeasurementPhase = pOutputBuffer === 0;
    const jsStackTrace = isMeasurementPhase ? new Error().stack : Module.RhpGetCurrentBrowserThreadStackTraceValue;

    let actualBufferLength = 0;
    let callerModuleId = "";
    for (let currentIndex = 0, currentFrameEndIndex; currentIndex < jsStackTrace.length; currentIndex = currentFrameEndIndex + 1)
    {
        currentFrameEndIndex = jsStackTrace.indexOf('\n', currentIndex);
        if (currentFrameEndIndex < 0)
            currentFrameEndIndex = jsStackTrace.length;

        // Unfortunately, we do have to rely here on the (undocumented) optimization that JS engines perform with string
        // slicing, where the returned object is what in C# terms would be Memory<T>, not a full string.
        const currentFrame = jsStackTrace.slice(currentIndex, currentFrameEndIndex);
        let wasmFuncStartIndex = currentFrame.indexOf(WASM_FUNCTION_TEXT);

        // Only start giving out frames once we've hit our managed WASM caller. All known engines implement enough of
        // the stack trace handling for us to always find _some_ WASM frame (even if we end up reporting it as JS one).
        if (wasmFuncStartIndex < 0 && actualBufferLength === 0)
            continue;

        if (wasmFuncStartIndex >= 0 && currentFrame.endsWith(callerModuleId, wasmFuncStartIndex) && !allFramesAsJS) {
            if (actualBufferLength === 0) {
                // Unfortunately, there is no 100% reliable way to identify which part of the standard
                // "WASM stack frame string" is the URL field, so we have to assume 'known' formats:
                // V8  : at ForeignModuleFrame (wasm://wasm/ec89ddd2:wasm-function[1000]:0x1797)
                // FF  : ForeignModuleFrame@http://localhost:6931/HelloWasm.js line 1462 > WebAssembly.Module:wasm-function[13839]:0x14482
                // JSC : ForeignModule.wasm-function[ForeignModuleFrame]@[wasm code]
                let idIndex = currentFrame.lastIndexOf('//', wasmFuncStartIndex);
                if (idIndex < 0)
                    idIndex = 0; // The JSC case.
                callerModuleId = currentFrame.slice(idIndex, wasmFuncStartIndex);
            }

            // WASM or unknown frame.
            if (!isMeasurementPhase) {
                let wasmFunctionIndex = 0;
                let wasmFunctionOffset = 0;

                wasmFuncStartIndex += WASM_FUNCTION_TEXT.length;
                let wasmFuncEndIndex = currentFrame.indexOf(']', wasmFuncStartIndex);
                if (wasmFuncEndIndex >= 0) {
                    // Add bias to make room for 'null' and the EDI separator.
                    wasmFunctionIndex = parseInt(currentFrame.slice(wasmFuncStartIndex, wasmFuncEndIndex), 10) + 2;
                    if (!isNaN(wasmFunctionIndex) && currentFrame.startsWith(WASM_OFFSET_TEXT, wasmFuncEndIndex)) {
                        wasmFunctionOffset = parseInt(currentFrame.slice(wasmFuncEndIndex + WASM_OFFSET_TEXT.length), 16);
                    }
                }

                Module.HEAP32[pOutputBuffer >>> 2] = wasmFunctionIndex; // Note that NaNs will turn into zeroes here
                pOutputBuffer += POINTER_SIZE;
                Module.HEAP32[pOutputBuffer >>> 2] = wasmFunctionOffset;
                pOutputBuffer += POINTER_SIZE;
            }

            actualBufferLength += 2;
        } else { // JS frame.
            const lengthInChunks = (2 * currentFrame.length + (POINTER_SIZE - 1)) >> POINTER_LOG2;

            if (!isMeasurementPhase) {
                Module.HEAP32[pOutputBuffer >>> 2] = -currentFrame.length;
                pOutputBuffer += POINTER_SIZE;

                for (let i = 0; i < currentFrame.length; i++) // TODO-LLVM: is there a faster way to do this?
                {
                    Module.HEAP16[(pOutputBuffer >>> 1) + i] = currentFrame.charCodeAt(i);
                }
                pOutputBuffer += lengthInChunks * POINTER_SIZE;
            }

            actualBufferLength += 1 + lengthInChunks;
        }
    }

    Module.RhpGetCurrentBrowserThreadStackTraceValue = isMeasurementPhase ? jsStackTrace : null;
    return actualBufferLength;
});

// Fill out the array of { indirect table index, biased wasm function index } tuples.
// We exploit the fact that by the WASM JS API spec, JS objects that represent funcrefs in
// the indirect function table must have the 'name' property set to their function's index.
//
// Note that this "bulk" version is just an optimization. We want to avoid WASM<->JS hops
// for this array.
//
EM_JS(void, RhpInitializeStackTraceIpMap, (JSPointerType pEntries, int count), {
#ifdef TARGET_64BIT
    const POINTER_SIZE = 8;
#else
    const POINTER_SIZE = 4;
#endif
    for (let i = 0; i < count; i++) {
        const fptr = Module.HEAP32[pEntries >>> 2];
        const func = wasmTable.get(fptr);
        let wasmFuncIndex = parseInt(func.name) + 2; // Add bias.
        if (isNaN(wasmFuncIndex))
            wasmFuncIndex = 0; // Be defensive against future extensions (e. g. JS Builtins) and WKI.

        pEntries += POINTER_SIZE;
        Module.HEAP32[pEntries >>> 2] = wasmFuncIndex;
        pEntries += POINTER_SIZE;
    }
});

EM_JS(int32_t, RhpGetBiasedWasmFunctionIndexForFunctionPointer, (JSPointerType fptr), {
    const func = wasmTable.get(fptr);
    let wasmFuncIndex = parseInt(func.name) + 2; // Add bias.
    if (isNaN(wasmFuncIndex))
        wasmFuncIndex = 0; // Be defensive against future extensions (e. g. JS Builtins) and WKI.
    return wasmFuncIndex;
});
#endif // HOST_BROWSER

FCIMPL1(void*, RhFindMethodStartAddress, void* addr)
{
    // Our stack trace "IP"s are method-level and so do not require adjustment.
    return addr;
}
FCIMPLEND
