export const enum MeasuredBlock {
    emscriptenStartup = "emscriptenStartup",
    instantiateWasm = "instantiateWasm",
    preInit = "preInit",
    preRun = "preRun",
    onRuntimeInitialized = "onRuntimeInitialized",
    postRun = "postRun",
    loadRuntime = "loadRuntime",
    bindingsInit = "bindingsInit",
    bindJsFunction = "bindJsFunction:",
    bindCsFunction = "bindCsFunction:",
    getAssemblyExports = "getAssemblyExports:",
    loadDataArchive = "loadDataArchive:",
}

export function startMeasure(block: string) {
    if (performance) {
        performance.mark("start-" + block);
    }
}

export function endMeasure(block: string) {
    if (performance) {
        performance.mark("end-" + block);
        performance.measure(block, "start-" + block, "end-" + block);
    }
}