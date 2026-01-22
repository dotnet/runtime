//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements the corehost and JS related to runtime hosting.
 */

/* eslint-disable no-undef */
/* eslint-disable space-before-function-paren */
(function () {
    function libFactory() {
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
            "wasm_load_icu_data", "BrowserHost_CreateHostContract", "BrowserHost_InitializeCoreCLR", "BrowserHost_ExecuteAssembly"
        ];
        let commonDeps = [
            "$DOTNET", "$DOTNET_INTEROP", "$ENV", "$FS", "$NODEFS",
            "$libBrowserHostFn",
            ...explicitDeps
        ];
        const lib = {
            $BROWSER_HOST: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        BROWSER_HOST.dotnetInternals = dotnetInternals;

                        const exports = {};
                        libBrowserHostFn(exports);
                        exports.dotnetInitializeModule(dotnetInternals);
                        BROWSER_HOST.assignExports(exports, BROWSER_HOST);

                        const loaderConfig = dotnetInternals[2/*InternalExchangeIndex.LoaderConfig*/];
                        if (!loaderConfig.resources.assembly ||
                            !loaderConfig.resources.coreAssembly ||
                            loaderConfig.resources.coreAssembly.length === 0 ||
                            !loaderConfig.mainAssemblyName ||
                            !loaderConfig.virtualWorkingDirectory ||
                            !loaderConfig.environmentVariables) {
                            throw new Error("Invalid runtime config, cannot initialize the runtime.");
                        }

                        for (const key in loaderConfig.environmentVariables) {
                            ENV[key] = loaderConfig.environmentVariables[key];
                        }

                        if (ENVIRONMENT_IS_NODE) {
                            Module.preInit = [() => {
                                FS.mkdir("/managed");
                                FS.mount(NODEFS, { root: "." }, "/managed");
                                FS.chdir("/managed");
                            }];
                        }
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
            lib[name] = () => "dummy";
            assignExportsBuilder += `_${String(name)} = exports.${String(name)};\n`;
        }
        for (const importName of explicitDeps) {
            explicitImportsBuilder += `_${importName}();\n`;
        }
        lib.$BROWSER_HOST.assignExports = new Function("exports", assignExportsBuilder);
        lib.$BROWSER_HOST.explicitImports = new Function(explicitImportsBuilder);

        autoAddDeps(lib, "$BROWSER_HOST");
        addToLibrary(lib);
    }
    libFactory();
})();
