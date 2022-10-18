import cwraps from "./cwraps";
import { Module } from "./imports";
import { MonoMethod } from "./types";

export const enum MeasuredBlock {
    emscriptenStartup = "mono.emscriptenStartup",
    instantiateWasm = "mono.instantiateWasm",
    preInit = "mono.preInit",
    preRun = "mono.preRun",
    onRuntimeInitialized = "mono.onRuntimeInitialized",
    postRun = "mono.postRun",
    loadRuntime = "mono.loadRuntime",
    bindingsInit = "mono.bindingsInit",
    bindJsFunction = "mono.bindJsFunction:",
    bindCsFunction = "mono.bindCsFunction:",
    callJsFunction = "mono.callJsFunction:",
    callCsFunction = "mono.callCsFunction:",
    getAssemblyExports = "mono.getAssemblyExports:",
    instantiateAsset = "mono.instantiateAsset:",
    interp = "interp:",
}

let uniqueId = 0;
export function startMeasure(block: string, id?: string): PerformanceMark {
    if (performance && performance.mark) {
        if (id) {
            uniqueId++;
            return performance.mark(`[${uniqueId}]${block}${id}`);
        }
        return performance.mark(block);
    }
    return undefined as any;
}

export function endMeasure(start: PerformanceMark) {
    if (start) {
        performance.measure(start.name, start.name);
        performance.clearMarks(start.name);
    }
}

export function mono_wasm_timestamp() {
    return performance.now();
}

const methodNames: Map<number, string> = new Map();
export function mono_wasm_measure(method: MonoMethod, start: number) {
    if (performance && performance.measure) {
        let methodName = methodNames.get(method as any);
        if (!methodName) {
            const chars = cwraps.mono_wasm_method_get_name(method);
            methodName = MeasuredBlock.interp + Module.UTF8ToString(chars);
            methodNames.set(method as any, methodName);
            Module._free(chars as any);
        }
        performance.measure(methodName, {
            start
        });
    }
}
