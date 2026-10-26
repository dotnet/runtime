// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

const exportEvents = [];
let checkingExportOrder = false;

function displayMeaning(meaning) {
    if (checkingExportOrder) {
        exportEvents.push("import");
        queueMicrotask(() => exportEvents.push("microtask"));
    }
    if (meaning === -1) {
        throw new Error("M2 import failure");
    }
    if (meaning === -2) {
        return null;
    }
    if (meaning === -3) {
        return undefined;
    }
    document.getElementById("out").textContent = `${meaning}`;
    return meaning + 1;
}

try {
    const { setModuleImports, getAssemblyExports } = await dotnet
        .withConfig({ appendElementOnExit: true, exitOnUnhandledError: true, logExitCode: true })
        .withWorker()
        .withApplicationArgumentsFromQuery()
        .withDiagnosticTracing(true)
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning
            }
        }
    });
    const exports = await getAssemblyExports("Wasm.Browser.Sample");
    for (let value = 0; value < 64; value++) {
        const result = exports.Sample.Test.Increment(value);
        if (typeof result !== "number" || result !== value + 1) {
            throw new Error("Synchronous export returned an unexpected value.");
        }
    }
    try {
        exports.Sample.Test.Increment(-1);
        throw new Error("Expected a managed export exception.");
    } catch (error) {
        if (!error.message.includes("M3 export failure")) throw error;
        error.dispose?.();
    }
    exportEvents.push("before");
    checkingExportOrder = true;
    const importedResult = exports.Sample.Test.ThroughImport(41);
    checkingExportOrder = false;
    exportEvents.push("after");
    if (importedResult !== 42 || exportEvents.join(",") !== "before,import,after") {
        throw new Error("Synchronous export/import ordering changed.");
    }
    await Promise.resolve();
    if (exportEvents.join(",") !== "before,import,after,microtask") {
        throw new Error("Import microtask did not run after the synchronous export returned.");
    }
    try {
        exports.Sample.Test.ThroughImport(-1);
        throw new Error("Expected an export/import exception.");
    } catch (error) {
        if (!error.message.includes("M2 import failure")) throw error;
        error.dispose?.();
    }
    if (exports.Sample.Test.Increment(42) !== 43) {
        throw new Error("Synchronous export did not recover after an exception.");
    }
    console.log(`Synchronous export ordering: ${exportEvents.join(",")}`);
    console.log("Synchronous export round trips completed.");
    const exitCode = await dotnet.runMainAndExit();
    console.log(`Program has exited with code ${exitCode}.`);
    exit(exitCode);
}
catch (err) {
    if (!err || typeof err.status !== "number") {
        exit(2, err);
    }
}