import { defineConfig } from "rollup";
import typescript from "@rollup/plugin-typescript";
import terser from "@rollup/plugin-terser";
import virtual from "@rollup/plugin-virtual";
import { readFile, writeFile, mkdir } from "fs/promises";
import * as fs from "fs";
import * as path from "path";
import { createHash } from "crypto";
import dts from "rollup-plugin-dts";
import { createFilter } from "@rollup/pluginutils";
import fast_glob from "fast-glob";
import gitCommitInfo from "git-commit-info";

const configuration = process.env.Configuration;
const isDebug = configuration !== "Release";
const productVersion = process.env.ProductVersion || "8.0.0-dev";
const nativeBinDir = process.env.NativeBinDir ? process.env.NativeBinDir.replace(/"/g, "") : "bin";
const monoWasmThreads = process.env.MonoWasmThreads === "true" ? true : false;
const monoDiagnosticsMock = process.env.MonoDiagnosticsMock === "true" ? true : false;
const terserConfig = {
    compress: {
        defaults: true,
        passes: 2,
        drop_debugger: false,// we invoke debugger
        drop_console: false,// we log to console
    },
    mangle: {
        // because of stack walk at src/mono/wasm/debugger/BrowserDebugProxy/MonoProxy.cs
        // and unit test at src\libraries\System.Runtime.InteropServices.JavaScript\tests\System.Runtime.InteropServices.JavaScript.Legacy.UnitTests\timers.mjs
        keep_fnames: /(mono_wasm_runtime_ready|mono_wasm_fire_debugger_agent_message|mono_wasm_set_timeout_exec)/,
        keep_classnames: /(ManagedObject|ManagedError|Span|ArraySegment|WasmRootBuffer|SessionOptionsBuilder)/,
    },
    format: {
        wrap_iife: true
    }
};
const plugins = isDebug ? [writeOnChangePlugin()] : [terser(terserConfig), writeOnChangePlugin()];
const banner = "//! Licensed to the .NET Foundation under one or more agreements.\n//! The .NET Foundation licenses this file to you under the MIT license.\n";
const banner_dts = banner + "//!\n//! This is generated file, see src/mono/wasm/runtime/rollup.config.js\n\n//! This is not considered public API with backward compatibility guarantees. \n";
// emcc doesn't know how to load ES6 module, that's why we need the whole rollup.js
const format = "iife";
const name = "__dotnet_runtime";
const inlineAssert = [
    {
        pattern: /mono_assert\(([^,]*), *"([^"]*)"\);/gm,
        // eslint-disable-next-line quotes
        replacement: 'if (!($1)) throw new Error("Assert failed: $2"); // inlined mono_assert'
    },
    {
        pattern: /mono_assert\(([^,]*), \(\) => *`([^`]*)`\);/gm,
        replacement: "if (!($1)) throw new Error(`Assert failed: $2`); // inlined mono_assert"
    }, {
        pattern: /^\s*mono_assert/gm,
        failure: "previous regexp didn't inline all mono_assert statements"
    }];

let gitHash;
try {
    const gitInfo = gitCommitInfo();
    gitHash = gitInfo.hash;
} catch (e) {
    gitHash = "unknown";
}

function consts(dict) {
    /// implement rollup-plugin-const in terms of @rollup/plugin-virtual
    /// It's basically the same thing except "consts" names all its modules with a "consts:" prefix,
    /// and the virtual module always exports a single default binding (the const value).

    let newDict = {};
    for (const k in dict) {
        const newKey = "consts:" + k;
        const newVal = JSON.stringify(dict[k]);
        newDict[newKey] = `export default ${newVal}`;
    }
    return virtual(newDict);
}

// set tsconfig.json options note exclude comes from tsconfig.json
// (which gets it from tsconfig.shared.json) to exclude node_modules,
// for example
const typescriptConfigOptions = {
    rootDirs: [".", "../../../../artifacts/bin/native/generated"],
    include: ["**/*.ts", "../../../../artifacts/bin/native/generated/**/*.ts"]
};

const outputCodePlugins = [regexReplace(inlineAssert), consts({ productVersion, configuration, monoWasmThreads, monoDiagnosticsMock, gitHash }), typescript(typescriptConfigOptions)];

const externalDependencies = [
];

const iffeConfig = {
    treeshake: !isDebug,
    input: "exports.ts",
    output: [
        {
            file: nativeBinDir + "/src/es6/runtime.es6.iffe.js",
            name,
            banner,
            format,
            plugins,
        }
    ],
    external: externalDependencies,
    plugins: outputCodePlugins,
    onwarn: onwarn
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
    external: externalDependencies,
    plugins: [dts()],
};
const legacyConfig = {
    input: "./net6-legacy/export-types.ts",
    output: [
        {
            format: "es",
            file: nativeBinDir + "/dotnet-legacy.d.ts",
            banner: banner_dts,
            plugins: [writeOnChangePlugin()],
        }
    ],
    external: externalDependencies,
    plugins: [dts()],
};


let diagnosticMockTypesConfig = undefined;

if (isDebug) {
    // export types also into the source code and commit to git
    // so that we could notice that the API changed and review it
    typesConfig.output.push({
        format: "es",
        file: "./dotnet.d.ts",
        banner: banner_dts,
        plugins: [alwaysLF(), writeOnChangePlugin()],
    });
    legacyConfig.output.push({
        format: "es",
        file: "./dotnet-legacy.d.ts",
        banner: banner_dts,
        plugins: [alwaysLF(), writeOnChangePlugin()],
    });

    // export types into the source code and commit to git
    diagnosticMockTypesConfig = {
        input: "./diagnostics/mock/export-types.ts",
        output: [
            {
                format: "es",
                file: "./diagnostics-mock.d.ts",
                banner: banner_dts,
                plugins: [alwaysLF(), writeOnChangePlugin()],
            }
        ],
        external: externalDependencies,
        plugins: [dts()],
    };
}

/* Web Workers */
function makeWorkerConfig(workerName, workerInputSourcePath) {
    const workerConfig = {
        input: workerInputSourcePath,
        output: [
            {
                file: nativeBinDir + `/src/dotnet-${workerName}-worker.js`,
                format: "iife",
                banner,
                plugins
            },
        ],
        external: externalDependencies,
        plugins: outputCodePlugins,
    };
    return workerConfig;
}

const workerConfigs = findWebWorkerInputs("./workers").map((workerInput) => makeWorkerConfig(workerInput.workerName, workerInput.path));

const allConfigs = [
    iffeConfig,
    typesConfig,
    legacyConfig,
].concat(workerConfigs)
    .concat(diagnosticMockTypesConfig ? [diagnosticMockTypesConfig] : []);
export default defineConfig(allConfigs);

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

function regexReplace(replacements = []) {
    const filter = createFilter("**/*.ts");

    return {
        name: "replace",

        renderChunk(code, chunk) {
            const id = chunk.fileName;
            if (!filter(id)) return null;
            return executeReplacement(this, code, id);
        },

        transform(code, id) {
            if (!filter(id)) return null;
            return executeReplacement(this, code, id);
        }
    };

    function executeReplacement(self, code, id) {
        // TODO use MagicString for sourcemap support
        let fixed = code;
        for (const rep of replacements) {
            const { pattern, replacement, failure } = rep;
            if (failure) {
                const match = pattern.test(fixed);
                if (match) {
                    self.error(failure + " " + id, pattern.lastIndex);
                    return null;
                }
            }
            else {
                fixed = fixed.replace(pattern, replacement);
            }
        }

        if (fixed == code) {
            return null;
        }

        return { code: fixed };
    }
}

// Finds all files that look like a webworker toplevel input file in the given path.
// Does not look recursively in subdirectories.
// Returns an array of objects {"workerName": "foo", "path": "/path/dotnet-foo-worker.ts"}
//
// A file looks like a webworker toplevel input if it's `dotnet-{name}-worker.ts` or `.js`
function findWebWorkerInputs(basePath) {
    const glob = "dotnet-*-worker.[tj]s";
    const files = fast_glob.sync(glob, { cwd: basePath });
    if (files.length == 0) {
        return [];
    }
    const re = /^dotnet-(.*)-worker\.[tj]s$/;
    let results = [];
    for (const file of files) {
        const match = file.match(re);
        if (match) {
            results.push({ "workerName": match[1], "path": path.join(basePath, file) });
        }
    }
    return results;
}

function onwarn(warning) {
    if (warning.code === "CIRCULAR_DEPENDENCY") {
        return;
    }

    if (warning.code === "UNRESOLVED_IMPORT" && warning.exporter === "process") {
        return;
    }

    console.warn(`(!) ${warning.toString()}`);
}