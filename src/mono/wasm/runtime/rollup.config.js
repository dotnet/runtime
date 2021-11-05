import { defineConfig } from "rollup";
import typescript from "@rollup/plugin-typescript";
import { terser } from "rollup-plugin-terser";
import { readFile, writeFile, mkdir } from "fs/promises";
import * as fs from "fs";
import { createHash } from "crypto";
import dts from "rollup-plugin-dts";

const outputFileName = "runtime.iffe.js";
const isDebug = process.env.Configuration !== "Release";
const nativeBinDir = process.env.NativeBinDir ? process.env.NativeBinDir.replace(/"/g, "") : "bin";
const terserConfig = {
    compress: {
        defaults: false,// to agressive minification breaks subsequent emcc compilation
        drop_debugger: false,// we invoke debugger
        drop_console: false,// we log to console
        unused: false,// this breaks stuff
        // below are minification features which seems to work fine
        collapse_vars: true,
        conditionals: true,
        dead_code: true,
        if_return: true,
        inline: true,
        join_vars: true,
        loops: true,
        reduce_vars: true,
        evaluate: true,
        hoist_props: true,
        sequences: true,
    },
    mangle: {
        // because of stack walk at src/mono/wasm/debugger/BrowserDebugProxy/MonoProxy.cs
        keep_fnames: /(mono_wasm_runtime_ready|mono_wasm_fire_debugger_agent_message)/,
    },
    // we export ES5 because emcc parser has trouble parsing it otherwise
    ecma: 5,
};
const plugins = isDebug ? [writeOnChangePlugin()] : [terser(terserConfig), writeOnChangePlugin()];

export default defineConfig([
    {
        treeshake: !isDebug,
        input: "exports.ts",
        output: [{
            banner: "//! Licensed to the .NET Foundation under one or more agreements.\n//! The .NET Foundation licenses this file to you under the MIT license.\n",
            name: "__dotnet_runtime",
            file: nativeBinDir + "/src/" + outputFileName,

            // emcc doesn't know how to load ES6 module, that's why we need the whole rollup.js
            format: "iife",
            plugins: plugins
        }],
        plugins: [typescript()]
    },
    {
        input: "./exports.ts",
        output: [
            {
                format: "es",
                file: nativeBinDir + "/src/" + "dotnet.d.ts",
            }
        ],
        plugins: [dts()],
    },
]);

// this would create .md5 file next to the output file, so that we do not touch datetime of the file if it's same -> faster incremental build.
function writeOnChangePlugin() {
    return {
        name: "writeOnChange",
        generateBundle: writeWhenChanged
    };
}

async function writeWhenChanged(options, bundle) {
    try {
        const asset = bundle[outputFileName];
        const code = asset.code;
        const hashFileName = options.file + ".sha256";
        const oldHashExists = await checkFileExists(hashFileName);
        const oldFileExists = await checkFileExists(options.file);

        const newHash = createHash("sha256").update(code).digest("hex");

        let isOutputChanged = true;
        if (oldHashExists && oldFileExists) {
            const oldHash = await readFile(hashFileName, { encoding: "ascii" });
            isOutputChanged = oldHash !== newHash;
        }
        if (isOutputChanged) {
            if (!await checkFileExists(hashFileName)) {
                await mkdir(nativeBinDir + "/src", { recursive: true });
            }
            await writeFile(hashFileName, newHash);
        } else {
            // this.warn('No change in ' + options.file)
            delete bundle[outputFileName];
        }
    } catch (ex) {
        this.warn(ex.toString());
    }
}

function checkFileExists(file) {
    return fs.promises.access(file, fs.constants.F_OK)
        .then(() => true)
        .catch(() => false);
}