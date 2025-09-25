//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import { defineConfig } from "rollup";
import typescript from "@rollup/plugin-typescript";
import { nodeResolve } from "@rollup/plugin-node-resolve";
import dts from "rollup-plugin-dts";
import {
    externalDependencies, isDebug, artifactsObjDir, envConstants, banner, banner_dts, configuration,
    keep_classnames, keep_fnames, reserved
} from "./rollup.config.defines.js"
import { terserPlugin, writeOnChangePlugin, consts, onwarn, alwaysLF, iife2fe, sourcemapPathTransform } from "./rollup.config.plugins.js"
import { promises as fs } from "fs";

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
    terser: {
        compress: {
            module: true,
        }, mangle: {
            module: true,
            reserved
        }
    }
});

const libNativeBrowser = configure({
    input: "./libs/System.Native.Browser/native/index.ts",
    output: [{
        name: "libNativeBrowser",
        format: "iife",
        file: artifactsObjDir + `/native/browser-${configuration}-wasm/System.Native.Browser/libSystem.Native.Browser.js`,
        footer: await fs.readFile("./libs/System.Native.Browser/libSystem.Native.Browser.footer.js"),
    }],
    terser: {
        compress: {
            toplevel: true,
            keep_fnames,
        }, mangle: {
            toplevel: true,
            keep_fnames,
            reserved,
        }
    }
});

const dotnetRuntimeJS = configure({
    input: "./libs/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.ts",
    output: [{
        file: artifactsObjDir + `/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js`,
    }],
    terser: {
        compress: {
            module: true,
        }, mangle: {
            module: true,
            keep_classnames,
            reserved
        }
    }
});

const libInteropJavaScriptNative = configure({
    input: "./libs/System.Runtime.InteropServices.JavaScript.Native/native/index.ts",
    output: [{
        name: "libInteropJavaScriptNative",
        format: "iife",
        file: artifactsObjDir + `/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js`,
        footer: await fs.readFile("./libs/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.footer.js"),
    }],
    terser: {
        compress: {
            toplevel: true,
            keep_fnames,
        }, mangle: {
            toplevel: true,
            keep_fnames,
            reserved,
        }
    }
});

const libBrowserHost = configure({
    input: "./corehost/browserhost/host/index.ts",
    output: [{
        name: "libBrowserHost",
        format: "iife",
        file: artifactsObjDir + `/coreclr/browser.wasm.${configuration}/corehost/libBrowserHost.js`,
        footer: await fs.readFile("./corehost/browserhost/libBrowserHost.footer.js"),
    }],
    terser: {
        compress: {
            toplevel: true,
            keep_fnames,
        }, mangle: {
            toplevel: true,
            keep_fnames,
            reserved,
        }
    }
});

export default defineConfig([
    dotnetJS,
    dotnetDTS,
    libNativeBrowser,
    dotnetRuntimeJS,
    libInteropJavaScriptNative,
    libBrowserHost,
]);

function configure({ input, output, terser }) {
    return {
        treeshake: !isDebug,
        input,
        output: output.map(o => {
            return {
                banner,
                format: "es",
                plugins: isDebug
                    ? [iife2fe(), writeOnChangePlugin()]
                    : [terserPlugin(terser), iife2fe(), writeOnChangePlugin()],
                sourcemap: true, //isDebug ? true : "hidden",
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
