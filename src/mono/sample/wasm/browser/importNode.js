// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

async function importMemory() { // NodeJS
    const require = await import('module').then(mod => mod.createRequire(import.meta.url));
    const fs = require("fs");
    const buffer = await fs.promises.readFile("./memory.dat");
    return new Int8Array(buffer);
}

async function runtime2() {
    console.log("Runtime 2");
    const dotnet = (await import("./dotnet.js?2")).dotnet;
    const memory = await importMemory();
    const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
        .withConfig({
            mainAssemblyName: "Wasm.Browser.Sample.dll",
            assets: [{
                behavior: "dotnetwasm",
                name: "dotnet.wasm"
            }],
            memory: memory
        })
        .withModuleConfig({
            configSrc: null
        })
        .create();

    setModuleImports("main.js", { location: { href: () => "window.location.href" } });

    await dotnet.withApplicationArguments("Runtime 2").run();
}

runtime2();