import { defineConfig } from "rollup";
import typescript from "@rollup/plugin-typescript";
import { terser } from "rollup-plugin-terser";
import { readFile, writeFile, mkdir } from "fs/promises";
import * as fs from "fs";
import * as path from "path";
import { createHash } from "crypto";
import dts from "rollup-plugin-dts";
import consts from "rollup-plugin-consts";

const configuration = process.env.Configuration;
const isDebug = configuration !== "Release";
const productVersion = process.env.ProductVersion || "7.0.0-dev";
const nativeBinDir = process.env.NativeBinDir ? process.env.NativeBinDir.replace(/"/g, "") : "bin";
const terserConfig = {
    compress: {
        defaults: false,// too agressive minification breaks subsequent emcc compilation
        drop_debugger: false,// we invoke debugger
        drop_console: false,// we log to console
        unused: false,// this breaks stuff
        // below are minification features which seems to work fine
        collapse_vars: true,
        conditionals: true,
        computed_props: true,
        properties: true,
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
        // and unit test at src\libraries\System.Private.Runtime.InteropServices.JavaScript\tests\timers.js
        keep_fnames: /(mono_wasm_runtime_ready|mono_wasm_fire_debugger_agent_message|mono_wasm_set_timeout_exec)/,
    },
};
const plugins = isDebug ? [writeOnChangePlugin()] : [terser(terserConfig), writeOnChangePlugin()];
const banner = "//! Licensed to the .NET Foundation under one or more agreements.\n//! The .NET Foundation licenses this file to you under the MIT license.\n";
const banner_dts = banner + "//!\n//! This is generated file, see src/mono/wasm/runtime/rollup.config.js\n\n//! This is not considered public API with backward compatibility guarantees. \n";
// emcc doesn't know how to load ES6 module, that's why we need the whole rollup.js
const format = "iife";
const name = "__dotnet_runtime";

const iffeConfig = {
    treeshake: !isDebug,
    input: "exports.ts",
    output: [
        {
            file: nativeBinDir + "/src/cjs/runtime.cjs.iffe.js",
            name,
            banner,
            format,
            plugins,
        },
        {
            file: nativeBinDir + "/src/es6/runtime.es6.iffe.js",
            name,
            banner,
            format,
            plugins,
        }
    ],
    onwarn: (warning, handler) => {
        if (warning.code === "EVAL" && warning.loc.file.indexOf("method-calls.ts") != -1) {
            return;
        }

        handler(warning);
    },
    plugins: [consts({ productVersion, configuration }), typescript()]
};
const typesConfig = {
    input: "./export-types.ts",
    output: [
        {
            format: "es",
            file: nativeBinDir + "/dotnet.d.ts",
            banner: banner_dts,
            plugins: [writeOnChangePlugin()],
        }
    ],
    plugins: [dts()],
};

if (isDebug) {
    // export types also into the source code and commit to git
    // so that we could notice that the API changed and review it
    typesConfig.output.push({
        format: "es",
        file: "./dotnet.d.ts",
        banner: banner_dts,
        plugins: [alwaysLF(), writeOnChangePlugin()],
    });
}

export default defineConfig([
    iffeConfig,
    typesConfig
]);

// this would create .sha256 file next to the output file, so that we do not touch datetime of the file if it's same -> faster incremental build.
function writeOnChangePlugin() {
    return {
        name: "writeOnChange",
        generateBundle: writeWhenChanged
    };
}

// force always unix line ending
function alwaysLF() {
    return {
        name: "writeOnChange",
        generateBundle: (options, bundle) => {
            const name = Object.keys(bundle)[0];
            const asset = bundle[name];
            const code = asset.code;
            asset.code = code.replace(/\r/g, "");
        }
    };
}

async function writeWhenChanged(options, bundle) {
    try {
        const name = Object.keys(bundle)[0];
        const asset = bundle[name];
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
            const dir = path.dirname(options.file);
            if (!await checkFileExists(dir)) {
                await mkdir(dir, { recursive: true });
            }
            await writeFile(hashFileName, newHash);
        } else {
            // this.warn('No change in ' + options.file)
            delete bundle[name];
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