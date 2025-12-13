// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function fixupPointer(signature: any, shiftAmount: number): any {
    return ((signature as any) >>> shiftAmount) as any;
}

export function normalizeException(ex: any) {
    let res = "unknown exception";
    if (ex) {
        res = ex.toString();
        const stack = ex.stack;
        if (stack) {
            // Some JS runtimes insert the error message at the top of the stack, some don't,
            //  so normalize it by using the stack as the result if it already contains the error
            if (stack.startsWith(res))
                res = stack;
            else
                res += "\n" + stack;
        }

        // TODO-WASM
        // res = mono_wasm_symbolicate_string(res);
    }
    return res;
}

export function isRuntimeRunning(): boolean {
    // TODO-WASM
    return true;
}

export function assertRuntimeRunning(): void {
    // TODO-WASM
}

export function assertJsInterop(): void {
    // TODO-WASM
}

export function startMeasure(): number {
    // TODO-WASM
    return 0;
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function endMeasure(mark: number, fqn: string, additionalInfo: string): void {
    // TODO-WASM
}
