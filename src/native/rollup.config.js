//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import { defineConfig } from "rollup";
import typescript from "@rollup/plugin-typescript";
import { nodeResolve } from "@rollup/plugin-node-resolve";
import dts from "rollup-plugin-dts";
import { terserPlugin, writeOnChangePlugin, consts, onwarn, alwaysLF, sourcemapPathTransform } from "./rollup.config.plugins.js"
import { externalDependencies, isDebug, artifactsObjDir, envConstants, banner, banner_dts, configuration } from "./rollup.config.defines.js"

const dotnetDTS = {
    input: "./libs/Common/JavaScript/types/export-api.ts",
    output: [
        {
            format: "es",
            file: artifactsObjDir + `/coreclr/browser.wasm.${configuration}/corehost/dotnet.d.ts`,
            banner: banner_dts,
            plugins: [writeOnChangePlugin()],
        },
        ...(isDebug ? [{
            format: "es",
            file: "./corehost/browserhost/loader/dotnet.d.ts",
            banner: banner_dts,
            plugins: [alwaysLF(), writeOnChangePlugin()],
        }] : [])
    ],
    external: externalDependencies,
    plugins: [dts()],
    onwarn
};

const dotnetJS = configure({
    input: "./corehost/browserhost/loader/dotnet.ts",
    output: [{
        file: artifactsObjDir + `/coreclr/browser.wasm.${configuration}/corehost/dotnet.js`,
        intro: "/*! bundlerFriendlyImports */",
    }],
});

const libSystemNativeBrowserJS = configure({
    input: "./libs/System.Native.Browser/libSystem.Native.Browser.ts",
    output: [{
        name: "libSystemNativeBrowserJS",
        format: "iife",
        file: artifactsObjDir + `/native/browser-${configuration}-wasm/System.Native.Browser/libSystem.Native.Browser.js`,
    }],
});

const dotnetRuntimeJS = configure({
    input: "./libs/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.ts",
    output: [{
        file: artifactsObjDir + `/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js`,
    }],
});

const libSystemRuntimeInteropServicesJavaScriptNativeBrowserJS = configure({
    input: "./libs/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.ts",
    output: [{
        name: "libSystemRuntimeInteropServicesJavaScriptNativeBrowserJS",
        format: "iife",
        file: artifactsObjDir + `/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js`,
    }],
});

export default defineConfig([
    dotnetJS,
    dotnetDTS,
    libSystemNativeBrowserJS,
    dotnetRuntimeJS,
    libSystemRuntimeInteropServicesJavaScriptNativeBrowserJS,
]);

function configure({ input, output }) {
    return {
        treeshake: !isDebug,
        input,
        output: output.map(o => {
            return {
                banner,
                format: "es",
                plugins: isDebug
                    ? [writeOnChangePlugin()]
                    : [terserPlugin(), writeOnChangePlugin()],
                sourcemap: isDebug ? true : "hidden",
                sourcemapPathTransform,
                ...o
            }
        }),
        external: externalDependencies,
        plugins: [
            nodeResolve(),
            consts(envConstants),
            typescript({
                tsconfig: "./tsconfig.json",
            })
        ],
        onwarn,
    }
}
