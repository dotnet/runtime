let dllExports;

const runtime = getDotnetRuntime(0);

let jsState = {};

export async function setup() {
    dllExports = await runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests.dll");
    jsState.id = getRndInteger(0, 1000);
    jsState.tid = getTid();
}

export function getState() {
    return jsState;
}

export function validateState(state) {
    const isvalid = state.tid === jsState.tid && state.id === jsState.id;
    if (!isvalid) {
        console.log("Expected: ", JSON.stringify(jsState));
        console.log("Actual: ", JSON.stringify(state));
    }
    return isvalid;
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
