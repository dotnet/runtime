let dllExports;

const runtime = getDotnetRuntime(0);

export async function setup() {
    dllExports = await runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests.dll");
}

export function getTid() {
    return runtime.Module["_pthread_self"]();
}
