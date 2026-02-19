//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements the corehost and JS related to runtime hosting.
 */

/* eslint-disable no-undef */
function libBrowserHostFactory() {
    // this executes the function at link time in order to capture exports
    // this is what Emscripten does for linking JS libraries
    // https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html#javascript-limits-in-library-files
    // it would execute the code at link time and call .toString() on functions to move it to the final output
    // this process would loose any closure references, unless they are passed to `__deps` and also explicitly given to the linker
    // JS name mangling and minification also applies, see src\native\rollup.config.defines.js and `reserved` there
    const exports = {};
    libBrowserHost(exports);

    // libBrowserHostFn is too complex for acorn-optimizer.mjs to find the dependencies
    let explicitDeps = [
        "wasm_load_icu_data",
        "BrowserHost_CreateHostContract",
        "BrowserHost_InitializeCoreCLR",
        "BrowserHost_ExecuteAssembly"
    ];
    let commonDeps = [
        "$DOTNET",
        "$DOTNET_INTEROP",
        "$ENV",
        "$FS",
        "$libBrowserHostFn",
        ...explicitDeps
    ];
    const mergeBrowserHost = {
        $BROWSER_HOST: {
            selfInitialize: () => {
                if (typeof dotnetInternals !== "undefined") {
                    BROWSER_HOST.dotnetInternals = dotnetInternals;

                    const exports = {};
                    libBrowserHostFn(exports);
                    exports.dotnetInitializeModule(dotnetInternals);
                    BROWSER_HOST.assignExports(exports, BROWSER_HOST);
                }
            },
        },
        $libBrowserHostFn: libBrowserHost,
        $BROWSER_HOST__postset: "BROWSER_HOST.selfInitialize()",
        $BROWSER_HOST__deps: commonDeps,
    };

    let assignExportsBuilder = "";
    let explicitImportsBuilder = "";
    for (const exportName of Reflect.ownKeys(exports)) {
        const name = String(exportName);
        if (name === "dotnetInitializeModule") continue;
        mergeBrowserHost[name] = () => "dummy";
        assignExportsBuilder += `_${String(name)} = exports.${String(name)};\n`;
    }
    for (const importName of explicitDeps) {
        explicitImportsBuilder += `_${importName}();\n`;
    }
    mergeBrowserHost.$BROWSER_HOST.assignExports = new Function("exports", assignExportsBuilder);
    mergeBrowserHost.$BROWSER_HOST.explicitImports = new Function(explicitImportsBuilder);

    autoAddDeps(mergeBrowserHost, "$BROWSER_HOST");
    addToLibrary(mergeBrowserHost);
}

libBrowserHostFactory();
