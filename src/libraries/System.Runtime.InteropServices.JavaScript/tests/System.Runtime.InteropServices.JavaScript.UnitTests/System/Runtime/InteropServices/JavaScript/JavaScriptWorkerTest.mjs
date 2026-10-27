// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export async function setup(runtime) {
    const exports = (await runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests"))
        .System.Runtime.InteropServices.JavaScript.Tests.WorkerInteropTest;
    const worker = runtime.getConfig().workerOptions !== undefined;
    runtime.setModuleImports("WorkerInteropTest", {
        echo: value => value,
        invokeEcho: value => {
            const result = exports.Echo(value);
            if (typeof result !== "number") throw new Error("Scalar export must return synchronously");
            return result;
        },
        invokeNested: value => exports.Nested(value),
        echoAsync: async value => {
            await new Promise(resolve => setTimeout(resolve, 0));
            return value;
        },
        invokeAsync: value => exports.EchoAsync(value),
        fail: value => {
            if (value === -1) throw new Error("Worker UI import failure");
            return value;
        },
        mode: () => {
            if (typeof document !== "object" || document.defaultView !== globalThis) {
                throw new Error("The registered import must execute on the UI thread");
            }
            const documentPresent = exports.HasDocument(0);
            if (documentPresent !== (worker ? 0 : 1)) {
                throw new Error("Managed execution is on the wrong thread");
            }
            return worker ? 1 : 0;
        },
    });
    // eslint-disable-next-line no-console
    console.log(`WorkerInteropTest: ${worker ? "worker" : "ordinary"} imports registered`);
}
