//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import { createHash } from "crypto";
import { readFile, writeFile, mkdir } from "fs/promises";
import virtual from "@rollup/plugin-virtual";
import * as fs from "fs";
import * as path from "path";
import terser from "@rollup/plugin-terser";

import { isContinuousIntegrationBuild } from "./rollup.config.defines.js"

export const terserPlugin = (terserOptions) => {
    let { compress, mangle } = terserOptions || {};
    compress = compress || {};
    mangle = mangle || {};
    return terser({
        ecma: "2020",
        compress: {
            defaults: true,
            passes: 2,
            drop_debugger: false, // we invoke debugger
            drop_console: false, // we log to console
            ...compress
        },
        // WASM-TODO: remove beautify
        format: {
            beautify: true,
        },
        mangle: {
            ...mangle,
        },
    })
}

// this would create .sha256 file next to the output file, so that we do not touch datetime of the file if it's same -> faster incremental build.
export const writeOnChangePlugin = () => ({
    name: "writeOnChange",
    generateBundle: writeWhenChanged
});

// Drop invocation from IIFE
export function iife2fe() {
    return {
        name: "iife2fe",
        generateBundle: (options, bundle) => {
            const name = Object.keys(bundle)[0];
            if (name.endsWith(".map")) return;
            const asset = bundle[name];
            const code = asset.code;
            //throw new Error("iife2fe " + code);
            asset.code = code
                .replace(/}\({}\);/, "};") // }({}); ->};
                .replace(/}\)\({}\);/, "});") // })({}); ->});
                ;
        }
    };
}

// force always unix line ending
export const alwaysLF = () => ({
    name: "writeOnChange",
    generateBundle: (options, bundle) => {
        const name = Object.keys(bundle)[0];
        const asset = bundle[name];
        const code = asset.code;
        asset.code = code.replace(/\r/g, "");
    }
});

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

export function onwarn(warning) {
    if (warning.code === "CIRCULAR_DEPENDENCY") {
        return;
    }

    if (warning.code === "UNRESOLVED_IMPORT" && warning.exporter === "process") {
        return;
    }

    if (warning.code === "PLUGIN_WARNING" && warning.message.indexOf("sourcemap") !== -1) {
        return;
    }

    // eslint-disable-next-line no-console
    console.warn(`(!) ${warning.toString()} ${warning.code}`);
}

export function consts(dict) {
    // implement rollup-plugin-const in terms of @rollup/plugin-virtual
    // It's basically the same thing except "consts" names all its modules with a "consts:" prefix,
    // and the virtual module always exports a single default binding (the const value).

    let newDict = {};
    for (const k in dict) {
        const newKey = "consts:" + k;
        const newVal = JSON.stringify(dict[k]);
        newDict[newKey] = `export default ${newVal}`;
    }
    return virtual(newDict);
}

const locationCache = {};
export function sourcemapPathTransform(relativeSourcePath, sourcemapPath) {
    let res = locationCache[relativeSourcePath];
    if (res === undefined) {
        if (!isContinuousIntegrationBuild) {
            const sourcePath = path.resolve(
                path.dirname(sourcemapPath),
                relativeSourcePath
            );
            res = `file:///${sourcePath.replace(/\\/g, "/")}`;
        } else {
            relativeSourcePath = relativeSourcePath.substring(12);
            res = `https://raw.githubusercontent.com/dotnet/runtime/${gitHash}/${relativeSourcePath}`;
        }
        locationCache[relativeSourcePath] = res;
    }
    return res;
}
