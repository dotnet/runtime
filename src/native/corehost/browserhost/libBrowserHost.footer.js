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
            "$NODEFS",
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

                        const browserVirtualAppBase = "/managed"; // keep in sync other places that define browserVirtualAppBase
                        // load all DLLs into linear memory and tell CoreCLR that they are in /managed folder via TRUSTED_PLATFORM_ASSEMBLIES
                        Module.preInit = [() => {
                            FS.mkdir(browserVirtualAppBase);
                            if (ENVIRONMENT_IS_NODE) {
                                // on NodeJS we mount the current working directory of the host OS as /managed
                                // so that any other files can be loaded via file IO of the emscripten FS emulator
                                // as in the dotnet application started in the host current folder
                                //
                                // this doesn't make sense in browser and it doesn't work for V8 shell
                                // it also means that any files in loaderConfig.resources.coreVfs and loaderConfig.resources.vfs will be ignored on NodeJS
                                // because NODEFS is mounted on top of /managed and we assume that the host file system has all the files needed
                                FS.mount(NODEFS, { root: "." }, browserVirtualAppBase);
                            }
                            FS.chdir(browserVirtualAppBase);
                        }];
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
    function trim() {
        return ERRNO_CODES.EOPNOTSUPP;
    }

    // TODO-WASM: fix PAL https://github.com/dotnet/runtime/issues/122506
    LibraryManager.library.__syscall_pipe = trim;
    delete LibraryManager.library.__syscall_pipe__deps;

    LibraryManager.library.__syscall_connect = trim;
    delete LibraryManager.library.__syscall_connect__deps;

    LibraryManager.library.__syscall_sendto = trim;
    delete LibraryManager.library.__syscall_sendto__deps;

    LibraryManager.library.__syscall_socket = trim;
    delete LibraryManager.library.__syscall_socket__deps;
})();
