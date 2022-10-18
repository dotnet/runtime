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