// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

try {
    const { getAssemblyExports, runMain, Module } = await dotnet
        .withElementOnExit()
        .withExitOnUnhandledError()
        .withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        .withEnvironmentVariable("MONO_LOG_MASK", "gc")
        .create();

    await runMain("Wasm.Browser.Sample", []);

    let linear;
    let tm;
    const library = await getAssemblyExports("Wasm.Browser.Sample");
    const testClass = library.Sample.TestClass;
    console.log("Start allocating objects...");
    for (var i = 0; i < 389; i++) {
        tm = testClass?.AllocateObjects();
        linear = Module.HEAP32.byteLength;
        console.log(`${i}: managed ${tm} WASM ${linear} bytes. Ratio ${linear / tm}`);
    }

    console.log("Forcing GC ...");
    tm = testClass.ForceGC();
    linear = Module.HEAP32.byteLength;
    console.log(`${i}: managed ${tm} WASM ${linear} bytes. Ratio ${linear / tm}`);

    console.log("Disposing allocated objects...");
    testClass.DisposeObjects();

    tm = testClass.AllocateObjects();
    linear = Module.HEAP32.byteLength;
    console.log(`${i}: managed ${tm} WASM ${linear} bytes. Ratio ${linear / tm}`);

    testClass.DisposeObjects();
    console.log("Forcing GC ...");
    tm = testClass.ForceGC();
    console.log(`${i}: managed ${tm} WASM ${linear} bytes. Ratio ${linear / tm}`);
    console.log("");
    console.log("Tadaaaa!");
}
catch (err) {
    exit(2, err);
}