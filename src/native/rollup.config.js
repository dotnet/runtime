//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import { defineConfig } from "rollup";
import typescript from "@rollup/plugin-typescript";
import { nodeResolve } from "@rollup/plugin-node-resolve";
import dts from "rollup-plugin-dts";
import {
    externalDependencies, envConstants, banner, banner_dts,
    isDebug, nativeJavaScriptBin,
    keep_classnames, keep_fnames, reserved
} from "./rollup.config.defines.js";
import { terserPlugin, writeOnChangePlugin, consts, onwarn, alwaysLF, iife2fe, sourcemapPathTransform } from "./rollup.config.plugins.js";
import { promises as fs } from "fs";

const dotnetDTS = {
    input: "./libs/Common/JavaScript/types/export-api.ts",
    output: [
        {
            format: "es",
            file: nativeJavaScriptBin + "dotnet.d.ts",
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
        file: nativeJavaScriptBin + "dotnet.js",
        intro: "/*! bundlerFriendlyImports */",
    }],
    terser: {
        compress: {
            module: true,
        }, mangle: {
            module: true,
        }
    }
});

const libNativeBrowser = configure({
    input: "./libs/System.Native.Browser/native/index.ts",
    output: [{
        name: "libNativeBrowser",
        format: "iife",
        file: nativeJavaScriptBin + "libSystem.Native.Browser.js",
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

const libBrowserUtils = configure({
    input: "./libs/System.Native.Browser/utils/index.ts",
    output: [{
        name: "libBrowserUtils",
        format: "iife",
        file: nativeJavaScriptBin + "libSystem.Browser.Utils.js",
        footer: await fs.readFile("./libs/System.Native.Browser/libSystem.Browser.Utils.footer.js"),
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
        file: nativeJavaScriptBin + "dotnet.runtime.js",
    }],
    terser: {
        compress: {
            module: true,
        }, mangle: {
            module: true,
            keep_classnames,
        }
    }
});

const libInteropJavaScriptNative = configure({
    input: "./libs/System.Runtime.InteropServices.JavaScript.Native/native/index.ts",
    output: [{
        name: "libInteropJavaScriptNative",
        format: "iife",
        file: nativeJavaScriptBin + "libSystem.Runtime.InteropServices.JavaScript.Native.js",
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
    input: "./corehost/browserhost/lib-browserhost/index.ts",
    output: [{
        name: "libBrowserHost",
        format: "iife",
        file: nativeJavaScriptBin + "libBrowserHost.js",
        footer: await fs.readFile("./corehost/browserhost/lib-browserhost/libBrowserHost.footer.js"),
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
    libBrowserUtils,
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
            };
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
    };
}
