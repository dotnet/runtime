// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function displayMeaning(meaning) {
    console.log(`Meaning of life is ${meaning}`);
    document.getElementById("out").innerHTML = `${meaning}`;
}

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

try {
    let releaseR2R;
    const r2rGate = new Promise(resolve => { releaseR2R = resolve; });
    const lateNames = new Set();
    let disableIntegrityCheck = false;
    const runtime = await dotnet
        .withConfig({ appendElementOnExit: true, exitOnUnhandledError: true, forwardConsole: true, logExitCode: true })
        .withDiagnosticTracing(true)
        .withConfigLoaded(config => {
            disableIntegrityCheck = !!config.disableIntegrityCheck;
            for (const asset of config.resources.r2rAssembly || []) lateNames.add(asset.name);
        })
        .withResourceLoader((type, name, uri, integrity) =>
            lateNames.has(name) ? r2rGate.then(() => fetch(uri, disableIntegrityCheck ? {} : { integrity })) : uri)
        .create();
    const { setModuleImports, getAssemblyExports, INTERNAL, getHeapU32 } = runtime;

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning
            }
        }
    });

    const exports = await getAssemblyExports("Wasm.Browser.Sample");
    const suspended = exports.Sample.Test.PrintMeaning(delay(2000).then(() => 42));
    if (lateNames.size) {
        if (exports.Sample.Test.DeferredProbe(41) !== 42) throw new Error("Interpreter probe failed.");
        const entryPoint = exports.Sample.Test.GetDeferredProbeEntryPoint() >>> 0;
        const interpretedCode = getHeapU32(entryPoint);
        console.log("Deferred R2R interpreter probe completed.");
        releaseR2R();
        await INTERNAL.waitForDeferredR2R();
        const firstAfterAttachment = exports.Sample.Test.DeferredProbe(41);
        const secondAfterAttachment = exports.Sample.Test.DeferredProbe(41);
        const state = INTERNAL.getDeferredR2RState();
        if (state.status !== "completed" || state.activationMode !== "lazy" || state.publishedMethods === 0 ||
            exports.Sample.Test.GetDeferredProbeEntryPoint() >>> 0 !== entryPoint ||
            getHeapU32(entryPoint) === interpretedCode ||
            (getHeapU32(entryPoint + 12) & 4) !== 0 ||
            firstAfterAttachment !== 42 || secondAfterAttachment !== 42) {
            throw new Error("Deferred R2R probe did not transition to compiled code.");
        }
        console.log(`Deferred R2R activated: ${state.publishedMethods} methods.`);
    }
    await suspended;
    const exitCode = await dotnet.run();
    console.log(`Program has exited with code ${exitCode}.`);
    exit(exitCode);
}
catch (err) {
    if (!err || typeof err.status !== "number") {
        exit(2, err);
    }
}