// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

async function exportMemory(memory) {
    const blob = new Blob([memory], { type: 'application/octet-stream' });

    let donwloadLink = document.createElement('a')
    donwloadLink.type = 'download'
    donwloadLink.href = URL.createObjectURL(blob)
    donwloadLink.download = 'memory.dat'
    donwloadLink.click()
}

async function importMemory() {
    const response = await fetch("/memory.dat");
    const buffer = await response.arrayBuffer();
    return new Int8Array(buffer);
}

async function runtime1() {
    console.log("Runtime 1");
    const dotnet = (await import("./dotnet.js?1")).dotnet;
    const { setModuleImports, getAssemblyExports, getConfig, Module } = await dotnet.create();

    setModuleImports("main.js", { location: { href: () => window.location.href } });

    const exports = getAssemblyExports(getConfig().mainAssemblyName);

    await exportMemory(Module.HEAP8);
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

    setModuleImports("main.js", { location: { href: () => window.location.href } });

    await dotnet.withApplicationArguments("Runtime 2").run();

    // const exports = await getAssemblyExports(getConfig().mainAssemblyName);
    // console.log(exports.Sample.Test.Greet());
}


const runtimeParam = new URLSearchParams(location.search).get("runtime");
if (runtimeParam === "1") {
    runtime1();
} else {
    runtime2();
}