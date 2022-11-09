// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

function exportMemory(memory) {
    const blob = new Blob([memory], { type: 'application/octet-stream' });

    let donwloadLink = document.createElement('a')
    donwloadLink.type = 'download'
    donwloadLink.href = URL.createObjectURL(blob)
    donwloadLink.download = 'memory.data'
    donwloadLink.click()
}

async function importMemory() {
    const response = await fetch("/memory.data");
    const buffer = await response.arrayBuffer();
    return new Int8Array(buffer);
}

async function runtime1() {
    const dotnet1 = (await import("./dotnet.js?1")).dotnet;
    const api1 = await dotnet1.create();
    globalThis.Module1 = api1.Module['asm']['memory'];
    exportMemory(api1.Module.HEAP8);
}

async function runtime2() {
    const dotnet2 = (await import("./dotnet.js?2")).dotnet;
    const memory = await importMemory();
    const api2 = await dotnet2
        .withConfig({
            mainAssemblyName: "Wasm.Browser.Sample.dll",
            assets: [
                {
                    virtualPath: "runtimeconfig.bin",
                    behavior: "vfs",
                    name: "supportFiles/0_runtimeconfig.bin"
                },
                {
                    loadRemote: false,
                    behavior: "icu",
                    name: "icudt.dat"
                },
                {
                    virtualPath: "/usr/share/zoneinfo/",
                    behavior: "vfs",
                    name: "dotnet.timezones.blat"
                },
                {
                    behavior: "dotnetwasm",
                    name: "dotnet.wasm"
                }],
            memory: memory
        })
        .withModuleConfig({
            configSrc: null
        })
        .create();

    globalThis.Module2 = api2.Module['asm']['memory'];

    await dotnet2.run(["Runtime 2"]);
}

// runtime1();
runtime2();