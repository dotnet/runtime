// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet as dotnet1 } from './dotnet.js?1'
import { dotnet as dotnet2 } from './dotnet.js?2'

const api1 = await dotnet1.create();

globalThis.Module1 = api1.Module['asm']['memory'];

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
        memory: api1.Module.HEAP8
    })
    .withModuleConfig({
        configSrc: null
    })
    .create();

globalThis.Module2 = api2.Module['asm']['memory'];

await dotnet2.run(["Runtime 2"]);