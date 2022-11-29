// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from "./dotnet.js"

async function importMemory() {
    const response = await fetch("./memory.dat");
    const buffer = await response.arrayBuffer();
    return new Int8Array(buffer);
}

const memory = await importMemory();
const { setModuleImports, getAssemblyExports } = await dotnet
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

setModuleImports("main.js", { location: { href: () => window.location.href } });

await dotnet.withApplicationArguments("Runtime 2").run();

const exports = await getAssemblyExports("Wasm.Browser.Sample.dll");
console.log(exports.Sample.Test.Greet());