// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

const exportEvents = [];
let checkingExportOrder = false;
let nestedMode = "off";
let nestedExport;
let nestedScalar;
let recordNestedOrder = false;
const nestedEvents = [];

function displayMeaning(meaning) {
    if (nestedMode !== "off") {
        if (recordNestedOrder) {
            nestedEvents.push(`import:${meaning}`);
            queueMicrotask(() => nestedEvents.push(`microtask:${meaning}`));
        }
        if (nestedMode === "siblings") {
            let sum = 0;
            for (let value = 0; value < 65; value++) sum += nestedScalar(value);
            return sum;
        }
        if ((nestedMode === "error" || nestedMode === "recover") && meaning === 2) {
            throw new Error("M4 inner import failure");
        }
        if (nestedMode === "recover" && meaning === 3) {
            let firstError;
            try { nestedExport(2); }
            catch (error) { firstError = error; }
            if (!firstError?.message.includes("M4 inner import failure")) throw new Error("Expected inner import failure.");
            try {
                nestedScalar(-1);
                throw new Error("Expected inner managed failure.");
            } catch (error) {
                if (!error.message.includes("M3 export failure")) throw error;
                error.dispose?.();
            }
            if (!firstError.message.includes("M4 inner import failure")) throw new Error("Nested error was overwritten.");
            firstError.dispose?.();
            return 90;
        }
        return meaning === 0 ? 1 : nestedExport(meaning - 1) + 1;
    }
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
    const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
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
    nestedExport = exports.Sample.Test.ThroughImport;
    nestedScalar = exports.Sample.Test.Increment;
    nestedMode = "chain";
    for (let value = 0; value < 32; value++) {
        if (nestedExport(4) !== 5) throw new Error("Nested export/import result changed.");
    }
    nestedMode = "siblings";
    if (nestedExport(99) !== 2145) throw new Error("Sibling calls exhausted an outer frame.");
    nestedMode = "error";
    try {
        nestedExport(4);
        throw new Error("Expected a nested import failure.");
    } catch (error) {
        if (!error.message.includes("M4 inner import failure")) throw error;
        error.dispose?.();
    }
    nestedMode = "recover";
    if (nestedExport(4) !== 91) throw new Error("Outer call did not recover from inner errors.");
    nestedMode = "chain";
    if (nestedExport(15) !== 16) throw new Error("Maximum supported nesting failed.");
    if (getConfig().workerOptions) {
        try {
            nestedExport(16);
            throw new Error("Expected nested export capacity failure.");
        } catch (error) {
            if (!error.message.includes("queue is full")) throw error;
            error.dispose?.();
        }
    }
    recordNestedOrder = true;
    nestedEvents.push("before");
    if (nestedExport(3) !== 4) throw new Error("Nested ordering result changed.");
    nestedEvents.push("after");
    const expectedNestedOrder = "before,import:3,import:2,import:1,import:0,after";
    if (nestedEvents.join(",") !== expectedNestedOrder) throw new Error("Nested call yielded to the event loop.");
    recordNestedOrder = false;
    await Promise.resolve();
    if (nestedEvents.join(",") !== `${expectedNestedOrder},microtask:3,microtask:2,microtask:1,microtask:0`) {
        throw new Error("Nested microtask ordering changed.");
    }
    nestedMode = "off";
    console.log(`Nested export ordering: ${nestedEvents.join(",")}`);
    console.log("Nested export round trips completed.");
    const exitCode = await dotnet.runMainAndExit();
    console.log(`Program has exited with code ${exitCode}.`);
    exit(exitCode);
}
catch (err) {
    if (!err || typeof err.status !== "number") {
        exit(2, err);
    }
}