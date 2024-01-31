import { defineConfig } from "rollup";
import typescript from "@rollup/plugin-typescript";
import terser from "@rollup/plugin-terser";
import virtual from "@rollup/plugin-virtual";
import { nodeResolve } from "@rollup/plugin-node-resolve";
import { readFile, writeFile, mkdir } from "fs/promises";
import * as fs from "fs";
import * as path from "path";
import { createHash } from "crypto";
import dts from "rollup-plugin-dts";
import { createFilter } from "@rollup/pluginutils";
import fast_glob from "fast-glob";
import gitCommitInfo from "git-commit-info";
import MagicString from "magic-string";

const configuration = process.env.Configuration;
const isDebug = configuration !== "Release";
const isContinuousIntegrationBuild = process.env.ContinuousIntegrationBuild === "true" ? true : false;
const productVersion = process.env.ProductVersion || "8.0.0-dev";
const nativeBinDir = process.env.NativeBinDir ? process.env.NativeBinDir.replace(/"/g, "") : "bin";
const wasmObjDir = process.env.WasmObjDir ? process.env.WasmObjDir.replace(/"/g, "") : "obj";
const wasmEnableThreads = process.env.WasmEnableThreads === "true" ? true : false;
const wasmEnableSIMD = process.env.WASM_ENABLE_SIMD === "1" ? true : false;
const wasmEnableExceptionHandling = process.env.WASM_ENABLE_EH === "1" ? true : false;
const wasmEnableJsInteropByValue = process.env.ENABLE_JS_INTEROP_BY_VALUE == "1" ? true : false;
const monoDiagnosticsMock = process.env.MonoDiagnosticsMock === "true" ? true : false;
// because of stack walk at src/mono/wasm/debugger/BrowserDebugProxy/MonoProxy.cs
// and unit test at with timers.mjs
const keep_fnames = /(mono_wasm_runtime_ready|mono_wasm_fire_debugger_agent_message_with_data|mono_wasm_fire_debugger_agent_message_with_data_to_pause|mono_wasm_schedule_timer_tick)/;
const keep_classnames = /(ManagedObject|ManagedError|Span|ArraySegment|WasmRootBuffer|SessionOptionsBuilder)/;
const terserConfig = {
    compress: {
        defaults: true,
        passes: 2,
        drop_debugger: false, // we invoke debugger
        drop_console: false, // we log to console
        keep_fnames,
        keep_classnames,
    },
    mangle: {
        keep_fnames,
        keep_classnames,
    },
};
const plugins = isDebug ? [writeOnChangePlugin()] : [terser(terserConfig), writeOnChangePlugin()];
const banner = "//! Licensed to the .NET Foundation under one or more agreements.\n//! The .NET Foundation licenses this file to you under the MIT license.\n";
const banner_dts = banner + "//!\n//! This is generated file, see src/mono/wasm/runtime/rollup.config.js\n\n//! This is not considered public API with backward compatibility guarantees. \n";
// emcc doesn't know how to load ES6 module, that's why we need the whole rollup.js
const inlineAssert = [
    {
        // eslint-disable-next-line quotes
        pattern: 'mono_check\\(([^,]*), *"([^"]*)"\\);',
        // eslint-disable-next-line quotes
        replacement: (match) => `if (!(${match[1]})) throw new Error("Assert failed: ${match[2]}"); // inlined mono_check`
    },
    {
        // eslint-disable-next-line quotes
        pattern: 'mono_check\\(([^,]*), \\(\\) => *`([^`]*)`\\);',
        replacement: (match) => `if (!(${match[1]})) throw new Error(\`Assert failed: ${match[2]}\`); // inlined mono_check`
    },
    {
        // eslint-disable-next-line quotes
        pattern: 'mono_assert\\(([^,]*), *"([^"]*)"\\);',
        // eslint-disable-next-line quotes
        replacement: (match) => `if (!(${match[1]})) mono_assert(false, "${match[2]}"); // inlined mono_assert condition`
    },
    {
        // eslint-disable-next-line quotes
        pattern: 'mono_assert\\(([^,]*), \\(\\) => *`([^`]*)`\\);',
        replacement: (match) => `if (!(${match[1]})) mono_assert(false, \`${match[2]}\`); // inlined mono_assert condition`
    }
];
const checkAssert =
{
    pattern: /^\s*mono_check/gm,
    failure: "previous regexp didn't inline all mono_check statements"
};
const checkNoLoader =
{
    pattern: /_loaderModuleLoaded/gm,
    failure: "module should not contain loaderModuleLoaded member. This is probably duplicated code in the output caused by a dependency outside on the loader module."
};
const checkNoRuntime =
{
    pattern: /_runtimeModuleLoaded/gm,
    failure: "module should not contain runtimeModuleLoaded member. This is probably duplicated code in the output caused by a dependency on the runtime module."
};


let gitHash;
try {
    const gitInfo = gitCommitInfo();
    gitHash = gitInfo.hash;
} catch (e) {
    gitHash = "unknown";
}
const envConstants = {
    productVersion,
    configuration,
    wasmEnableThreads,
    wasmEnableSIMD,
    wasmEnableExceptionHandling,
    monoDiagnosticsMock,
    gitHash,
    wasmEnableJsInteropByValue,
    isContinuousIntegrationBuild,
};

const locationCache = {};
function sourcemapPathTransform(relativeSourcePath, sourcemapPath) {
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

function consts(dict) {
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

// set tsconfig.json options note exclude comes from tsconfig.json
// (which gets it from tsconfig.shared.json) to exclude node_modules,
// for example
const typescriptConfigOptions = {
    rootDirs: [".", "../../../../artifacts/bin/native/generated"],
    include: ["**/*.ts", "../../../../artifacts/bin/native/generated/**/*.ts"]
};

const outputCodePlugins = [consts(envConstants), typescript(typescriptConfigOptions)];
const externalDependencies = ["module", "process"];

const loaderConfig = {
    treeshake: !isDebug,
    input: "./loader/index.ts",
    output: [
        {
            format: "es",
            file: nativeBinDir + "/dotnet.js",
            banner,
            plugins,
            sourcemap: true,
            sourcemapPathTransform,
        }
    ],
    external: externalDependencies,
    plugins: [nodeResolve(), regexReplace(inlineAssert), regexCheck([checkAssert, checkNoRuntime]), ...outputCodePlugins],
    onwarn: onwarn
};
const runtimeConfig = {
    treeshake: !isDebug,
    input: "exports.ts",
    output: [
        {
            format: "es",
            file: nativeBinDir + "/dotnet.runtime.js",
            banner,
            plugins,
            sourcemap: true,
            sourcemapPathTransform,
        }
    ],
    external: externalDependencies,
    plugins: [regexReplace(inlineAssert), regexCheck([checkAssert, checkNoLoader]), ...outputCodePlugins],
    onwarn: onwarn
};
const wasmImportsConfig = {
    treeshake: true,
    input: "exports-linker.ts",
    output: [
        {
            format: "iife",
            name: "exportsLinker",
            file: wasmObjDir + "/exports-linker.js",
            plugins: [evalCodePlugin()],
            sourcemap: false
        }
    ],
    external: externalDependencies,
    plugins: [...outputCodePlugins],
    onwarn: onwarn
};
const typesConfig = {
    input: "./types/export-types.ts",
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
    onwarn: onwarn
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
        onwarn: onwarn
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
    loaderConfig,
    runtimeConfig,
    wasmImportsConfig,
    typesConfig,
].concat(workerConfigs)
    .concat(diagnosticMockTypesConfig ? [diagnosticMockTypesConfig] : []);
export default defineConfig(allConfigs);

function evalCodePlugin() {
    return {
        name: "evalCode",
        generateBundle: evalCode
    };
}

async function evalCode(options, bundle) {
    try {
        const name = Object.keys(bundle)[0];
        const asset = bundle[name];
        const code = asset.code;
        eval(code);
        const extractedCode = globalThis.export_linker_indexes_as_code();
        asset.code = extractedCode;
    } catch (ex) {
        this.warn(ex.toString());
        throw ex;
    }
}


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

function regexCheck(checks = []) {
    const filter = createFilter("**/*.ts");

    return {
        name: "regexCheck",

        renderChunk(code, chunk) {
            const id = chunk.fileName;
            if (!filter(id)) return null;
            return executeCheck(this, code, id);
        },

        transform(code, id) {
            if (!filter(id)) return null;
            return executeCheck(this, code, id);
        }
    };

    function executeCheck(self, code, id) {
        // self.warn("executeCheck" + id);
        for (const rep of checks) {
            const { pattern, failure } = rep;
            const match = pattern.test(code);
            if (match) {
                self.error(failure + " " + id);
                return null;
            }
        }

        return null;
    }
}


function regexReplace(replacements = []) {
    const filter = createFilter("**/*.ts");

    return {
        name: "regexReplace",

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

    function executeReplacement(_, code, id) {
        const magicString = new MagicString(code);
        if (!codeHasReplacements(code, id, magicString)) {
            return null;
        }

        const result = { code: magicString.toString() };
        result.map = magicString.generateMap({ hires: true });
        return result;
    }

    function codeHasReplacements(code, id, magicString) {
        let result = false;
        let match;
        for (const rep of replacements) {
            const { pattern, replacement } = rep;
            const rx = new RegExp(pattern, "gm");
            while ((match = rx.exec(code))) {
                result = true;
                const updated = replacement(match);
                const start = match.index;
                const end = start + match[0].length;
                magicString.overwrite(start, end, updated);
            }
        }

        // eslint-disable-next-line no-cond-assign
        return result;
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

    if (warning.code === "PLUGIN_WARNING" && warning.message.indexOf("sourcemap") !== -1) {
        return;
    }

    // eslint-disable-next-line no-console
    console.warn(`(!) ${warning.toString()} ${warning.code}`);
}
