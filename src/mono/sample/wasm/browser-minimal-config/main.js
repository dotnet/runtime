import { dotnet } from "./_framework/dotnet.js";

async function fetchBinary(uri) {
    return new Uint8Array(await (await fetch(uri)).arrayBuffer());
}

const assets = [
    {
        name: "dotnet.native.js",
        behavior: "js-module-native"
    },
    {
        name: "dotnet.js",
        behavior: "js-module-dotnet"
    },
    {
        name: "dotnet.runtime.js",
        behavior: "js-module-runtime"
    },
    {
        name: "dotnet.native.wasm",
        behavior: "dotnetwasm"
    },
    {
        name: "System.Private.CoreLib.wasm",
        behavior: "assembly"
    },
    {
        name: "System.Runtime.InteropServices.JavaScript.wasm",
        behavior: "assembly"
    },
    {
        name: "Wasm.Browser.Config.Sample.wasm",
        buffer: await fetchBinary("./_framework/Wasm.Browser.Config.Sample.wasm"),
        behavior: "assembly"
    },
    {
        name: "System.Console.wasm",
        behavior: "assembly"
    },
];

const resources = {
    "jsModuleNative": {
        "dotnet.native.js": ""
    },
    "jsModuleRuntime": {
        "dotnet.runtime.js": ""
    },
    "wasmNative": {
        "dotnet.native.wasm": ""
    },
    "assembly": {
        "System.Console.wasm": "",
        "System.Private.CoreLib.wasm": "",
        "System.Runtime.InteropServices.JavaScript.wasm": "",
        "Wasm.Browser.Config.Sample.wasm": ""
    },
};

const config = {
    "mainAssemblyName": "Wasm.Browser.Config.Sample.dll",
    assets,
};

await dotnet
    .withConfig(config)
    .withElementOnExit()
    .withExitCodeLogging()
    .run();
