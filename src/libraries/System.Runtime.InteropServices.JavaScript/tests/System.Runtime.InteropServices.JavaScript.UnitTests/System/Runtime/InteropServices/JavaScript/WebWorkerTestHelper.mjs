let dllExports;

let jsState = {};

let runtime;

export async function setup() {
    try {
        if (!getDotnetRuntime) {
            throw new Error("getDotnetRuntime is null or undefined");
        }
        if (!runtime) {
            runtime = getDotnetRuntime(0);
        }
        if (!runtime) {
            console.error(getDotnetRuntime);
            throw new Error("runtime is null or undefined");
        }
        dllExports = await runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests.dll");
        if (!dllExports) {
            throw new Error("dllExports is null or undefined");
        }
        jsState.id = getRndInteger(0, 1000);
        jsState.tid = getTid();
    }
    catch (e) {
        console.error("MONO_WASM: WebWorkerTestHelper.setup failed: " + JSON.stringify(globalThis.monoThreadInfo, null, 2));
        console.error("MONO_WASM: WebWorkerTestHelper.setup failed: " + e.toString());
        throw e;
    }
}

export function getState() {
    return jsState;
}

export function validateState(state) {
    try {
        if (!state) {
            throw new Error("state is null or undefined");
        }
        const isvalid = state.tid === jsState.tid && state.id === jsState.id;
        if (!isvalid) {
            console.log("Expected: ", JSON.stringify(jsState));
            console.log("Actual: ", JSON.stringify(state));
        }
        return isvalid;
    }
    catch (e) {
        console.error("MONO_WASM: WebWorkerTestHelper.validateState failed: " + e.toString());
        throw e;
    }
}

export async function promiseState() {
    await delay(10);
    return getState();
}

export async function promiseValidateState(state) {
    await delay(10);
    return validateState(state);
}

export function getTid() {
    return runtime.Module["_pthread_self"]();
}

export function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms))
}

export function getRndInteger(min, max) {
    return Math.floor(Math.random() * (max - min)) + min;
}
