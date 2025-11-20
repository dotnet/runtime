import { dotnet } from "./_framework/dotnet.js";
import * as runtime from "./_framework/dotnet.runtime.js";

async function fetchBinary(url) {
    return (await fetch(url, { cache: "no-cache" })).arrayBuffer();
}

function fetchWasm(url) {
    return {
        response: fetch(url, { cache: "no-cache" }),
        url,
        name: url.substring(url.lastIndexOf("/") + 1)
    };
}

const assets = [
    {
        name: "dotnet.native.js",
        // demo dynamic import
        moduleExports: import("./_framework/dotnet.native.js"),
        behavior: "js-module-native"
    },
    {
        name: "dotnet.runtime.js",
        // demo static import
        moduleExports: runtime,
        behavior: "js-module-runtime"
    },
    {
        name: "dotnet.native.wasm",
        // demo pending download promise
        pendingDownload: fetchWasm("./_framework/dotnet.native.wasm"),
        behavior: "dotnetwasm"
    },
    {
        name: "System.Private.CoreLib.wasm",
        behavior: "assembly",
        isCore: true,
    },
    {
        name: "System.Runtime.InteropServices.JavaScript.wasm",
        behavior: "assembly",
        isCore: true,
    },
    {
        name: "Wasm.Browser.Config.Sample.wasm",
        // demo buffer promise
        buffer: fetchBinary("./_framework/Wasm.Browser.Config.Sample.wasm"),
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
    "coreAssembly": {
        "System.Private.CoreLib.wasm": "",
        "System.Runtime.InteropServices.JavaScript.wasm": "",
    },
    "assembly": {
        "System.Console.wasm": "",
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
