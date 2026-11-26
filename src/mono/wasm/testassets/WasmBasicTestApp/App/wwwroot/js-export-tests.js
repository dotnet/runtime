// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { exit } from "./_framework/dotnet.js";
const output = message => console.log(`TestOutput -> ${message}`);
const check = (condition, message) => {
    if (!condition) throw new Error(message);
};

export async function runJSExportTest(scenario, getAssemblyExports) {
    if (scenario === "LegacyJSExports") {
        const legacy = (await getAssemblyExports("LegacyExports.dll")).LegacyExports.Exports;
        check(legacy.Add(17, 25) === 42, "Legacy integer export failed");
        check(legacy.Add(-17, 4) === -13, "Repeated legacy export invocation failed");
        const text = "Legacy exports: \u9FC0 \uD83D\uDE00";
        check(legacy.Echo(text) === text, "Legacy string export failed");
        const value = { name: "legacy", value: 42 };
        check(legacy.EchoObject(value) === value, "Legacy JSObject export failed");
        check(legacy.Nested.Negate(17) === -17, "Legacy nested export failed");
        let exceptionMessage;
        try {
            legacy.Throw();
        } catch (error) {
            exceptionMessage = error.message;
        }
        check(exceptionMessage === "Legacy export exception", "Legacy exception propagation failed");
        check(await legacy.AddAsync(19, 23) === 42, "Legacy async export failed");
        output("Legacy JSExports passed");
    } else {
        check(scenario === "DeferredJSExports", `Unknown JSExport scenario: ${scenario}`);
        for (const name of ["System.Private.CoreLib", "System.Private.CoreLib.dll"]) {
            check(Object.keys(await getAssemblyExports(name)).length === 0, `Unexpected exports from ${name}`);
        }
        output("Empty BCL exports passed");
        const library = await getAssemblyExports("Library.dll");
        const again = await getAssemblyExports("Library");
        const counting = (await getAssemblyExports("CountingLibrary")).AssemblyProviderTest.CountingExports;
        output("lazy:discovered");
        const add = counting.Add;
        const hashAdd = counting["Add.1"];
        const unused = counting.Unused;
        check(add === hashAdd && typeof unused === "function" && counting.Missing === undefined &&
            Object.keys(counting).includes("Unused.2"), "Deferred export discovery changed the object shape");
        output("lazy:read");
        globalThis.probeDeferredBindingReentry = () => {
            let rejected = 0;
            for (let attempt = 0; attempt < 2; attempt++) {
                try {
                    add(0, 0);
                } catch (error) {
                    if (error.message !== "A JavaScript export cannot be bound recursively." &&
                        error.message !== "JSExportReentrantBinding") throw error;
                    rejected++;
                }
            }
            check(rejected === 2, "Repeated reentrant bindings were not rejected");
        };
        let message;
        try {
            add(-1, 0);
        } catch (error) {
            message = error.message;
        } finally {
            delete globalThis.probeDeferredBindingReentry;
        }
        check(message === "Deferred export failure", `First deferred invocation did not propagate its exception: ${message}`);
        output("lazy:first");
        check(hashAdd(19, 23) === 42 && counting.Add === add, "Repeated deferred invocation or function identity failed");
        output("lazy:repeated");
        let bindingFailed = false;
        try {
            await counting.RetryAsync(41);
        } catch (error) {
            if (!error.message.includes("Deferred binding failure")) throw error;
            bindingFailed = true;
        }
        check(bindingFailed && await counting.RetryAsync(41) === 42, "A failed async binding was not rejected and retried");

        // Generated signature hashes distinguish Add(int, int) from Add(int).
        const addTwoIntsHash = 1568108894;
        const addOneIntHash = 1018901745;
        const exports = library.AssemblyProviderTest;
        check(exports.Exports.Add(17, 25) === 42 &&
            exports.Exports[`Add.${addTwoIntsHash}`] === exports.Exports.Add &&
            exports.Exports[`Add.${addOneIntHash}`](41) === 42 &&
            exports.Exports.Echo("type-map") === "type-map" &&
            await exports.Exports.AddAsync(20, 22) === 42 &&
            exports.InaccessibleExports.Private.Add(17, 25) === 42 &&
            exports.InaccessibleExports.Protected.Negate(-42) === 42 &&
            again.AssemblyProviderTest.Exports.Add(19, 23) === 42, "Mapped JSExport invocation failed");
        output("Mapped JSExports passed");
    }
    exit(0);
}
